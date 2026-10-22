namespace Erlang;

internal static class ManagedCollectorNative413
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || !owner.HasManagedCollectorProfile413
            || !ManagedCollectorNames413.Signatures.Contains((module, function, arity)))
        {
            body = null!;

            return false;
        }
        body = new ManagedCollectorBinding413(
            owner,
            module,
            function,
            arity
        ).Body;

        return true;
    }
}
