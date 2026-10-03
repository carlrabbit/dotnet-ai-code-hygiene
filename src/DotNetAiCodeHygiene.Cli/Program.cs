using System.CommandLine;

namespace DotNetAiCodeHygiene.Cli;

internal static class Program
{
    private static readonly string[] ReservedCommands =
    [
        "format",
        "normalize",
        "check",
        "explain",
        "ignore",
        "unignore",
        "ignores",
        "rules"
    ];

    public static Task<int> Main(string[] args)
    {
        RootCommand root = CreateRootCommand();
        System.CommandLine.ParseResult parseResult = root.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            foreach (System.CommandLine.Parsing.ParseError error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return Task.FromResult(2);
        }

        return parseResult.InvokeAsync();
    }

    private static RootCommand CreateRootCommand()
    {
        RootCommand root = new("AI-first code hygiene tooling.");
        foreach (string name in ReservedCommands)
        {
            root.Subcommands.Add(new Command(name, $"Reserved {name} command; product behavior is not implemented yet."));
        }

        return root;
    }
}
