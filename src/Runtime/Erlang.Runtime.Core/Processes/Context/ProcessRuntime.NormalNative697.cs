namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal IDisposable EnterNormalNative697(ProcessContext context)
        => actorDispatcher.EnterNormalNative697(context);

    internal void CloseNormalNativeAdmission697()
        => actorDispatcher.CloseNormalNative697();
}
