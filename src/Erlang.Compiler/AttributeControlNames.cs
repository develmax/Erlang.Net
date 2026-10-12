namespace Erlang.Compiler;

internal static class AttributeControlNames
{
    public static IReadOnlyList<string> Reserved { get; } = Array.AsReadOnly(new[]
    {
        "import",
        "import_record",
        "on_load",
        "nifs",
        "native_record",
        "export_record",
        "file",
        "type",
        "opaque",
        "nominal",
        "export_type",
        "spec",
        "callback",
        "optional_callbacks",
        "doc",
        "moduledoc",
        ModuleAttributeNames.Compile
    });
}
