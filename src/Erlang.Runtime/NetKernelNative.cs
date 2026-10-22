namespace Erlang;

internal static class NetKernelNative
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is not null && module == NetKernelNativeNames.Module &&
            function == NetKernelNativeNames.UnicodeIo && arity == 1)
        {
            body = (context, arguments) =>
            {
                if (!ReferenceEquals(context.Runtime, owner))
                    throw new InvalidOperationException(NetKernelNativeNames.ForeignRuntime);
                context.Cancellation.ThrowIfCancellationRequested();

                return ValueTask.FromResult(owner.DflagUnicodeIo(arguments[0]));
            };

            return true;
        }
        body = null!;

        return false;
    }
}
