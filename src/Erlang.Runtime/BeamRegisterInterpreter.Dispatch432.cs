namespace Erlang;

internal static partial class BeamRegisterInterpreter
{
    // This is the actual selected entry, not a second catalogue of parsed opcodes.
    // Every external/local-function entry uses this exact delegate below.
    private static readonly BeamExecutionHandler432 executionHandler432 = ExecuteCore;

    private static ValueTask<Term> Dispatch432(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        SourceCodeAdmissionKind admission,
        long? budget,
        LocalFunctionInvocation? invocation = null
    )
        => executionHandler432(
            context,
            code,
            function,
            arguments,
            environment,
            execution,
            admission,
            budget,
            invocation
        );

    internal static Term ActiveInstructions432()
    {
        DebugCodeProfile427? profile = ManagedDebugCodeProfile427;
        if (profile is null || !executionHandler432.Method.Equals(profile.Entry))
            throw new NotSupportedException(DebugDispatchNames432.Profile);

        return Cons.From(new[] { Term.String(executionHandler432.Method.Name) });
    }
}
