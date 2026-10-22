namespace Erlang;

internal sealed partial class PersistentArenaStore416
{
    private bool ownerAdmissionClosed417;

    // Constant state mutation after acquiring the store gate. Acquiring that gate
    // can still wait for existing O(capacity) metadata work; no latency claim.
    internal void CloseOwnerAdmission417()
    {
        lock (gate) ownerAdmissionClosed417 = true;
    }

    internal PersistentArenaOwnerState417 OwnerState417()
    {
        lock (gate)
            return new(
                ownerAdmissionClosed417 || current is null ? null : current.Info(),
                ownerAdmissionClosed417 || current is null,
                accounting.Snapshot()
            );
    }
}
