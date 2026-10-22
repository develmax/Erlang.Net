namespace Erlang;

// Ownership transfer is explicit when startup cleanup cannot prove that the
// actual created process exited. Parent must retain its dispatcher/environment
// leases until Child.PhysicalExit succeeds and observe PhysicalCleanup too.
internal sealed class ErlDriverHostStartupFailure423 : Exception
{
    internal ErlDriverHostStartupFailure423(ErlDriverHostChild420 child, Exception startup, Exception cleanup)
        : base(ErlDriverPhysicalExitNames423.Unsettled, new AggregateException(startup, cleanup))
    {
        Child = child;
    }

    internal ErlDriverHostChild420 Child { get; }
}
