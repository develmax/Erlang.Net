namespace Erlang;

internal delegate ValueTask<Term> BeamExecutionHandler432(
    ProcessContext context,
    BeamLinkedCode code,
    BeamLinkedFunction function,
    IReadOnlyList<Term> arguments,
    IReadOnlyList<Term> environment,
    BeamExecutionIdentity execution,
    SourceCodeAdmissionKind admission,
    long? budget,
    LocalFunctionInvocation? invocation,
    SourceCodeLease? admittedLease647
);
