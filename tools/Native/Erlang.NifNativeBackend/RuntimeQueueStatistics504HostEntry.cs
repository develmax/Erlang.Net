namespace Erlang.NifNativeBackend;

public static class RuntimeQueueStatistics504HostEntry
{
    public static Task HeldDirtyQueues() => NifNativeBinaryHostChecks.Run(RuntimeQueueStatistics504HostChecks.HeldDirtyQueues);
}
