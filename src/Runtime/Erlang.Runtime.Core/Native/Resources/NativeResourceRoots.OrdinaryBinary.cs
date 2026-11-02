namespace Erlang;

internal static partial class NativeResourceRoots524
{
    internal static IDisposable? RetainGraph524(
        Term source,
        ProcessRuntime? expected = null,
        bool skipOwnedMagic576 = false,
        bool hostAtomics586 = false
    )
        => RetainGraphCore172(
            source,
            expected,
            skipOwnedMagic576,
            hostAtomics586,
            null
        );

    internal static IDisposable? ReserveSuspensionAckGraph172(
        Term source, ProcessRuntime expected, List<NativeResourceRootLease524> reservations)
        => RetainGraphCore172(
            source,
            expected,
            false,
            false,
            reservations
        );

    private static IDisposable? RetainGraphCore172(
        Term source,
        ProcessRuntime? expected,
        bool skipOwnedMagic576,
        bool hostAtomics586,
        List<NativeResourceRootLease524>? reservations172
    )
    {
        IDisposable? ordinary = OrdinaryBinaryRoots.Retain(source, expected, skipOwnedMagic576);
        IDisposable? existing = null;
        try
        {
            existing = RetainGraphBeforeOrdinaryBinary(
                source,
                expected,
                skipOwnedMagic576,
                hostAtomics586,
                reservations172
            );
            if (ordinary is null) return existing;
            if (existing is null) return ordinary;

            return new NativeResourceRootSet524([ordinary, existing]);
        }
        catch (Exception original)
        {
            List<Exception>? cleanup = null;
            try
            {
                existing?.Dispose();
            }
            catch (Exception error)
            {
                (cleanup ??= new()).Add(error);
            }
            try
            {
                ordinary?.Dispose();
            }
            catch (Exception error)
            {
                (cleanup ??= new()).Add(error);
            }
            if (cleanup is not null) throw new AggregateException(new[] { original }.Concat(cleanup));
            throw;
        }
    }
}
