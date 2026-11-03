using System.Collections;
using static Erlang.Tests.DirtyIoConcurrency522Names;

namespace Erlang.Tests;

internal static class DirtyIoConcurrency522Checks
{
    internal static async Task HeldIoLeavesRealControlLane()
    {
        foreach (int invalid in new[] { 0, int.MaxValue })
        {
            bool rejected = false;
            try
            {
                await using var unexpected = new ProcessRuntime(new ProcessRuntimeOptions { DirtyIoSchedulerCount = invalid });
            }
            catch (ArgumentOutOfRangeException)
            {
                rejected = true;
            }
            Check(rejected);
        }
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = Native521RuntimeFactory.Create(owned);
            await runtime.InitializeModulesAsync(_ => { });
            await using var holder = new RuntimeNodeFoundationHostActor(runtime);
            await using var control = new RuntimeNodeFoundationHostActor(runtime);
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Task<bool>? held = null;
            int heldThread = 0, controlThread = 0;
            try
            {
                held = holder.InvokeAsync(async context =>
                {
                    await DirtyNative419Access.Schedule(
                        context,
                        DirtyNative419Names.Io,
                        [],
                        (c, _) =>
                    {
                        heldThread = Environment.CurrentManagedThreadId;
                        Check(DirtyNative419Access.Kind(c) == DirtyNative419Names.Io && !DirtyNative419Access.NormalTurn(c));
                        entered.Set();
                        Check(release.Wait(DirtyNative419Names.Deadline));

                        return Nil.Value;
                    }
                    );

                    return true;
                });
                Check(entered.Wait(DirtyNative419Names.Deadline));
                await control.InvokeAsync(async context =>
                {
                    await DirtyNative419Access.Schedule(
                        context,
                        DirtyNative419Names.Io,
                        [],
                        (c, _) =>
                    {
                        controlThread = Environment.CurrentManagedThreadId;
                        Check(DirtyNative419Access.Kind(c) == DirtyNative419Names.Io && !DirtyNative419Access.NormalTurn(c));

                        return Nil.Value;
                    }
                    );
                    Check((await runtime.Modules.Call(
                        context,
                        DirtyNative419Names.Runtime,
                        DirtyNative419Names.SystemInfo,
                        Term.A(DirtyNative419Names.Counts[2])
                    )).Equals(Term.I(ProfileIoWorkers)));
                    Check((await runtime.Modules.Call(
                        context,
                        DirtyNative419Names.Runtime,
                        DirtyNative419Names.SystemInfo,
                        Term.A(DirtyNative419Names.Counts[0])
                    )).Equals(Term.I(1)));

                    return true;
                }).WaitAsync(DirtyNative419Names.Deadline);
                Check(!held.IsCompleted && heldThread != controlThread);
                object pool = Snapshot(runtime);
                Check(Field<int>(pool, IoWorkers) == ProfileIoWorkers && Field<int>(pool, CpuWorkers) == 1);
                int[] io = Rows(pool).Where(row => Field<object>(row, WorkerKind).ToString() == DirtyNative419Names.Io)
                    .Select(row => Field<int>(row, ThreadId)).ToArray();
                Check(io.Length == ProfileIoWorkers && io.Distinct().Count() == ProfileIoWorkers
                    && io.Contains(heldThread) && io.Contains(controlThread));
            }
            finally
            {
                release.Set();
                if (held is not null)
                    await held.WaitAsync(DirtyNative419Names.Deadline);
            }
        }
    }

    internal static async Task CloseJoinsEveryIoWorker()
    {
        foreach (bool owned in new[] { false, true })
        {
            var runtime = Native521RuntimeFactory.Create(owned);
            await runtime.InitializeModulesAsync(_ => { });
            var first = new RuntimeNodeFoundationHostActor(runtime);
            var second = new RuntimeNodeFoundationHostActor(runtime);
            using var enteredFirst = new ManualResetEventSlim();
            using var enteredSecond = new ManualResetEventSlim();
            using var releaseFirst = new ManualResetEventSlim();
            using var releaseSecond = new ManualResetEventSlim();
            Task<bool>? firstWork = null, secondWork = null;
            Task? closing = null;
            try
            {
                firstWork = Held(first, enteredFirst, releaseFirst);
                secondWork = Held(second, enteredSecond, releaseSecond);
                Check(enteredFirst.Wait(DirtyNative419Names.Deadline) && enteredSecond.Wait(DirtyNative419Names.Deadline));
                ProcessContext firstContext = await first.Ready;
                ProcessContext secondContext = await second.Ready;
                var physicalFirst = DebugDirtyQueue424Access.PhysicalWork(runtime, firstContext);
                var physicalSecond = DebugDirtyQueue424Access.PhysicalWork(runtime, secondContext);
                closing = runtime.DisposeAsync().AsTask();
                Check(!closing.IsCompleted);
                releaseFirst.Set();
                await physicalFirst.Settled.WaitAsync(DirtyNative419Names.Deadline);
                Check(!closing.IsCompleted);
                releaseSecond.Set();
                await physicalSecond.Settled.WaitAsync(DirtyNative419Names.Deadline);
                await closing.WaitAsync(DirtyNative419Names.Deadline);
                object pool = Snapshot(runtime);
                Check(Field<bool>(pool, DirtyNative419Names.Joined));
                Check(Rows(pool).Count() == ProfileIoWorkers + 1);
                Check(Rows(pool).All(row => !Field<bool>(row, WorkerAlive) && Field<bool>(row, WorkerJoined)
                    && Field<bool>(row, WorkerQuiesced)));
                await Canceled(firstWork, firstContext);
                await Canceled(secondWork, secondContext);
            }
            finally
            {
                releaseFirst.Set();
                releaseSecond.Set();
                if (closing is not null)
                    await closing.WaitAsync(DirtyNative419Names.Deadline);
                await first.DisposeAsync();
                await second.DisposeAsync();
                await runtime.DisposeAsync();
            }
        }
    }

    private static Task<bool> Held(RuntimeNodeFoundationHostActor actor, ManualResetEventSlim entered, ManualResetEventSlim release)
        => actor.InvokeAsync(async context =>
        {
            await DirtyNative419Access.Schedule(
                context,
                DirtyNative419Names.Io,
                [],
                (_, _) =>
            {
                entered.Set();
                Check(release.Wait(DirtyNative419Names.Deadline));

                return Nil.Value;
            }
            );

            return true;
        });

    private static async Task Canceled(Task task, ProcessContext owner)
    {
        bool canceled = false;
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        Check(canceled && owner.Cancellation.IsCancellationRequested);
    }

    private static object Snapshot(ProcessRuntime runtime)
    {
        object owner = typeof(ProcessRuntime).GetField(DirtyNative419Names.Owner, Members)!.GetValue(runtime)!;

        return owner.GetType().GetMethod(PoolSnapshot, Members)!.Invoke(owner, null)!;
    }

    private static IEnumerable<object> Rows(object pool) => ((IEnumerable)pool.GetType().GetProperty(Workers, Members)!.GetValue(pool)!).Cast<object>();

    private static T Field<T>(object value, string property) => (T)value.GetType().GetProperty(property, Members)!.GetValue(value)!;

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(nameof(DirtyIoConcurrency522Checks));
    }
}
