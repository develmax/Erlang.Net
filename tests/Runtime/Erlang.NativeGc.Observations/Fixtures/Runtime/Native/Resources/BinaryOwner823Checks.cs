using Erlang;
using System.Diagnostics;
using System.Reflection;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.BinaryOwner823Names;

namespace Erlang.Tests;

internal static class BinaryOwner823Checks
{
    internal static async Task Run()
    {
        foreach (bool enabled in new[] { false, true })
        {
            await Profile(enabled);
            await DeathDuringAdmission(enabled);
        }
    }

    private static Array Keys()
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(ItemType, true)!;
        Array keys = Array.CreateInstance(type, 1);
        keys.SetValue(Activator.CreateInstance(type, [BinaryAtom, BinaryName, null]), 0);

        return keys;
    }

    private static ValueTask<Term> QueryInfo(ProcessContext requester, Pid target)
        => (ValueTask<Term>)Call(
            requester.Runtime,
            Query,
            requester,
            target,
            Keys(),
            true
        )!;

    private static int PendingCount(ProcessContext context)
    {
        object gate = typeof(ProcessRuntime).GetField(Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(context.Runtime)!;
        lock (gate) return (int)Property(Property(context, Pending), Count);
    }

    private static async Task WaitPending(ProcessContext target)
    {
        var elapsed = Stopwatch.StartNew();
        while (PendingCount(target) == 0)
        {
            Check(elapsed.Elapsed < Deadline);
            await Task.Delay(PollInterval);
        }
    }

    private static async Task DeathDuringAdmission(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext target = await owner.Ready;
        object heap = Property(target, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            new BitString(new byte[BinaryBytes])
        );
        MethodInfo terminate = typeof(ProcessRuntime).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == Terminate && method.GetParameters().Length == TerminateParameterCount);
        Func<bool?> die = () =>
        {
            terminate.Invoke(runtime, [target, NormalAtom, null, null]);

            return null;
        };
        Call(
            target.Mailbox,
            SendPending,
            Nil.Value,
            die,
            null,
            false,
            null,
            false,
            null
        );
        Term result = await QueryInfo(target, target.Self);
        Check(result.Equals(UndefinedAtom));
        Check(!runtime.IsAlive(target.Self));
        bool retired = false;
        try
        {
            _ = Call(target, Capture);
        }
        catch (ObjectDisposedException)
        {
            retired = true;
        }
        Check(retired);
    }

    private static async Task Profile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext target = await owner.Ready;
        ProcessContext sender = await requester.Ready;
        object heap = Property(target, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        try
        {
            Call(
                frame,
                Set,
                slot,
                new BitString(new byte[BinaryBytes])
            );
            Term physical = (Term)Call(target, Capture)!;
            var expected = Term.Tuple(BinaryAtom, physical);
            Term self = await owner.InvokeAsync(async context => await QueryInfo(context, context.Self));
            Check(self.Equals(expected));

            Task<Term> remote = requester.InvokeAsync(async context => await QueryInfo(context, target.Self));
            await WaitPending(target);
            Check(!remote.IsCompleted);
            await owner.Invoke(context =>
            {
                Call(runtime, Admit, context);

                return Nil.Value;
            });
            Check((await remote.WaitAsync(Deadline)).Equals(expected));
            Check(PendingCount(target) == 0);
            Check(((Term)Call(target, Capture)!).Equals(physical));

            // A cancelled requester retires its producer before a delayed owner
            // marker is drained. The owner must not publish into disposed state.
            Task<Term> cancelled = requester.InvokeAsync(async context => await QueryInfo(context, target.Self));
            await WaitPending(target);
            ((CancellationTokenSource)Property(sender, Stop)).Cancel();
            bool observedCancellation = false;
            try
            {
                await cancelled.WaitAsync(Deadline);
            }
            catch (OperationCanceledException)
            {
                observedCancellation = true;
            }
            Check(observedCancellation);
            await owner.Invoke(context =>
            {
                Call(runtime, Admit, context);

                return Nil.Value;
            });
            Check(PendingCount(target) == 0);
            Check(((Term)Call(target, Capture)!).Equals(physical));
        }
        finally
        {
            Call(frame, Clear, slot);
            Call(heap, Collect);
        }
    }
}
