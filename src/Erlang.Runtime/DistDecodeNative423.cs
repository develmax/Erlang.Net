namespace Erlang;

internal static class DistDecodeNative423
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
        if (owner is null || module != DistDecodeNames423.Module || function != DistDecodeNames423.Function || arity != DistDecodeNames423.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != DistDecodeNames423.Arity)
                throw new ArgumentException(DistDecodeNames423.Foreign);
            owner.RequireInternalActor419(context);
            try
            {
                if (arguments[0] is not TupleTerm tuple || tuple.Items.Count > DistDecodeNames423.MaximumCacheEntries)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var cache = new Atom[tuple.Items.Count];
                for (int index = 0; index < cache.Length; index++)
                    cache[index] = tuple.Items[index] as Atom ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (arguments[1] is not BitString { IsBinary: true, BitLength: > 0 } binary)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                ReadOnlySpan<byte> bytes = binary.ByteSpan;
                if (bytes.Length < EtfHeaderLayout.MinimumPacketBytes || bytes[EtfHeaderLayout.VersionOffset] != EtfHeaderLayout.Version
                    || bytes[EtfHeaderLayout.TagOffset] == EtfTags.Compressed)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                EtfDecodedTerm decoded = owner.ImportExternalTerm(
                    bytes,
                    new EtfDecodeOptions { AtomTranslationCache423 = cache, DistributionDebug423 = true },
                    Math.Max(ExternalTermFormat.DefaultMaxBytes, bytes.Length)
                );
                owner.RequireInternalActor419(context);

                return ValueTask.FromResult(decoded.Term);
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
