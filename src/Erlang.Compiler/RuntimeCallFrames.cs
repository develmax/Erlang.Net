namespace Erlang.Compiler;

internal static class RuntimeCallFrames
{
    internal static ErlangException Capture(
        ErlangException exception,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        if (module == ErlangModuleNames.Module && function == ErlangBasicBifNames.Apply &&
            arguments.Count == ErlangApplyLayout.ModuleApplyArity && exception.StackTraceTerm is Cons)
            return exception;

        return exception.WithStackFrame(module, function, arguments);
    }
}
