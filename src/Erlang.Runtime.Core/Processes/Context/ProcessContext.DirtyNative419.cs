namespace Erlang;

public sealed partial class ProcessContext
{
    private readonly AsyncLocal<BeamNativeCallSite419?> nativeSite419 = new();
    internal BeamNativeCallSite419? ScheduledNativeSite419 => nativeSite419.Value;

    internal IDisposable EnterBeamNativeCallSite419(BeamLinkedCode code, int instruction)
    {
        var previous = nativeSite419.Value;
        nativeSite419.Value = executionFrame.Value is { } frame ? BeamNativeCallSites419.Capture(code, instruction, frame) : null;

        return new DirtyNativeSiteScope419(this, previous);
    }

    internal IDisposable? EnterScheduledNativeSource419(BeamNativeCallSite419? site)
    {
        if (site is null) return null;
        var previous = executionFrame.Value;
        var pending = site.Frame with { ContinuationIdentity = site.Return };
        executionFrame.Value = site.Frame with { Parent = pending, ContinuationIdentity = site.Entry };
        Volatile.Write(ref visibleExecutionFrame, executionFrame.Value);

        return new ExecutionFrameScope(this, previous);
    }

    internal void RestoreNativeCallSite419(BeamNativeCallSite419? previous) => nativeSite419.Value = previous;
}
