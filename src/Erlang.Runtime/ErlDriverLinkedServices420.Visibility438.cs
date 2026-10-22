namespace Erlang;

internal sealed partial class ErlDriverLinkedServices420
{
    private int visibilityDepth438;
    private ErlDriverLinkedScope438? visibilityScope438;
    private bool visibilityFailed438;

    private void EnterLinkedVisibility438()
    {
        // Called with the existing reentrant physical native-thread gate held.
        // A second independent native thread waits before contacting the parent.
        int next = checked(visibilityDepth438 + 1);
        if (visibilityDepth438 == 0)
        {
            ErlDriverLinkedScope438 scope = host.BeginInitialization438(bridge.ReadOwner());
            visibilityScope438 = scope;
            visibilityFailed438 = false;
        }
        visibilityDepth438 = next;
    }

    internal void FailLinkedVisibility438() => visibilityFailed438 = true;

    private ErlDriverLinkedScope438 CurrentScope438()
        => visibilityScope438 ?? new ErlDriverLinkedScope438(host.Session438, bridge.ReadOwner(), 0);

    private void LeaveLinkedVisibility438(bool cleanupSucceeded)
    {
        if (!cleanupSucceeded) visibilityFailed438 = true;
        if (visibilityDepth438 <= 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        if (--visibilityDepth438 != 0) return;
        ErlDriverLinkedScope438 scope = visibilityScope438
            ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        try
        {
            host.EndInitialization438(scope, !visibilityFailed438);
        }
        finally
        {
            visibilityScope438 = null;
        }
    }
}
