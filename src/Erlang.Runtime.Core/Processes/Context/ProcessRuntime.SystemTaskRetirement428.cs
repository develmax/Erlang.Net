namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Task RetireSystemTaskOwner428(ProcessContext target, ProcessSystemTaskOwner428 owner)
    {
        if (!owner.RetirementStarted)
        {
            // Queue allocation precedes publication of the closing transition.
            // Worker takes runtime.gate before it reads the actual owner state.
            _ = SystemTaskFlow428.Start(() => RunSystemTaskRetirement428(target, owner));
            owner.RetirementStarted = true;
            owner.Closing = true;
            target.SetSystemTaskPriority428(-1);
            actorDispatcher.PriorityChanged(target);
        }

        return owner.Retired.Task;
    }

    private async Task RunSystemTaskRetirement428(ProcessContext target, ProcessSystemTaskOwner428 owner)
    {
        try
        {
            while (true)
            {
                Task? physical = null;
                bool done = false;
                for (int step = 0; step < SystemTaskNames428.SignalBudget; step++)
                {
                    lock (gate)
                    {
                        var node = owner.Pending.First;
                        if (node is not null && ReferenceEquals(node.Value, owner.Active)) node = node.Next;
                        if (node is null)
                        {
                            physical = owner.Active?.PhysicalDone.Task;
                            done = physical is null;
                            break;
                        }
                        var request = node.Value;
                        owner.Pending.Remove(node);
                        if (request.ReadyNode.List is { } queue) queue.Remove(request.ReadyNode);
                        try
                        {
                            ReplySystemTask428(request, false);
                        }
                        catch (Exception error)
                        {
                            owner.Failures.Add(error);
                        }
                        finally
                        {
                            request.Clear();
                            request.PhysicalDone.TrySetResult();
                        }
                    }
                }
                if (done) break;
                if (physical is not null) await physical.ConfigureAwait(false);
                else await Task.Yield();
            }
        }
        catch (Exception error)
        {
            lock (gate) owner.Failures.Add(error);
        }
        finally
        {
            lock (gate)
            {
                if (systemTaskOwners428.TryGetValue(target, out var current) && ReferenceEquals(current, owner))
                    systemTaskOwners428.Remove(target);
                target.SetSystemTaskPriority428(-1);
                actorDispatcher.PriorityChanged(target);
                if (owner.Failures.Count == 0) owner.Retired.TrySetResult();
                else owner.Retired.TrySetException(owner.Failures);
            }
        }
    }

    private Task SystemTaskProcessRetirement428(ProcessContext target, Task previous)
    {
        if (!systemTaskOwners428.TryGetValue(target, out var owner)) return previous;

        return Task.WhenAll(previous, RetireSystemTaskOwner428(target, owner));
    }

    private Task CloseSystemTasks428()
    {
        CloseDirtySystemHandlers428();
        foreach (var pair in systemTaskOwners428) RetireSystemTaskOwner428(pair.Key, pair.Value);
        Task owners = Task.WhenAll(systemTaskOwners428.Values.Select(owner => owner.Retired.Task));

        return systemTaskServiceFailures428.Count == 0 ? owners :
            Task.WhenAll(owners, Task.FromException(new AggregateException(systemTaskServiceFailures428)));
    }

    private Task DrainSystemTasks428()
    {
        lock (gate) return CloseSystemTasks428();
    }
}
