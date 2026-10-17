namespace Erlang.Compiler;

internal static class RuntimeCallFrames
{
    internal static ErlangException Capture(
        ProcessContext context,
        ErlangException exception,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        if (module == ErlangModuleNames.Module && function == ErlangBasicBifNames.Apply &&
            arguments.Count == ErlangApplyLayout.ModuleApplyArity && exception.StackTraceTerm is Cons)
            return exception;

        return context.CaptureCallFailure(
            exception,
            module,
            function,
            arguments
        );
    }
}
