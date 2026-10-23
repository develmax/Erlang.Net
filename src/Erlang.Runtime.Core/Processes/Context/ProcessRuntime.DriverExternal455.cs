namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal byte[] DecodeDriverExternal455(byte[] bytes)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
            // ERL_DRV_EXT2TERM follows erts_binary2term_prepare: decode one
            // complete term, even when the supplied span has trailing bytes.
            EtfDecodedTerm decoded = ImportExternalTerm(bytes);

            return EncodeDriverIdentity422(decoded.Term);
        }
    }
}
