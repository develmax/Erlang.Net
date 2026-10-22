namespace Erlang;

internal interface IErlDriverPortTaskParent420
{
    // Must run the callback in a real normal dispatcher/auxiliary lane turn,
    // and retain that scheduling permit until the delegated call completes.
    Task Dispatch(ErlDriverPortIdentity420 port, Func<Task> callback);
}
