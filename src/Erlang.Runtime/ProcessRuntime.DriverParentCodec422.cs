namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal byte[] EncodeDriverIdentity422(Term value)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
            LocalNodeSnapshot wire = LocalIdentity.Snapshot;

            return ExternalTermFormat.Encode(
                value,
                new EtfEncodingOptions
                {
                    LocalNode = wire.Node,
                    LocalCreation = wire.Creation,
                    NodeIdentity = LocalIdentity,
                    NodeSnapshot = wire,
                    LocalSeed = LocalTermSeed,
                    FunctionOwner = InitializationPid
                }
            );
        }
    }
}
