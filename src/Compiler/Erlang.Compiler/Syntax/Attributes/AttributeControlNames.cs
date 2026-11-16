namespace Erlang.Compiler;

internal static class AttributeControlNames
{
    public static IReadOnlyList<string> Reserved { get; } = Array.AsReadOnly(new[]
    {
        "import",
        "import_record",
        ModuleAttributeNames.OnLoad,
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
        ModuleAttributeNames.Doc,
        ModuleAttributeNames.ModuleDoc,
        ModuleAttributeNames.Compile
    });
}
