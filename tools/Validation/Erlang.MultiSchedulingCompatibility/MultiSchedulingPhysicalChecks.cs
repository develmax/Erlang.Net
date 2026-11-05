using System.Collections.Concurrent;
using Erlang;
using static MultiSchedulingProbe;
using static MultiSchedulingProbeNames;

internal static class MultiSchedulingPhysicalChecks
{
    internal static async Task DrainNormal()
    {
        await using var runtime = new ProcessRuntime(Lanes);
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        var blocked = Signal();
        var unblock = Signal();
        var held = Start(
            runtime,
            async context =>
        {
            await runtime.Modules.Call(
                context,
                Runtime,
                ProcessFlag,
                Term.A(Binding),
                Term.I(2)
            );
            entered.TrySetResult();
            Require(release.Wait(Deadline));

            return Nil.Value;
        }
        );
        try
        {
            await entered.Task.WaitAsync(Deadline);
            var blocker = Start(
                runtime,
                async context =>
            {
                await FlagCall(context, Block);
                blocked.TrySetResult();
                Require((await InfoCall(context, Identity)).Equals(Term.I(1)));
                await unblock.Task;

                return await FlagCall(context, Unblock);
            }
            );
            await Until(() => BarrierPending(runtime));
            Require(!blocked.Task.IsCompleted);
            release.Set();
            await Finish(held);
            await blocked.Task.WaitAsync(Deadline);
            unblock.TrySetResult();
            await Finish(blocker);
        }
        finally
        {
            release.Set();
            unblock.TrySetResult();
        }
    }

    internal static async Task Normal()
    {
        await using var runtime = new ProcessRuntime(Lanes);
        var result = Start(
            runtime,
            async context =>
        {
            Require((await FlagCall(context, Block)).Equals(Term.A(Blocked)));
            var threads = new ConcurrentDictionary<int, byte>();
            int active = 0;
            int overlap = 0;
            var guests = Enumerable.Range(0, Lanes).Select(_ => Start(
                runtime,
                async guest =>
            {
                for (int turn = 0; turn < Turns; turn++)
                {
                    Require((await InfoCall(guest, Identity)).Equals(Term.I(1)));
                    threads.TryAdd(Environment.CurrentManagedThreadId, 0);
                    if (Interlocked.Increment(ref active) != 1)
                        Interlocked.Increment(ref overlap);
                    Thread.SpinWait(SpinIterations);
                    Interlocked.Decrement(ref active);
                    await guest.YieldExecution();
                }

                return Nil.Value;
            }
            )).ToArray();
            foreach (var guest in guests)
                await Finish(guest);
            Require(overlap == 0 && threads.Count == 1);
            Require((await FlagCall(context, Unblock)).Equals(Term.A(Enabled)));

            return Nil.Value;
        }
        );
        await Finish(result);
    }

