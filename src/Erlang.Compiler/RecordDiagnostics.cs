namespace Erlang.Compiler;

internal static class RecordDiagnostics
{
    public const string GuardUpdate = "Tuple record update is not legal in a guard";
    public const string WildcardUpdate = "A tuple record update cannot contain a wildcard field";
    public const string InvalidInfo = "record_info requires literal fields/size and a defined record name";

    public static string UndefinedRecord(string name) => $"Record '{name}' is undefined";

    public static string UndefinedField(string record, string field) => $"Field '{field}' is undefined in record '{record}'";

    public static string DuplicateRecord(string name) => $"Record '{name}' is already defined";

    public static string DuplicateField(string name) => $"Record field '{name}' is already defined";
}
