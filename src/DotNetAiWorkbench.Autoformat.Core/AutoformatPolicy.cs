namespace DotNetAiWorkbench.Autoformat.Core;

/// <summary>The sole owner of the fixed mechanical EditorConfig policy used by Autoformat and transitional hygiene.</summary>
public static class AutoformatPolicy
{
    public static IReadOnlyList<string> ApprovedDiagnostics { get; } = new[] { "IDE0005", "IDE0011", "IDE0036", "IDE0040", "IDE0090", "IDE0034", "IDE0004", "IDE0037" };

    public static IReadOnlyList<(string Key, string Value)> MechanicalOptions { get; } = new (string Key, string Value)[]
    {
        ("indent_style", "space"), ("indent_size", "4"), ("dotnet_sort_system_directives_first", "true"),
        ("dotnet_separate_import_directive_groups", "false"), ("csharp_prefer_braces", "true"),
        ("csharp_preferred_modifier_order", "public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async"),
        ("dotnet_style_require_accessibility_modifiers", "always"), ("csharp_style_implicit_object_creation_when_type_is_apparent", "true"),
        ("csharp_prefer_simple_default_expression", "true"), ("dotnet_style_coalesce_expression", "true"),
        ("dotnet_style_prefer_inferred_tuple_names", "true"), ("dotnet_style_prefer_inferred_anonymous_type_member_names", "true"),
        ("dotnet_diagnostic.IDE0005.severity", "warning"), ("dotnet_diagnostic.IDE0004.severity", "warning"),
        ("dotnet_diagnostic.IDE0011.severity", "error"), ("dotnet_diagnostic.IDE0034.severity", "warning"),
        ("dotnet_diagnostic.IDE0036.severity", "warning"), ("dotnet_diagnostic.IDE0040.severity", "error"),
        ("dotnet_diagnostic.IDE0037.severity", "warning"), ("dotnet_diagnostic.IDE0090.severity", "warning")
    };

    public static string BuildEditorConfigBlock(string start, string end) => string.Join("\n",
        new[] { start, "[*.cs]" }.Concat(MechanicalOptions.Select(option => option.Key + " = " + option.Value)).Append(end));
}
