namespace Erlang;

internal interface IErlDriverMonitorHost420
{
    ErlDriverMonitorRegistration420 Register(
        ErlDriverPortIdentity420 port,
        ulong attempt,
        ulong targetActor,
        Pid target
    );

    int Unregister(ErlDriverPortIdentity420 port, ulong attempt);
}
