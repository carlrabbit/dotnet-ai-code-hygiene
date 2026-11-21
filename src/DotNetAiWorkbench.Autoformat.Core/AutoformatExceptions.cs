namespace DotNetAiWorkbench.Autoformat.Core;

public sealed class ProductException(string message) : Exception(message);
public sealed class EnvironmentException(string message) : Exception(message);
