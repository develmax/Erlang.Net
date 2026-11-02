using static Erlang.Tests.OrdinaryVm686Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

internal static class OrdinaryVm686Checks
{
    internal static Task PureFastPath() => Run(OrdinaryVm686Mode.Pure);

    internal static Task ActiveShutdown() => Run(OrdinaryVm686Mode.ActiveShutdown);

    internal static Task QueuedShutdown() => Run(OrdinaryVm686Mode.QueuedShutdown);

    internal static Task HibernateArgument() => Run(OrdinaryVm686Mode.HibernateArgument);

    internal static Task ErrorReason() => Run(OrdinaryVm686Mode.Error);

    internal static Task ThrowReason() => Run(OrdinaryVm686Mode.Throw);

    internal static Task ExitReason() => Run(OrdinaryVm686Mode.Exit);

    private static async Task Run(OrdinaryVm686Mode mode)
    {
        bool pure = mode == OrdinaryVm686Mode.Pure;
        bool queued = mode == OrdinaryVm686Mode.QueuedShutdown;
        await using var fixture = await OrdinaryVm686Fixture.Create(!pure);
        string sourceMode = mode switch
        {
            OrdinaryVm686Mode.HibernateArgument => ModeHibernate,
            OrdinaryVm686Mode.Error => ModeError,
            OrdinaryVm686Mode.Throw => ModeThrow,
            OrdinaryVm686Mode.Exit => ModeExit,
            _ => ModePure
        };
        await fixture.StartChild(sourceMode, queued);
        Require(!(bool)Property(fixture.Runtime, Profile));
        object? heap = fixture.HeapNow();
        if (pure || queued)
        {
            // Runtime storage is enrolled at actual ProcessContext admission;
            // this neither grants guest execution nor implies worker entry.
            Require((bool)Property(fixture.Context!, StorageEnabled) && heap is not null && fixture.RegistryContainsChild());
            Require(fixture.EnabledNow() == pure);
            Require((Field(fixture.Context!.Dictionary, DictionaryFrame) is not null) == pure);
            object input = Field(fixture.Context, Input)!;
            object prepared = Field(input, Prepared)!;
            object? graph = Property(prepared, Graph);
            if (pure) Require(graph is null && fixture.EntryCalls == 1);
            else
            {
                Require(graph is not null && (bool)Property(graph, LiveGraph));
                Require(fixture.EntryCalls == 0);
            }
        }
        else
        {
            Require(fixture.EnabledNow() && heap is not null && fixture.RegistryContainsChild());
            object snapshot = Property(heap!, Snapshot);
            Require(Convert.ToInt64(Property(snapshot, CapacityWords)) > 0
                && Convert.ToInt32(Property(snapshot, RootFrames)) > 0);
        }
        fixture.ReleaseProducer();
        if (!pure) Require(!fixture.NativeRetired());
        if (mode is OrdinaryVm686Mode.ActiveShutdown or OrdinaryVm686Mode.QueuedShutdown)
        {
            await fixture.StopVm();
            await fixture.AwaitPhysicalChild();
            await fixture.AwaitNativeRetired();
            Require(fixture.EntryCalls == (queued ? 0 : 1));
            if (heap is not null)
            {
                object snapshot = Property(heap, Snapshot);
                Require(Convert.ToInt64(Property(snapshot, CapacityWords)) == 0
                    && Convert.ToInt32(Property(snapshot, RootFrames)) == 0);
            }
            await AwaitOwnerGone(fixture);

            return;
        }
        fixture.GoNow();
        if (mode is OrdinaryVm686Mode.Error or OrdinaryVm686Mode.Throw or OrdinaryVm686Mode.Exit)
        {
            // Do not observe the queued DOWN until its source frame/slab and
            // child execution actually ended. Only recipient custody may remain.
            await fixture.AwaitPhysicalChild();
            Require(!fixture.NativeRetired());
            object snapshot = Property(heap!, Snapshot);
            Require(Convert.ToInt64(Property(snapshot, CapacityWords)) == 0
                && Convert.ToInt32(Property(snapshot, RootFrames)) == 0);
            Term reason = await fixture.ReadDown();
            ValidateReason(mode, reason, fixture.Reference!);
            fixture.CaptureResourceViews(reason);
            await fixture.RequireActualResourceOperations(reason);
            fixture.DisposeViews();
            await fixture.AwaitNativeRetired();
            await AwaitOwnerGone(fixture);
            GC.KeepAlive(reason);

            return;
        }
        Term done = await fixture.ReadDone();
        Require(done.Equals(pure ? Term.A(Ok) : Term.Tuple(Term.A(Value), Term.I(1))));
        Require(!fixture.Child!.Completion.IsCompleted);
        if (!pure) Require(!fixture.NativeRetired());
        fixture.FinishNow();
        Term down = await fixture.ReadDown();
        Require(down.Equals(Term.A(Normal)));
        await fixture.AwaitPhysicalChild();
        if (!pure)
        {
            await fixture.AwaitNativeRetired();
            await AwaitOwnerGone(fixture);
        }
        else
        {
            Require(fixture.EnabledNow() && fixture.HeapNow() is not null);
            object snapshot = Property(fixture.HeapNow()!, Snapshot);
            Require(Convert.ToInt64(Property(snapshot, CapacityWords)) == 0 && Convert.ToInt32(Property(snapshot, RootFrames)) == 0);
            await AwaitOwnerGone(fixture);
        }
    }

    private static void ValidateReason(OrdinaryVm686Mode mode, Term reason, ReferenceTerm reference)
    {
        if (mode == OrdinaryVm686Mode.Exit)
        {
            Require(reason.Equals(Term.Tuple(Term.A(Native), reference)));

            return;
        }
        Require(reason is TupleTerm { Items.Count: 2 });
        var pair = (TupleTerm)reason;
        Term expected = Term.Tuple(Term.A(Native), reference);
        if (mode == OrdinaryVm686Mode.Throw) expected = Term.Tuple(Term.A(NoCatch), expected);
        Require(pair.Items[0].Equals(expected));
        Require(pair.Items[1] is Cons);
        Term[] stack = Cons.Items(pair.Items[1]).ToArray();
        Require(stack.Length > 0 && stack.All(frame => frame is TupleTerm { Items.Count: 4 }));
        Require(stack.Cast<TupleTerm>().Any(frame => frame.Items[0].Equals(Term.A(Module))));
        if (mode == OrdinaryVm686Mode.Error)
            Require(ResourceIn(pair.Items[1], reference));
    }

    private static bool ResourceIn(Term source, ReferenceTerm expected)
    {
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm reference && reference.Equals(expected)) return true;
            if (value is TupleTerm tuple)
                foreach (Term child in tuple.Items) pending.Push(child);
            else if (value is Cons list)
            {
                pending.Push(list.Head);
                pending.Push(list.Tail);
            }
        }

        return false;
    }

    private static async Task AwaitOwnerGone(OrdinaryVm686Fixture fixture)
    {
        using var deadline = new CancellationTokenSource(Deadline);
        while (fixture.RegistryContainsChild()) await Task.Delay(Poll, deadline.Token);
    }
}
