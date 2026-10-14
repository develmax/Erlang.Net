namespace Erlang.Differential;

/// <summary>Independent expected diagnostic contracts for variable scope fixtures.</summary>
public static class CompiledDiagnosticExpectations
{
    public const string ModuleDefinitionCode = "ERL010";
    public const string NonLatin1ModuleTag = "non_latin1_module_unsupported";
    public const string EmptyModuleTag = "empty_module_name";
    public const string BlankModuleTag = "blank_module_name";
    public const string ControlModuleTag = "ctrl_chars_in_module_name";
    public const string NonLatin1ModuleMessage = "module names with non-latin1 characters are not supported";
    public const string EmptyModuleMessage = "the module name must not be empty";
    public const string BlankModuleMessage = "the module name must contain at least one visible character";
    public const string ControlModuleMessage = "the module name must not contain control characters";
    public const string RecordDefinitionCode = "ERL009";
    public const string UndefinedRecordTag = "undefined_record";
    public const string UndefinedRecordFieldTag = "undefined_field";
    public const string DuplicateRecordTag = "redefine_record";
    public const string DuplicateRecordFieldTag = "redefine_field";
    public const string RecordWildcardUpdateTag = "wildcard_in_update";
    public const string RecordWildcardUpdateMessage = "A tuple record update cannot contain a wildcard field";
    public const string VariableBindingCode = "ERL006";
    public const string UnboundVariableTag = "unbound_var";
    public const string UnsafeVariableTag = "unsafe_var";
    public const string ReferenceMode = "compile:forms/error reasons without annotations;erl_scan:string/error reasons for explicit character-literal kinds";
    public const string StacktraceFreshnessCode = "ERL006";
    public const string StacktraceGuardCode = "ERL007";
    public const string StacktraceBoundTag = "stacktrace_bound";
    public const string StacktraceGuardTag = "stacktrace_guard";
    public const string InvalidPatternTag = "illegal_pattern";
    public const string InvalidPatternCode = "ERL004";
    public const string InvalidPatternMessage = "Invalid or unsupported pattern";

    public static string Code(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.BasedInteger => BasedIntegerDiagnosticExpectations.Code,
        DiagnosticCaseKind.NativeRecord => NativeRecordDiagnosticCases.Contracts.Single(contract => contract.Case.Name == fixture.Name).Code,
        DiagnosticCaseKind.UndefinedFunction => FunctionResolutionDiagnosticExpectations.Code,
        DiagnosticCaseKind.FormBoundarySyntax => FormBoundaryExpectations.Code,
        DiagnosticCaseKind.CharacterLiteralIllegal or DiagnosticCaseKind.CharacterLiteralUnterminated => CharacterLiteralDiagnosticExpectations.Code,
        DiagnosticCaseKind.RecordPredicateGuard or DiagnosticCaseKind.RecordPredicateThreeGuard => RecordPredicateDiagnosticExpectations.GuardCode,
        DiagnosticCaseKind.NonLatin1Module or DiagnosticCaseKind.EmptyModule or DiagnosticCaseKind.BlankModule or DiagnosticCaseKind.ControlModule => ModuleDefinitionCode,
        DiagnosticCaseKind.VariableScope => VariableBindingCode,
        DiagnosticCaseKind.StacktraceBound => StacktraceFreshnessCode,
        DiagnosticCaseKind.StacktraceGuard => StacktraceGuardCode,
        DiagnosticCaseKind.InvalidPattern => InvalidPatternCode,
        DiagnosticCaseKind.UndefinedRecord or DiagnosticCaseKind.UndefinedRecordField or DiagnosticCaseKind.DuplicateRecord or DiagnosticCaseKind.DuplicateRecordField or DiagnosticCaseKind.RecordWildcardUpdate => RecordDefinitionCode,
        _ => throw new NotSupportedException()
    };

