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
    RecordWildcardUpdate,
    NonLatin1Module,
    EmptyModule,
    BlankModule,
    ControlModule,
    RecordPredicateGuard,
    RecordPredicateThreeGuard,
    CharacterLiteralIllegal,
    CharacterLiteralUnterminated,
    UndefinedFunction,
    FormBoundarySyntax,
    NativeRecord
}
