namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Dictionary<ProcessContext, HashSet<StatisticsTrapRequest>>? statisticsTrapRequests;

    internal void RegisterStatisticsTrap(ProcessContext caller, StatisticsTrapRequest request)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(caller);
            caller.Cancellation.ThrowIfCancellationRequested();
            if (!RuntimeActorExecutionTurn.IsOwnedBy(caller))
                throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
            statisticsTrapRequests ??= new();
            if (!statisticsTrapRequests.TryGetValue(caller, out var pending))
                statisticsTrapRequests.Add(caller, pending = new());
            if (!pending.Add(request))
                throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        }
    }

    internal void SettleStatisticsTrap(ProcessContext caller, StatisticsTrapRequest request)
    {
        lock (gate)
        {
            if (statisticsTrapRequests is null || !statisticsTrapRequests.TryGetValue(caller, out var pending)
                || !pending.Remove(request))
                throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
            if (pending.Count == 0) statisticsTrapRequests.Remove(caller);
            request.CompleteRetirement();
        }
    }

    private Task StatisticsTrapProcessRetirement(ProcessContext caller, Task previous)
    {
        lock (gate)
        {
            if (statisticsTrapRequests is null || !statisticsTrapRequests.TryGetValue(caller, out var pending))
                return previous;

            return Task.WhenAll(pending.Select(request => request.Retirement).Append(previous));
        }
    }

    private bool StatisticsTrapRetirementCompleted(ProcessContext caller)
        => statisticsTrapRequests is null || !statisticsTrapRequests.ContainsKey(caller);

    private Task DrainStatisticsTraps()
    {
        lock (gate)
            return statisticsTrapRequests is null ? Task.CompletedTask
                : Task.WhenAll(statisticsTrapRequests.Values.SelectMany(pending => pending).Select(request => request.Retirement));
    }
}