    public static string Message(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.BasedInteger => BasedIntegerDiagnosticExpectations.Message(fixture),
        DiagnosticCaseKind.NativeRecord => NativeRecordDiagnosticCases.Contracts.Single(contract => contract.Case.Name == fixture.Name).Message,
        DiagnosticCaseKind.UndefinedFunction => FunctionResolutionDiagnosticExpectations.Message(fixture),
        DiagnosticCaseKind.FormBoundarySyntax => FormBoundaryExpectations.Message,
        DiagnosticCaseKind.CharacterLiteralIllegal => CharacterLiteralDiagnosticExpectations.IllegalMessage,
        DiagnosticCaseKind.CharacterLiteralUnterminated => CharacterLiteralDiagnosticExpectations.UnterminatedMessage,
        DiagnosticCaseKind.RecordPredicateGuard => RecordPredicateDiagnosticExpectations.GuardMessage,
        DiagnosticCaseKind.RecordPredicateThreeGuard => RecordPredicateDiagnosticExpectations.ThreeGuardMessage,
        DiagnosticCaseKind.NonLatin1Module => NonLatin1ModuleMessage,
        DiagnosticCaseKind.EmptyModule => EmptyModuleMessage,
        DiagnosticCaseKind.BlankModule => BlankModuleMessage,
        DiagnosticCaseKind.ControlModule => ControlModuleMessage,
        DiagnosticCaseKind.VariableScope => Message(fixture.Variable),
        DiagnosticCaseKind.StacktraceBound => $"Stacktrace variable '{fixture.Variable}' must be fresh",
        DiagnosticCaseKind.StacktraceGuard => $"Stacktrace variable '{fixture.Variable}' is not legal in a guard",
        DiagnosticCaseKind.InvalidPattern => InvalidPatternMessage,
        DiagnosticCaseKind.UndefinedRecord => $"Record '{fixture.Variable}' is undefined",
        DiagnosticCaseKind.UndefinedRecordField => $"Field '{fixture.UnsafeConstruct}' is undefined in record '{fixture.Variable}'",
        DiagnosticCaseKind.DuplicateRecord => $"Record '{fixture.Variable}' is already defined",
        DiagnosticCaseKind.DuplicateRecordField => $"Record field '{fixture.UnsafeConstruct}' is already defined",
        DiagnosticCaseKind.RecordWildcardUpdate => RecordWildcardUpdateMessage,
        _ => throw new NotSupportedException()
    };

    public static Term ReferenceOutcome(CompiledDiagnosticCase fixture) => fixture.Kind switch
    {
        DiagnosticCaseKind.BasedInteger => BasedIntegerDiagnosticExpectations.Outcome(fixture),
        DiagnosticCaseKind.NativeRecord => Term.Tuple(
            Term.A(OracleOutcomeTags.Failure),
            Term.List(NativeRecordDiagnosticCases.Contracts.Single(contract => contract.Case.Name == fixture.Name).Reason)
        ),
        DiagnosticCaseKind.UndefinedFunction => FunctionResolutionDiagnosticExpectations.Outcome(fixture),
        DiagnosticCaseKind.FormBoundarySyntax => FormBoundaryExpectations.Outcome,
        DiagnosticCaseKind.CharacterLiteralIllegal => CharacterLiteralDiagnosticExpectations.Outcome(false),
        DiagnosticCaseKind.CharacterLiteralUnterminated => CharacterLiteralDiagnosticExpectations.Outcome(true),
        DiagnosticCaseKind.RecordPredicateGuard or DiagnosticCaseKind.RecordPredicateThreeGuard => RecordPredicateDiagnosticExpectations.GuardOutcome,
        DiagnosticCaseKind.NonLatin1Module => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(NonLatin1ModuleTag))),
        DiagnosticCaseKind.EmptyModule => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(EmptyModuleTag))),
        DiagnosticCaseKind.BlankModule => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(BlankModuleTag))),
        DiagnosticCaseKind.ControlModule => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(ControlModuleTag))),
        DiagnosticCaseKind.VariableScope => ReferenceOutcome(fixture.Variable, fixture.UnsafeConstruct),
        DiagnosticCaseKind.StacktraceBound => StackOutcome(StacktraceBoundTag, fixture.Variable),
        DiagnosticCaseKind.StacktraceGuard => StackOutcome(StacktraceGuardTag, fixture.Variable),
        DiagnosticCaseKind.InvalidPattern => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.A(InvalidPatternTag))),
        DiagnosticCaseKind.UndefinedRecord => StackOutcome(UndefinedRecordTag, fixture.Variable),
        DiagnosticCaseKind.DuplicateRecord => StackOutcome(DuplicateRecordTag, fixture.Variable),
        DiagnosticCaseKind.RecordWildcardUpdate => StackOutcome(RecordWildcardUpdateTag, fixture.Variable),
        DiagnosticCaseKind.UndefinedRecordField => RecordFieldOutcome(UndefinedRecordFieldTag, fixture),
        DiagnosticCaseKind.DuplicateRecordField => RecordFieldOutcome(DuplicateRecordFieldTag, fixture),
        _ => throw new NotSupportedException()
    };

    private static Term RecordFieldOutcome(string tag, CompiledDiagnosticCase fixture) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.Tuple(Term.A(tag), Term.A(fixture.Variable), Term.A(fixture.UnsafeConstruct!)))
    );

    private static Term StackOutcome(string tag, string variable) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.List(Term.Tuple(Term.A(tag), Term.A(variable))));

    public static string Message(string variable) => $"Unbound or unsafe variable '{variable}'";

    public static Term ReferenceOutcome(string variable, string? unsafeConstruct = null) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(unsafeConstruct is null
            ? Term.Tuple(Term.A(UnboundVariableTag), Term.A(variable))
            : Term.Tuple(Term.A(UnsafeVariableTag), Term.A(variable), Term.Tuple(Term.A(unsafeConstruct), Term.I(1))))
    );
}
