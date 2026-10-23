namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    private Term DecodeExternal455(ReadOnlyMemory<byte> bytes)
    {
        // ERL_DRV_EXT2TERM can contain LOCAL_EXT (prim_inet's send monitor).
        // Only the parent owns the current node epoch and local checksum seed.
        // Decode there, then import its ordinary ETF representation here.
        var reply = peer.RequestAsync(
            ErlDriverHostOperation420.DecodeExternal455,
            0,
            0,
            0,
            0,
            null,
            bytes.ToArray(),
            CancellationToken.None
        )
            .AsTask().GetAwaiter().GetResult();
        if (reply.NativeResult == -1) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (reply.NativeResult != 0 || reply.Data is null)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return ExternalTermFormat.Decode(reply.Data);
    }
}
