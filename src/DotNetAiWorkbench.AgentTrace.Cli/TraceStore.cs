using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DotNetAiWorkbench.AgentTrace;

internal interface ITraceStore
{
    public IReadOnlyList<StoredEvent> ReadAll();
    public bool Append(TraceEvent item);
    public string Provider { get; }
    public string Location { get; }
    public string LockDirectory { get; }
}

internal static class TraceRepository
{
    public static (string Root, ITraceStore Store) Open(string? explicitRoot)
    {
        string root = ResolveRoot(explicitRoot);
        string configFile = Path.Combine(root, ".agenttrace.json");
        string provider = "jsonl", configuredPath = Path.Combine(".agenttrace", "sessions");
        if (File.Exists(configFile))
        {
            try
            {
                TraceConfig? config = JsonSerializer.Deserialize<TraceConfig>(File.ReadAllText(configFile), TraceJson.Options);
                if (config is null || config.SchemaVersion != 1 || config.Storage is null || config.Storage.Provider is not ("sqlite" or "jsonl") || string.IsNullOrWhiteSpace(config.Storage.Path))
                {
                    throw new TraceException(".agenttrace.json must use schemaVersion 1 and storage provider 'jsonl' or 'sqlite' with a path.");
                }

                provider = config.Storage.Provider;
                configuredPath = config.Storage.Path;
                if (config.SchemaVersion != 1)
                {
                    throw new TraceException($"Unsupported .agenttrace.json schema version {config.SchemaVersion}.");
                }

                if (provider is not ("jsonl" or "sqlite"))
                {
                    throw new TraceException("Invalid .agenttrace.json storage provider; expected 'jsonl' or 'sqlite'.");
                }

                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    throw new TraceException("Invalid .agenttrace.json storage path; it must not be empty.");
                }
            }
            catch (TraceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TraceException($"Invalid .agenttrace.json: {ex.Message}");
            }
        }

        string fullPath = Path.IsPathRooted(configuredPath) ? Path.GetFullPath(configuredPath) : Path.GetFullPath(Path.Combine(root, configuredPath));
        try
        {
            ITraceStore store = provider == "sqlite" ? new SqliteTraceStore(fullPath) : new JsonlTraceStore(fullPath);
            return (root, store);
        }
        catch (TraceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TraceException($"Could not open configured {provider} trace store at '{fullPath}': {ex.Message}");
        }
    }

    private static string ResolveRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            string target = Path.GetFullPath(explicitRoot);
            if (Directory.Exists(Path.Combine(target, ".git")) || File.Exists(Path.Combine(target, ".git")))
            {
                return target;
            }

            throw new TraceException($"--repo path '{target}' is not a Git repository root.");
        }

        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")) && !File.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new TraceException("Current directory is not inside a Git repository. Supply --repo <path>.");
    }
}

internal sealed class JsonlTraceStore : ITraceStore
{
    public string Provider => "jsonl";
    public string Location { get; }
    public string LockDirectory { get; }

    public JsonlTraceStore(string directory)
    {
        Location = Path.GetFullPath(directory);
        LockDirectory = Path.Combine(Location, ".locks");
        Directory.CreateDirectory(Location);
        Directory.CreateDirectory(LockDirectory);
    }

    private string FileFor(string session) => Path.Combine(Location, SessionFileName(session) + ".jsonl");

    public IReadOnlyList<StoredEvent> ReadAll()
    {
        var all = new List<StoredEvent>();
        foreach (string file in Directory.EnumerateFiles(Location, "*.jsonl"))
        {
            string text;
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                text = reader.ReadToEnd();
            }

            string[] lines = text.Split('\n');
            bool hasTerminatedRecord = text.EndsWith('\n');
            long sequence = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    TraceEvent item = JsonSerializer.Deserialize<TraceEvent>(line, TraceJson.Options) ?? throw new TraceException($"Invalid trace record in '{file}'.");
                    all.Add(new StoredEvent(sequence++, item));
                }
                catch (JsonException ex) when (i == lines.Length - 1 && !hasTerminatedRecord)
                {
                    _ = ex; // Ignore only a possibly torn trailing JSONL record.
                }
                catch (JsonException ex)
                {
                    throw new TraceException($"Invalid trace record in '{file}'.", ex);
                }
            }
        }

        return all;
    }

    public bool Append(TraceEvent item)
    {
        // TraceService holds this session's cross-process lock across repair and append.
        byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(item, TraceJson.Options) + "\n");
        using var stream = new FileStream(FileFor(item.Session), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.WriteThrough);
        bool recoveredTail = RecoverUnterminatedTail(stream);
        stream.Position = stream.Length;
        stream.Write(data);
        stream.Flush(true);
        return recoveredTail;
    }

    private static bool RecoverUnterminatedTail(FileStream stream)
    {
        if (stream.Length == 0)
        {
            return false;
        }

        stream.Position = stream.Length - 1;
        if (stream.ReadByte() == '\n')
        {
            return false;
        }

        long tailStart = stream.Length;
        for (long position = stream.Length - 1; position >= 0; position--)
        {
            stream.Position = position;
            if (stream.ReadByte() == '\n')
            {
                tailStart = position + 1;
                break;
            }

            tailStart = position;
        }

        long tailLength = stream.Length - tailStart;
        if (tailLength > int.MaxValue)
        {
            throw new TraceException("The unterminated JSONL tail is too large to recover safely.");
        }

        byte[] tailBytes = new byte[(int)tailLength];
        stream.Position = tailStart;
        stream.ReadExactly(tailBytes);

        string tail;
        try
        {
            tail = new System.Text.UTF8Encoding(false, true).GetString(tailBytes);
        }
        catch (System.Text.DecoderFallbackException)
        {
            stream.SetLength(tailStart);
            stream.Flush(true);
            return true;
        }

        try
        {
            using (JsonDocument.Parse(tail)) { }
        }
        catch (JsonException)
        {
            // A syntactically partial final record is discarded; prior newline-terminated records stay intact.
            stream.SetLength(tailStart);
            stream.Flush(true);
            return true;
        }

        try
        {
            _ = JsonSerializer.Deserialize<TraceEvent>(tail, TraceJson.Options)
                ?? throw new TraceException("A valid unterminated JSONL record is not a trace event.");
        }
        catch (JsonException ex)
        {
            throw new TraceException("A valid unterminated JSONL record is not a trace event; refusing to alter it.", ex);
        }

        // Valid JSON without its line terminator remains a complete record; separate it before appending.
        stream.Position = stream.Length;
        stream.WriteByte((byte)'\n');
        stream.Flush(true);
        return false;
    }

    internal static string SessionFileName(string session)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(session)));
}

