namespace Erlang.NifFixtureHost;

internal sealed class NifGenerationState(object owner, Action unload)
{
    internal object Owner { get; } = owner;
    internal Action Unload { get; } = unload;
    internal int Active
    {
        get; set;
    }
    internal bool Retired
    {
        get; set;
    }
    internal bool Unloaded
    {
        get; set;
    }
}
