using System.Numerics;

namespace Erlang;

internal sealed class RuntimeMicrostateRecord(
    RuntimeMicrostateAccounting owner,
    string type,
    int identity,
    RuntimeMicrostateThreadIdentity physicalThread
)
{
    internal RuntimeMicrostateAccounting Owner { get; } = owner;
    internal string Type { get; } = type;
    internal int Identity { get; } = identity;
    internal RuntimeMicrostateThreadIdentity PhysicalThread { get; } = physicalThread;
    internal BigInteger[] Counters { get; } = new BigInteger[RuntimeMicrostateNames.States.Length];
    internal RuntimeMicrostate State = RuntimeMicrostate.Other;
    internal BigInteger LastCounter;
    internal bool Active;
    internal bool Managed => Type == RuntimeMicrostateNames.Scheduler;
    internal bool ManagedAttached;
}
