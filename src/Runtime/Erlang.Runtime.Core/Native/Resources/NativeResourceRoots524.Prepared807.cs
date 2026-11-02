namespace Erlang;

internal static partial class NativeResourceRoots524
{
    private static IDisposable? RetainGraphBeforeOrdinaryBinary(
        Term source,
        ProcessRuntime? expected = null,
        bool skipOwnedMagic576 = false,
        bool hostAtomics586 = false,
        List<NativeResourceRootLease524>? reservations172 = null
    )
    {
        IDisposable? prepared = PreparedLoaderRoots807.RetainGraph807(
            source,
            expected,
            skipOwnedMagic576,
            hostAtomics586
        );
        IDisposable? existing = null;
        try
        {
            existing = RetainGraphBeforePrepared807(
                source,
                expected,
                skipOwnedMagic576,
                hostAtomics586,
                reservations172
            );
            if (prepared is null) return existing;
            if (existing is null) return prepared;

            return new NativeResourceRootSet524([prepared, existing]);
        }
        catch (Exception primary)
        {
            Exception? first = null, second = null;
            try
            {
                existing?.Dispose();
            }
            catch (Exception error)
            {
                first = error;
            }
            try
            {
                prepared?.Dispose();
            }
            catch (Exception error)
            {
                second = error;
            }
            if (first is not null || second is not null)
                throw new AggregateException(new[] { primary, first, second }.OfType<Exception>());
            throw;
        }
    }
}
