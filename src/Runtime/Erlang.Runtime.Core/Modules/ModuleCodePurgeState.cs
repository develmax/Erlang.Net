namespace Erlang;

internal sealed class ModuleCodePurgeState
{
    public SourcePurgeFenceSession? Session;
    public bool Retired;
    public TaskCompletionSource? Pending;
}
