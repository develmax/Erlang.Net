using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.SchedulerRetirement607Names;

namespace Erlang.Tests;

internal static class SchedulerRetirement607Checks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        using var releaseBody = new ManualResetEventSlim();
        using var releaseRetirement = new ManualResetEventSlim();
        var bodyEntered = SchedulerControlCalls.Signal();
        var retirementEntered = SchedulerControlCalls.Signal();
        object? graph = null;
        IDisposable? publication = null;
        var probe = new RetirementProbe(retirementEntered, releaseRetirement);
        try
        {
            var blocked = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.Second,
                context =>
            {
                bodyEntered.TrySetResult();
                SchedulerControlCalls.Require(releaseBody.Wait(SchedulerControlCalls.Deadline, context.Cancellation));

                return ValueTask.FromResult<Term>(Nil.Value);
            }
            );
            ProcessContext owner = await blocked.Context();
            await bodyEntered.Task.WaitAsync(SchedulerControlCalls.Deadline);
            object roots = Property(runtime, MailboxOpaqueRemove601Names.Owner);
            graph = Call(
                roots,
                MailboxOpaqueRemove601Names.Prepare,
                runtime,
                probe
            )!;
            Type scope = typeof(ProcessRuntime).Assembly.GetType(MailboxOpaqueRemove601Names.Scope, true)!;
            publication = (IDisposable)scope.GetMethod(MailboxOpaqueRemove601Names.Forward, Members)!
                .Invoke(null, [runtime, graph])!;
            typeof(Mailbox).GetMethod(MailboxOpaqueRemove601Names.Send, Members)!
                .Invoke(owner.Mailbox, [Term.I(1), null, false, false, null, false, null, publication]);
            publication.Dispose();
            publication = null;
            await ((IAsyncDisposable)graph).DisposeAsync();

            var changing = new SchedulerControlActor(
                runtime,
                SchedulerAffinityHostNames.First,
                context => SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.First)
            );
            await SchedulerControlInspection.Wait(() =>
                Equals(SchedulerControlInspection.Snapshot(runtime).Changing, changing.Process.Pid));
            releaseBody.Set();
            await retirementEntered.Task.WaitAsync(SchedulerControlCalls.Deadline);
            await SchedulerControlCalls.Normal(changing.Process);

            object gate = typeof(ProcessRuntime).GetField(RuntimeGate, Members)!.GetValue(runtime)!;
            lock (gate)
            {
                var exits = (IDictionary)typeof(ProcessRuntime).GetField(ExitContinuations, Members)!.GetValue(runtime)!;
                Require(exits.Contains(owner) && !blocked.Process.Completion.IsCompleted);
            }
            Require(SchedulerControlInspection.Snapshot(runtime).Online == SchedulerAffinityHostNames.First);
            releaseRetirement.Set();
            // No lane-online operation occurs before this physical completion.
            await SchedulerControlCalls.Normal(blocked.Process);
            Require(SchedulerControlInspection.Snapshot(runtime).Online == SchedulerAffinityHostNames.First);
            Require(probe.Count == 1 && owner.Mailbox.Count == 0);
            lock (gate)
            {
                var exits = (IDictionary)typeof(ProcessRuntime).GetField(ExitContinuations, Members)!.GetValue(runtime)!;
                Require(!exits.Contains(owner));
            }
            GC.KeepAlive(graph);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            releaseBody.Set();
            releaseRetirement.Set();
            try
            {
                publication?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (graph is IAsyncDisposable captured)
                try
                {
                    await captured.DisposeAsync().AsTask().WaitAsync(SchedulerControlCalls.Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            // RED cleanup may re-enable lane 2 only after the assertion failed;
            // this cannot turn the success observation above into a false pass.
            try
            {
                var recovery = new SchedulerControlActor(
                    runtime,
                    SchedulerAffinityHostNames.First,
                    context => SchedulerControlCalls.SetOnline(context, SchedulerAffinityHostNames.Second)
                );
                await SchedulerControlCalls.Normal(recovery.Process);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(SchedulerControlCalls.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    // This is a real runtime-owned queue group disposer, not a substitute
    // physical Task injected into the exit continuation and not a NIF claim.
    private sealed class RetirementProbe(TaskCompletionSource entered, ManualResetEventSlim release) : IDisposable
    {
        internal int Count;

        public void Dispose()
        {
            Interlocked.Increment(ref Count);
            entered.TrySetResult();
            if (!release.Wait(SchedulerControlCalls.Deadline)) throw new TimeoutException(Failure);
        }
    }
}
