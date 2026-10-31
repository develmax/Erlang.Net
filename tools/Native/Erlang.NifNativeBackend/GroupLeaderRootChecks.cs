namespace Erlang.NifNativeBackend;

public static class GroupLeaderRootChecks
{
    public static async Task StorageAndSnapshot()
    {
        foreach (bool owned in new[] { false, true })
        {
            Pid? host = null;
            Pid? stale = null;
            await using (var runtime = NativeOwned516Profile.Create(owned))
            {
                await CpuStatisticsAccess.Actor<Term>(
                    runtime,
                    context =>
                {
                    var expected = Foreign();
                    Require(runtime.SetGroupLeader(expected, context.Self));
                    Pid actual = stale = context.GroupLeader;
                    ProcessTermSequence506 block = Block(actual);
                    Require(ReferenceEquals(block.Owner, context.TermHeap506) && actual.Equals(expected));
                    Require(runtime.SetGroupLeader(actual, context.Self) && ReferenceEquals(context.GroupLeader, actual));
                    host = runtime.GetGroupLeader(context);
                    Require(host.Equals(expected) && ExternalIdentifierTestAccess.Storage(host) is null);
                    Require(context.TermHeap506.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                    Require(context.GroupLeader.Equals(expected));
                    Require(runtime.SetGroupLeader(context.Self, context.Self));
                    var collected = context.TermHeap506.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold);
                    Require(collected.ReclaimedAllocationWords567 == GroupLeaderRootCaseNames.ExternalPidWords);
                    Require(ReferenceEquals(context.GroupLeader, context.Self));
                    RequireStale(actual);

                    return ValueTask.FromResult<Term>(Nil.Value);
                }
                );
            }
            Require(host is not null && host.Node == GroupLeaderRootCaseNames.Node && host.Id == GroupLeaderRootCaseNames.Id
                && host.Creation == GroupLeaderRootCaseNames.Creation);
            RequireStale(stale!);
            GC.KeepAlive(stale);
        }
    }

    public static async Task InheritanceAndClose()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            Pid? escaped = null;
            await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async parent =>
            {
                Require(runtime.SetGroupLeader(Foreign(), parent.Self));
                Pid first = parent.GroupLeader;
                ProcessTermSequence506 parentBlock = Block(first);
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                ProcessContext? childContext = null;
                ProcessTermSequence506? childBlock = null;
                Exception? childFailure = null;
                ProcessHandle child = runtime.Spawn(
                    async context =>
                {
                    try
                    {
                        childContext = context;
                        Pid value = context.GroupLeader;
                        childBlock = Block(value);
                        Require(!ReferenceEquals(parentBlock, childBlock) && ReferenceEquals(childBlock.Owner, context.TermHeap506));
                        Require(value.Equals(first));
                        NativeOffheapHeader576 parentNode, childNode;
                        lock (parent.TermHeap506.Gate) parentNode = parent.TermHeap506.RequireNativeOffheapHeader576(parentBlock, NativeOffheapKind576.ExternalPid);
                        lock (context.TermHeap506.Gate) childNode = context.TermHeap506.RequireNativeOffheapHeader576(childBlock, NativeOffheapKind576.ExternalPid);
                        Require(parentNode.Binary == childNode.Binary && childNode.ExternalNode!.Read().References == 2);
                        escaped = runtime.GetGroupLeader(context);
                        ready.TrySetResult();
                        await release.Task;
                        Require(context.GroupLeader.Equals(Foreign()));
                        Require(context.TermHeap506.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == 0);
                    }
                    catch (Exception error)
                    {
                        childFailure = error;
                        ready.TrySetException(error);
                    }

                    return Nil.Value;
                },
                    parent
                );
                try
                {
                    await ready.Task.WaitAsync(GroupLeaderRootCaseNames.Timeout);
                    Require(runtime.SetGroupLeader(parent.Self, parent.Self));
                    Require(parent.TermHeap506.CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedAllocationWords567 == GroupLeaderRootCaseNames.ExternalPidWords);
                    RequireStale(first);
                }
                finally
                {
                    release.TrySetResult();
                    await child.Completion.WaitAsync(GroupLeaderRootCaseNames.Timeout);
                    if (childContext is not null) await childContext.Unwound.Task.WaitAsync(GroupLeaderRootCaseNames.Timeout);
                }
                if (childFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(childFailure).Throw();
                Require(childBlock is not null && escaped is not null && escaped.Equals(Foreign())
                    && ExternalIdentifierTestAccess.Storage(escaped) is null);

                return Nil.Value;
            }
            );
            Require(escaped is not null && escaped.Equals(Foreign()));
        }
    }

    private static Pid Foreign() => new(GroupLeaderRootCaseNames.Node, GroupLeaderRootCaseNames.Id, GroupLeaderRootCaseNames.Creation);

    private static ProcessTermSequence506 Block(Pid value)
    {
        Require(ExternalIdentifierTestAccess.Storage(value) is ProcessTermSequence506);
        var block = (ProcessTermSequence506)ExternalIdentifierTestAccess.Storage(value)!;
        Require(block.AllocationWords526 == GroupLeaderRootCaseNames.ExternalPidWords && block.Count == 0
            && block.Slab.Cells.ReadHeader526(block.AllocationOffset526) == GroupLeaderRootCaseNames.ExternalPidHeader
            && block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + 3) == unchecked((nuint)GroupLeaderRootCaseNames.Id));

        return block;
    }

    private static void RequireStale(Pid value)
    {
        bool refused = false;
        try
        {
            _ = value.Node;
        }
        catch (ObjectDisposedException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(GroupLeaderRootCaseNames.Failure);
    }
}
