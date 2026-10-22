namespace Erlang;

public sealed partial class ProcessRuntime
{
    private static readonly AsyncLocal<Action<string, ProcessRuntime, ProcessContext?>?> libraryStartupObserver = new();
}
