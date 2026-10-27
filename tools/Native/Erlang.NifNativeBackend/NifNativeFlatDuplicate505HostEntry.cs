namespace Erlang.NifNativeBackend;

public static class NifNativeFlatDuplicate505HostEntry
{
    public static Task MaterializedSameReferenceRows()
        => NifNativeBinaryHostChecks.Run(NifNativeFlatDuplicate505HostChecks.MaterializedSameReferenceRows);

    public static Task FactoryRowsAndPeerEquality()
        => NifNativeBinaryHostChecks.Run(NifNativeFlatDuplicate505HostChecks.FactoryRowsAndPeerEquality);
}
