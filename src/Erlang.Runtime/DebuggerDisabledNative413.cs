namespace Erlang;

internal static class DebuggerDisabledNative413
{
    // Source branch is runtime enabled-bit false, equally present in JIT and emu.
    // Every non-supported BIF checks it before inspecting any guest argument.
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || !owner.HasDisabledDebugger413 || module != DebuggerDisabledNames413.Module
            || !DebuggerDisabledNames413.Signatures.Contains((function, arity)))
        {
            body = null!;

            return false;
        }
        body = (context, arguments) =>
        {
            owner.RequireDisabledDebuggerActor413(context);
            if (arguments.Count != arity) throw new ArgumentException(DebuggerDisabledNames413.Foreign);
            if (function == DebuggerDisabledNames413.Supported)
                return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.False));
            throw context.CaptureNativeFailure(
                new ErlangException(ErlangErrorReasons.UndefinedFunction),
                DebuggerDisabledNames413.Module,
                function,
                arguments
            );
        };

        return true;
    }
}
