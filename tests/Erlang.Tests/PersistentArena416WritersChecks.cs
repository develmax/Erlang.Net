namespace Erlang.Tests;

internal static class PersistentArena416WritersChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            async Task<bool> Write(ProcessContext context, int value, TaskCompletionSource ready)
            {
                ready.TrySetResult();
                await gate.Task;
                try
                {
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        Term.A(PersistentArena416HostNames.Key),
                        Term.I(value),
                        true
                    );

                    return true;
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    return false;
                }
            }
            Task<bool> one = first.InvokeAsync(context => Write(context, 1, ready1));
            Task<bool> two = second.InvokeAsync(context => Write(context, 2, ready2));
            await Task.WhenAll(ready1.Task, ready2.Task);
            gate.TrySetResult();
            bool[] results = await Task.WhenAll(one, two);
            PersistentArena416HostAccess.Require(results.Count(value => value) == 1);
            await first.Invoke(context =>
            {
                object info = PersistentArena416HostAccess.Info(store, context);
                object allocations = PersistentArena416HostAccess.Allocations(store);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(info, PersistentArena416HostNames.Count) == 1
                    && PersistentArena416HostAccess.Number(allocations, PersistentArena416HostNames.AllocatedPairs) == 1
                    && PersistentArena416HostAccess.Number(allocations, PersistentArena416HostNames.AllocatedTables) == 1
                    && PersistentArena416HostAccess.Get(store, context, Term.A(PersistentArena416HostNames.Key)).Equals(Term.I(results[0] ? 1 : 2)));

                return true;
            });
        }
        finally
        {
            gate.TrySetResult();
            await PersistentArena416HostAccess.Close(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }
}
