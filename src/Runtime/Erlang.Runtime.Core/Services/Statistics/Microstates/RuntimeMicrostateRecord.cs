using System.Numerics;

namespace Erlang;

internal sealed class RuntimeMicrostateRecord(
    RuntimeMicrostateAccounting owner,
    string type,
    int identity,
    RuntimeMicrostateThreadIdentity physicalThread,
    bool managed
)
{
    internal RuntimeMicrostateRecord(
        RuntimeMicrostateAccounting owner,
        string type,
        int identity,
        RuntimeMicrostateThreadIdentity physicalThread
    ) : this(
        owner,
        type,
        identity,
        physicalThread,
        type == RuntimeMicrostateNames.Scheduler
    )
    {
    }

    internal RuntimeMicrostateAccounting Owner { get; } = owner;
    internal string Type { get; } = type;
    internal int Identity { get; } = identity;
    internal RuntimeMicrostateThreadIdentity PhysicalThread { get; } = physicalThread;
    internal BigInteger[] Counters { get; } = new BigInteger[RuntimeMicrostateNames.States.Length];
    internal RuntimeMicrostate State = RuntimeMicrostate.Other;
    internal BigInteger LastCounter;
    internal bool Active;
    internal bool Managed { get; } = managed;
    internal bool ManagedAttached;
}