internal sealed class SqliteTraceStore : ITraceStore
{
    private const int SchemaVersion = 1;
    private readonly string _connectionString;
    public string Provider => "sqlite";
    public string Location { get; }
    public string LockDirectory { get; }

    static SqliteTraceStore()
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());
    }

    public SqliteTraceStore(string path)
    {
        Location = Path.GetFullPath(path);
        if (Directory.Exists(Location))
        {
            throw new TraceException("Configured SQLite path is a directory; choose a database file path.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Location)!);
        LockDirectory = Location + ".locks";
        Directory.CreateDirectory(LockDirectory);
        using (new SessionLock(LockDirectory, "database-initialization"))
        {
            if (File.Exists(Location) && new FileInfo(Location).Length > 0)
            {
                ValidateExistingDatabase(Location);
            }

            _connectionString = new SqliteConnectionStringBuilder { DataSource = Location, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true, DefaultTimeout = 10 }.ToString();
            Initialize();
        }
    }

    private static void ValidateExistingDatabase(string path)
    {
        string connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 5 }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }
        }

        if (names.Count == 0)
        {
            return; // An empty SQLite database is a valid new destination.
        }

        if (!names.SetEquals(["trace_meta", "trace_events"]))
        {
            throw new TraceException("Configured SQLite file is not an AgentTrace database; choose a new path to avoid altering unrelated data.");
        }

        if (!ColumnsMatch(connection, "trace_meta", ["schema_version"]) || !ColumnsMatch(connection, "trace_events", ["sequence", "session_key", "payload"]))
        {
            throw new TraceException("Configured SQLite file has incompatible AgentTrace table definitions; choose a new path to avoid altering unrelated data.");
        }

        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT schema_version FROM trace_meta LIMIT 1";
        object? version = versionCommand.ExecuteScalar();
        if (version is null || version == DBNull.Value || Convert.ToInt32(version) != SchemaVersion)
        {
            throw new TraceException("Configured AgentTrace SQLite database has an unsupported schema version.");
        }
    }

    private static bool ColumnsMatch(SqliteConnection connection, string table, string[] expected)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table})";
        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase);
    }

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=10000; PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
        return connection;
    }

    private void Initialize()
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "CREATE TABLE IF NOT EXISTS trace_meta (schema_version INTEGER NOT NULL); CREATE TABLE IF NOT EXISTS trace_events (sequence INTEGER PRIMARY KEY AUTOINCREMENT, session_key TEXT NOT NULL, payload TEXT NOT NULL); CREATE INDEX IF NOT EXISTS ix_trace_events_session ON trace_events(session_key, sequence);";
        command.ExecuteNonQuery();
        using var version = connection.CreateCommand();
        version.Transaction = transaction;
        version.CommandText = "SELECT schema_version FROM trace_meta LIMIT 1";
        object? actual = version.ExecuteScalar();
        if (actual is null || actual == DBNull.Value)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO trace_meta(schema_version) VALUES($version)";
            insert.Parameters.AddWithValue("$version", SchemaVersion);
            insert.ExecuteNonQuery();
        }
        else if (Convert.ToInt32(actual) != SchemaVersion)
        {
            throw new TraceException("Configured AgentTrace SQLite database has an unsupported schema version.");
        }

        transaction.Commit();
    }

    public IReadOnlyList<StoredEvent> ReadAll()
    {
        using var connection = Connect();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sequence, payload FROM trace_events ORDER BY sequence";
        using var reader = command.ExecuteReader();
        var events = new List<StoredEvent>();
        while (reader.Read())
        {
            TraceEvent item = JsonSerializer.Deserialize<TraceEvent>(reader.GetString(1), TraceJson.Options) ?? throw new TraceException("Invalid SQLite trace record.");
            events.Add(new StoredEvent(reader.GetInt64(0), item));
        }

        return events;
    }

    public bool Append(TraceEvent item)
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO trace_events(session_key,payload) VALUES($session,$payload)";
        command.Parameters.AddWithValue("$session", item.Session);
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(item, TraceJson.Options));
        command.ExecuteNonQuery();
        transaction.Commit();
        return false;
    }
}

internal sealed class SessionLock(string directory, string session) : IDisposable
{
    private readonly FileStream _stream = Acquire(Path.Combine(directory, JsonlTraceStore.SessionFileName(session) + ".lock"));

    private static FileStream Acquire(string path)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 3000)
            {
                Thread.Sleep(20);
            }
        }
    }

    public void Dispose() => _stream.Dispose();
}
