namespace Erlang;

internal static class DebugDispatchNative432
{
    internal static bool TryCapture(
        ProcessRuntime? runtime,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (runtime is null || IntPtr.Size != sizeof(ulong) || module != DebugDispatchNames432.Module || !DebugDispatchNames432.Signatures.Contains((function, arity))
            || BeamRegisterInterpreter.ManagedDebugCodeProfile427 is null) return false;
        body = (context, arguments) => Invoke(
            runtime,
            context,
            function,
            arity,
            arguments
        );

        return true;
    }

    private static async ValueTask<Term> Invoke(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        int arity,
        IReadOnlyList<Term> arguments
    )
    {
        runtime.RequireDebugDispatch432(context);
        if (arguments.Count != arity) throw new ArgumentException(DebugDispatchNames432.Arity);
        if (function == DebugDispatchNames432.Instructions) return BeamRegisterInterpreter.ActiveInstructions432();
        try
        {
            if (arguments[1] is not Atom flag || flag.Name is not (ErlangBooleanAtoms.True or ErlangBooleanAtoms.False))
                throw new ErlangException(Term.A(ErlangErrorReasons.BadArgument));
            var pattern = DebugBreakpointPattern432.Parse(arguments[0]);

            return await runtime.Modules.SetDebugBreakpoint432(context, pattern, flag.Name == ErlangBooleanAtoms.True);
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                DebugDispatchNames432.Module,
                function,
                arguments
            );
        }
    }
}
