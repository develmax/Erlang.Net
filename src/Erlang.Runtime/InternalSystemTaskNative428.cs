namespace Erlang;

internal static class InternalSystemTaskNative428
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || module != SystemTaskNames428.Module || (function, arity) is not
            ((SystemTaskNames428.Request, 3) or (SystemTaskNames428.Request, 4)
             or (SystemTaskNames428.CheckDirty, 2) or (SystemTaskNames428.HandleSignals, 1))) return false;
        body = async (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity)
                throw new InvalidOperationException(SystemTaskNames428.Foreign);
            try
            {
                if (function == SystemTaskNames428.HandleSignals)
                {
                    Term result = owner.HandleDirtySignals428(context, arguments[0], out int work);
                    // Actual bounded admission work is charged only after both
                    // target ownership leases have been released; never await a
                    // caller yield while holding another process's control owner.
                    if (work != 0) await context.ChargeAsync(work);

                    return result;
                }

                return function switch
                {
                    SystemTaskNames428.Request => owner.RequestSystemTask428(context, arguments),
                    SystemTaskNames428.CheckDirty => owner.CheckDirtyProcessCode428(context, arguments[0], arguments[1]),
                    _ => throw new InvalidOperationException(SystemTaskNames428.Foreign)
                };
            }
            catch (ErlangException error)
            {
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    arguments
                );
            }
        };

        return true;
    }
}
