namespace Erlang;

internal sealed class DdllParentRow422
{
    internal DdllParentRow422(
        ulong identity,
        ErlDriverName420 name,
        ErlDriverPath420? path,
        bool linked,
        ulong ownerImage,
        bool killPorts
    )
    {
        Identity = identity;
        Name = name;
        Path = path;
        Linked = linked;
        OwnerImage = ownerImage;
        KillPorts = killPorts;
        Node = new(this);
    }

    internal ulong Identity { get; set; }
    internal ErlDriverName420 Name { get; }
    internal ErlDriverPath420? Path { get; set; }
    internal bool Linked { get; }
    internal ulong OwnerImage { get; }
    internal bool KillPorts { get; set; }
    internal DdllParentStateKind422 State;
    internal LinkedListNode<DdllParentRow422> Node { get; }
    internal LinkedList<DdllParentProcessReference422> Processes { get; } = new();
    internal HashSet<ulong> Ports { get; } = new();
    internal ErlDriverPath420? ReloadPath;
    internal bool ReloadKillPorts;
    internal bool RetirementQueued;
    internal uint NativeFlags;
}
