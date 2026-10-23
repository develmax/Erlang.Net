namespace Erlang;

internal sealed partial class RuntimeEtsStoredPayload
{
    private RuntimeEtsStoredPayload(RuntimeEtsCompressed438 compressed)
    {
        compressed438 = compressed;
        ArrayPayloadBytes = compressed.ArrayPayloadBytes;
    }

    internal Term MaterializeKey438(long position, CancellationToken cancellation)
    {
        RuntimeEtsCompressed438? compressed = Volatile.Read(ref compressed438);
        if (compressed is not null)
        {
            if (position != compressed.KeyPosition) throw new InvalidDataException(EtsCompressedNames438.Invalid);

            return MaterializeNodes438(compressed.KeyNodes, 0, cancellation);
        }
        var owned = Volatile.Read(ref nodes) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredPayload));
        if (owned[0].Kind != RuntimeEtsStoredKind.Tuple || owned[0].Children is not { } fields
            || position < 1 || position > fields.Length) throw new InvalidDataException(EtsCompressedNames438.Invalid);

        return MaterializeNodes438(owned, fields[(int)position - 1], cancellation);
    }
}
