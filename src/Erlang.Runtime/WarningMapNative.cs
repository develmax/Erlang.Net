namespace Erlang;

internal static class WarningMapNative
{
    private static readonly Term Warning = Term.A(WarningMapNativeNames.Warning);
    private static readonly Term Info = Term.A(WarningMapNativeNames.Info);
    private static readonly Term Error = Term.A(WarningMapNativeNames.Error);

    internal static Term Value(RuntimeWarningMapping mapping) => mapping switch
    {
        RuntimeWarningMapping.Warning => Warning,
        RuntimeWarningMapping.Info => Info,
        RuntimeWarningMapping.Error => Error,
        _ => throw new ArgumentOutOfRangeException(nameof(mapping))
    };

    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is not null && module == WarningMapNativeNames.Module && function == WarningMapNativeNames.Function && arity == 0)
        {
            body = (context, _) =>
            {
                if (!ReferenceEquals(context.Runtime, owner))
                    throw new InvalidOperationException(WarningMapNativeNames.ForeignRuntime);
                context.Cancellation.ThrowIfCancellationRequested();

                return ValueTask.FromResult(owner.ReadWarningMapping());
            };

            return true;
        }
        body = null!;

        return false;
    }
}
