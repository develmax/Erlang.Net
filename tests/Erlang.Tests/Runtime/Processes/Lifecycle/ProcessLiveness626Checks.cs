using static Erlang.Tests.ProcessLiveness626Names;

namespace Erlang.Tests;

internal static class ProcessLiveness626Checks
{
    internal static async Task ClosedBeforeToken()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext child = await target.Ready.WaitAsync(Deadline);
        using var entered = new ManualResetEventSlim();
        int requested = -1;
        Task? stopping = null, observation = null;
        try
        {
            // Hold a real existing cleanup gate, not a fabricated mailbox or
            // cancellation flag. Terminate closes admission before this gate.
            lock (Field(child.Dictionary, Gate))
            {
                stopping = killer.Invoke(sender =>
                {
                    runtime.Exit(sender, child.Self, Term.A(ProcessLiveness499Names.Kill));

                    return true;
                });
                Require(SpinWait.SpinUntil(
                    () => (bool)Field(child, Terminated) && (bool)Field(child.Mailbox, Closed),
                    Deadline
                ));
                Require(!child.Cancellation.IsCancellationRequested);
                observation = Task.Run(() => ProcessLiveness499OrderingChecks.ReceiveForOrdering(
                    runtime,
                    child,
                    child.Cancellation,
                    canceled =>
                    {
                        Volatile.Write(ref requested, canceled ? 1 : 0);
                        entered.Set();
                    }
                ));
                Require(entered.Wait(Deadline));
                Require(Volatile.Read(ref requested) == 0);
            }
            await stopping.WaitAsync(Deadline);
            await observation.WaitAsync(Deadline);
            Require((await target.Process.Completion.WaitAsync(Deadline)).Equals(Term.A(ProcessLiveness499Names.Killed)));
            Require(!runtime.IsAlive(child.Self));
        }
        finally
        {
            if (stopping is not null) await stopping.WaitAsync(Deadline);
            if (observation is not null) await observation.WaitAsync(Deadline);
        }
    }

    internal static async Task LiveCancellation()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext child = await actor.Ready.WaitAsync(Deadline);
        using var unrelated = new CancellationTokenSource();
        unrelated.Cancel();
        bool visible = false;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                await ProcessLiveness499OrderingChecks.ReceiveForOrdering(runtime, context, unrelated.Token);

                return true;
            }).WaitAsync(Deadline);
        }
        catch (OperationCanceledException)
        {
            visible = true;
        }
        Require(visible && runtime.IsAlive(child.Self) && !child.Cancellation.IsCancellationRequested);
    }

    private static object Field(object owner, string name)
        => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