    internal static async Task QueueWhileBlocked(int capacity, bool normal)
    {
        await using var runtime = new ProcessRuntime(capacity);
        var ready = Signal();
        var release = Signal();
        var cpuEntered = Signal();
        var ioEntered = Signal();
        var cpuSubmitted = Signal();
        var ioSubmitted = Signal();
        var blocker = Start(
            runtime,
            async context =>
        {
            await FlagCall(context, normal ? BlockNormal : Block);
            ready.TrySetResult();
            await release.Task;

            return await FlagCall(context, normal ? UnblockNormal : Unblock);
        }
        );
        try
        {
            await ready.Task.WaitAsync(Deadline);
            var cpu = Start(runtime, context => Dirty(
                context,
                Cpu,
                (_, _) =>
            {
                cpuEntered.TrySetResult();

                return Nil.Value;
            },
                cpuSubmitted
            ));
            var io = Start(runtime, context => Dirty(
                context,
                Io,
                (_, _) =>
            {
                ioEntered.TrySetResult();

                return Nil.Value;
            },
                ioSubmitted
            ));
            await Task.WhenAll(cpuSubmitted.Task, ioSubmitted.Task).WaitAsync(Deadline);
            if (normal)
            {
                await Task.WhenAll(cpuEntered.Task, ioEntered.Task).WaitAsync(Deadline);
                await Finish(cpu);
                await Finish(io);
            }
            else
            {
                Require(!cpuEntered.Task.IsCompleted && !ioEntered.Task.IsCompleted);
                Require(ReadSnapshot<int>(runtime, Pending) == 2 && ReadSnapshot<int>(runtime, Running) == 0);
            }
            release.TrySetResult();
            await Finish(blocker);
            await Finish(cpu);
            await Finish(io);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    internal static async Task ActiveDirty()
    {
        await using var runtime = new ProcessRuntime(Lanes);
        using var cpuRelease = new ManualResetEventSlim();
        using var ioRelease = new ManualResetEventSlim();
        var cpuEntered = Signal();
        var ioEntered = Signal();
        var cpu = Start(
            runtime,
            context => Dirty(
                context,
                Cpu,
                (_, _) =>
        {
            cpuEntered.TrySetResult();
            Require(cpuRelease.Wait(Deadline));

            return Nil.Value;
        },
                Signal()
            )
        );
        var io = Start(
            runtime,
            context => Dirty(
                context,
                Io,
                (_, _) =>
        {
            ioEntered.TrySetResult();
            Require(ioRelease.Wait(Deadline));

            return Nil.Value;
        },
                Signal()
            )
        );
        var blocked = Signal();
        var unblock = Signal();
        try
        {
            await Task.WhenAll(cpuEntered.Task, ioEntered.Task).WaitAsync(Deadline);
            var blocker = Start(
                runtime,
                async context =>
            {
                await FlagCall(context, Block);
                blocked.TrySetResult();
                await unblock.Task;

                return await FlagCall(context, Unblock);
            }
            );
            await Until(() => IsPaused(runtime));
            Require(!blocked.Task.IsCompleted);
            Require((await Finish(Start(runtime, context => InfoCall(context, Multi)))).Equals(Term.A(Enabled)));
            var queuedEntered = Signal();
            var submitted = Signal();
            var queued = Start(runtime, context => Dirty(
                context,
                Io,
                (_, _) =>
            {
                queuedEntered.TrySetResult();

                return Nil.Value;
            },
                submitted
            ));
            await submitted.Task.WaitAsync(Deadline);
            cpuRelease.Set();
            await Finish(cpu);
            Require(!blocked.Task.IsCompleted && !queuedEntered.Task.IsCompleted);
            ioRelease.Set();
            await Finish(io);
            await blocked.Task.WaitAsync(Deadline);
            Require(!queuedEntered.Task.IsCompleted && ReadSnapshot<int>(runtime, Running) == 0);
            unblock.TrySetResult();
            await Finish(blocker);
            await Finish(queued);
        }
        finally
        {
            cpuRelease.Set();
            ioRelease.Set();
            unblock.TrySetResult();
        }
    }

    internal static async Task CancelPendingBlocker()
    {
        await using var runtime = new ProcessRuntime(Lanes);
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        var cpu = Start(
            runtime,
            context => Dirty(
                context,
                Cpu,
                (_, _) =>
        {
            entered.TrySetResult();
            Require(release.Wait(Deadline));

            return Nil.Value;
        },
                Signal()
            )
        );
        try
        {
            await entered.Task.WaitAsync(Deadline);
            ProcessHandle blocker = runtime.Spawn(async context => await FlagCall(context, Block));
            await Until(() => IsPaused(runtime));
            await KillProcess(runtime, blocker.Pid);
            await blocker.Completion.WaitAsync(Deadline);
            Require(!IsPaused(runtime));
            var io = Start(runtime, context => Dirty(
                context,
                Io,
                (_, _) => Nil.Value,
                Signal()
            ));
            await Finish(io);
            Require((await Finish(Start(runtime, context => InfoCall(context, Multi)))).Equals(Term.A(Enabled)));
            release.Set();
            await Finish(cpu);
        }
        finally
        {
            release.Set();
        }
    }

    internal static async Task Close(bool active)
    {
        var runtime = new ProcessRuntime(Lanes);
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        var blocked = Signal();
        Task? close = null;
        try
        {
            if (active)
            {
                _ = Start(
                    runtime,
                    context => Dirty(
                        context,
                        Cpu,
                        (_, _) =>
                {
                    entered.TrySetResult();
                    Require(release.Wait(Deadline));

                    return Nil.Value;
                },
                        Signal()
                    )
                );
                await entered.Task.WaitAsync(Deadline);
            }
            _ = runtime.Spawn(async context =>
            {
                await FlagCall(context, Block);
                blocked.TrySetResult();
                await context.ReceiveAsync<Term>(_ => null);

                return Nil.Value;
            });
            if (active)
                await Until(() => IsPaused(runtime));
            else
                await blocked.Task.WaitAsync(Deadline);
            var submitted = Signal();
            _ = runtime.Spawn(async context => await Dirty(
                context,
                Io,
                (_, _) => Nil.Value,
                submitted
            ));
            await submitted.Task.WaitAsync(Deadline);
            close = runtime.DisposeAsync().AsTask();
            if (active)
                Require(!close.IsCompleted);
            release.Set();
            await close.WaitAsync(Deadline);
            Require(ReadSnapshot<bool>(runtime, Joined));
        }
        finally
        {
            release.Set();
            if (close is null)
                await runtime.DisposeAsync();
        }
    }

    internal static async Task RepeatedBlocks()
    {
        await using var runtime = new ProcessRuntime(Lanes);
        await Finish(Start(
            runtime,
            async context =>
        {
            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                await FlagCall(context, Block);
                var submitted = Signal();
                var entered = Signal();
                var dirty = Start(runtime, guest => Dirty(
                    guest,
                    cycle % 2 == 0 ? Cpu : Io,
                    (_, _) =>
                {
                    entered.TrySetResult();

                    return Nil.Value;
                },
                    submitted
                ));
                await submitted.Task;
                Require(!entered.Task.IsCompleted);
                await FlagCall(context, Unblock);
                await Finish(dirty);
            }

            return Nil.Value;
        }
        ));
    }
}
