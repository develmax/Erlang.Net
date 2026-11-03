namespace Erlang.NifNativeBackend;

public static class OrdinaryBinaryWritableChecks
{
    // Native producer replaces the earlier explicitly conservative refusal.
    // Same permanent entry now proves real Source flags/capacity/alias/GC state.
    public static Task PreserveWriterCapacityAndAliases() => NativeWritableChecks.SourceLifecycle();
}
