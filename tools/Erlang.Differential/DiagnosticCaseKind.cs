namespace Erlang.Differential;

public enum DiagnosticCaseKind
{
    VariableScope,
    StacktraceBound,
    StacktraceGuard,
    InvalidPattern,
    UndefinedRecord,
    UndefinedRecordField,
    DuplicateRecord,
    DuplicateRecordField,
    RecordWildcardUpdate
}
