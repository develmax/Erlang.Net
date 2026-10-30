namespace Erlang;

internal static partial class NativeResourceRoots524
{
    internal static IDisposable? RetainGraph524(
        Term source,
        ProcessRuntime? expected = null,
        bool skipOwnedMagic576 = false,
        bool hostAtomics586 = false
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
                hostAtomics586
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
