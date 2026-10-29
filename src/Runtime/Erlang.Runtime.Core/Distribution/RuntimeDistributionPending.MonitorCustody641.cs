namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void AddConnectedSpawnRequest641(DistributionSpawnRequest request)
    {
        try
        {
            connectedSpawnRequests.Add(request.Reference, request);
        }
        catch
        {
            request.MonitorCustody641?.Close641();
            request.OwnerExitCustody641?.Close641();
            throw;
        }
    }

    private bool RemoveConnectedSpawnRequest641(ReferenceTerm reference)
        => RemoveConnectedSpawnRequest641(reference, out _);

    private bool RemoveConnectedSpawnRequest641(ReferenceTerm reference, out DistributionSpawnRequest? request)
    {
        if (!connectedSpawnRequests.Remove(reference, out request))
            return false;
        request.MonitorCustody641?.Close641();
        request.OwnerExitCustody641?.Close641();

        return true;
    }

    private void ClearConnectedSpawnRequests641()
    {
        foreach (var request in connectedSpawnRequests.Values)
        {
            request.MonitorCustody641?.Close641();
            request.OwnerExitCustody641?.Close641();
        }
        connectedSpawnRequests.Clear();
    }
}
