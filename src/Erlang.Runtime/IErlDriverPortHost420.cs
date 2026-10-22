namespace Erlang;

// Required actual parent VM operations. A provider cannot be composed without
// the real port registry/backpressure/budget/DDLL implementation behind these.
internal interface IErlDriverPortHost420
{
    // Publish/stage actual VM exit only; child stop runs after callback return.
    int Failure(
        ErlDriverPortIdentity420 port,
        Term reason,
        bool eof,
        bool unlink
    );

    void SetBusy(ErlDriverPortIdentity420 port, bool busy);

    ErlDriverBusyLimits420 BusyLimits(ErlDriverPortIdentity420 port, ulong? low, ulong? high);

    int ConsumeTimeslice(ErlDriverPortIdentity420 port, int percent);

    int LockDriver(ErlDriverPortIdentity420 port);
}
