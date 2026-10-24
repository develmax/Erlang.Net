namespace Erlang.Tests;

internal static class ModuleLoadingHostParameters
{
    public static readonly TimeSpan WorkerDeadline = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan BlockedReaderObservation = TimeSpan.FromMilliseconds(100);
}
