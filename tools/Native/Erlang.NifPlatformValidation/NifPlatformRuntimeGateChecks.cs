internal static class NifPlatformRuntimeGateChecks
{
    // Retained for callers of the previous validation entry point.
    internal static Task Run() => NativeWordStorageChecks.Run();
}
