namespace Erlang;

internal static class IoPrintableRangeNative
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is not null && module == IoPrintableRangeNativeNames.Module
            && function == IoPrintableRangeNativeNames.Function && arity == 0)
        {
            body = (context, _) =>
            {
                if (!ReferenceEquals(context.Runtime, owner))
                    throw new InvalidOperationException(IoPrintableRangeNativeNames.ForeignRuntime);
                context.Cancellation.ThrowIfCancellationRequested();

                return ValueTask.FromResult(owner.ReadPrintableRange());
            };

            return true;
        }
        body = null!;

        return false;
    }
}
