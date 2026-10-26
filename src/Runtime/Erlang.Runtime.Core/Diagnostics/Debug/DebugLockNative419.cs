namespace Erlang;

internal static class DebugLockNative419
{
    // Exact !ERTS_ENABLE_LOCK_COUNT branches, erl_bif_info.c6302-6416.
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || !owner.HasDisabledLockCounting419 || module != DebugLockNames419.Module || !DebugLockNames419.Signatures.Contains((function, arity))) return false;
        body = (context, arguments) =>
        {
            owner.RequireDebugLocksActor419(context);
            if (arguments.Count != arity) throw new ArgumentException(DebugLockNames419.Foreign);
            if (function == DebugLockNames419.Control && arity == 2)
                return owner.ScheduleDirtyNative419(
                    context,
                    DirtyNativeKind419.Cpu,
                    module,
                    function,
                    arguments,
                    static (_, _) => throw new ErlangException(ErlangErrorReasons.BadArgument)
                );
            if (function == DebugLockNames419.Control)
                throw context.CaptureNativeFailure(
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    module,
                    function,
                    arguments
                );

            return owner.ScheduleDirtyNative419(
                context,
                DirtyNativeKind419.Cpu,
                module,
                function,
                arguments,
                static (_, _) => Term.A(DebugLockNames419.Error)
            );
        };

        return true;
    }
}
