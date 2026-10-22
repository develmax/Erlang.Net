namespace Erlang;

internal sealed class DirtyNativeSiteScope419(ProcessContext owner, BeamNativeCallSite419? previous) : IDisposable
{
    public void Dispose() => owner.RestoreNativeCallSite419(previous);
}
