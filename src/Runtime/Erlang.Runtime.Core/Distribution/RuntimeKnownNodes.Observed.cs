namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    internal void CloseObservedIdentities()
    {
        lock (gate)
        {
            observedIdentitiesClosed = true;
            StopPhysicalNodeAdmission();
            observed.Clear();
        }
    }
}
