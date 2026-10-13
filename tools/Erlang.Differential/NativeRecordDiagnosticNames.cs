namespace Erlang.Differential;

internal static class NativeRecordDiagnosticNames
{
    public const string InvalidUnderscoreName = "invalid_underscore_name";
    public const string PutRecord = "put_record";
    public const string FailRegister = "f";
    public const string DestinationRegister = "x";
    public const string AtomOperand = "atom";
    public const string ListOperand = "list";
    public const string NilOperand = "nil";
    public const string AbortedModule = "native_record_syntax_contract";
    public const string AbortedFunction = "run";
    public const int AbortedFunctionArity = 0;
    public const int AbortedInstructionIndex = 6;
    public const int AbortedLiveRegisters = 0;
    public const int AbortedFailLabel = 0;
    public const int AbortedDestinationIndex = 0;
    public const string RecordCode = "ERL009";
    public const string GuardCode = "ERL007";
    public const string NoValue = "novalue";
    public const string IllegalDefault = "illegal_native_record_default";
    public const string IllegalIndex = "native_record_illegal_record_index";
    public const string IllegalMultiField = "native_record_illegal_multi_field_init";
    public const string IllegalInfo = "native_record_illegal_record_info";
    public const string InGuard = "native_record_in_guard";
    public const string TupleExport = "tuple_record_export";
    public const string UndefinedType = "undefined_type";
    public const string UndefinedField = "undefined_field";
    public const string RedefineLocal = "redefine_local_record";
    public const string RedefineImported = "redefine_imported_record";
    public const string Undefined = "undefined_native_record";
}
