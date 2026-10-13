namespace Erlang.Compiler;

internal static class NativeRecordDiagnostics
{
    public const string MultiField = "multi-field initialization (assigning to _) is only supported for tuple records";
    public const string GuardConstruction = "creating a record in a guard is only supported for tuple records";
    public const string RecordInfo = "record_info/2 is only supported for tuple records";
    public const string TupleExport = "tuple records cannot be exported; only native records can";

    public static string MissingValue(string name, string field) => $"field {field} is not initialized in native record {name}";

    public static string InvalidDefault(string name, string field) => $"illegal default value for field {field} in native record {name}";

    public static string Undefined(string name) => $"native record {name} undefined";

    public static string Index(string name, string field) => $"syntax #{name}.{field} is only supported for tuple records";

    public static string UndefinedType(string name, int arity) => $"type {name}({string.Join(",", Enumerable.Repeat("_", arity))}) undefined";

    public static string RedefineImported(string module, string name) => $"record {name} already imported from {module}";

    public static string RedefineLocal(string name) => $"record {name} is already defined locally";

    public static string DuplicateExport(string name) => $"native record {name} already exported";
}
