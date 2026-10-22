namespace Erlang;

internal sealed partial class RuntimePorts
{
    internal PortTerm ReserveDriverIdentity422()
    {
        lock (gate)
        {
            runtime.ThrowIfHalted();
            ulong next = checked(nextId + 1);
            var identity = new PortTerm(runtime.LocalIdentity, next);
            nextId = next;

            return identity;
        }
    }
}
