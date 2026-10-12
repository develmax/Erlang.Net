namespace Erlang;

internal sealed class ModuleCodePurgeState
{
    public bool Retired;
    public TaskCompletionSource? Pending;
}
