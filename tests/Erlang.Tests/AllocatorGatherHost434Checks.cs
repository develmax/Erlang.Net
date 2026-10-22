using System.Numerics;
using Erlang;
namespace Erlang.Tests;

internal static class AllocatorGatherHost434Checks
{
    internal static async Task NifSpans()
    {
        await using var runtime = await AllocatorGatherHost434Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object domain = AllocatorGatherHost434Access.Get(runtime, "NativeAllocationDomain");
        object owner = Activator.CreateInstance(typeof(ProcessRuntime).Assembly.GetType(AllocatorGatherHost434Names.Nif)!, true)!;
        AllocatorGatherHost434Access.Call(owner, "JoinDomain", domain);
        AllocatorGatherHost434Access.Call(owner, "RegisterWindow434", AllocatorGatherHost434Names.NifModule);
        try
        {
            nint zero = (nint)AllocatorGatherHost434Access.Call(owner, "Allocate", (nuint)0)!;
            nint small = (nint)AllocatorGatherHost434Access.Call(owner, "Allocate", (nuint)8)!;
            nint large = (nint)AllocatorGatherHost434Access.Call(owner, "Allocate", (nuint)64)!;
            AllocatorGatherHost434Access.Require(zero != 0 && small != 0 && large != 0);
            TupleTerm hist = (TupleTerm)await AllocatorGatherHost434Access.Query(actor, runtime, AllocatorGatherHost434Names.Driver);
            var rows = Cons.Items(hist.Items[2]).Cast<TupleTerm>().ToArray();
            AllocatorGatherHost434Access.Require(rows.Length == 1 && rows[0].Items[0].Equals(Term.A(AllocatorGatherHost434Names.NifModule))
             && rows[0].Items[1].Equals(Term.A("nif_internal")) && rows[0].Items[2].Equals(Term.Tuple(
                 Term.I(1),
                 Term.I(1),
                 Term.I(0),
                 Term.I(0),
                 Term.I(1)
             )));
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.Driver,
                true
            )).SequenceEqual(new ulong[] { 1, 8, 64 }));
            small = (nint)AllocatorGatherHost434Access.Call(
                owner,
                "Reallocate",
                small,
                (nuint)32
            )!;
            AllocatorGatherHost434Access.Require(small != 0);
            AllocatorGatherHost434Access.Call(owner, "Free", zero);
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.Driver,
                true
            )).SequenceEqual(new ulong[] { 32, 64 }));
        }
        finally
        {
            AllocatorGatherHost434Access.Call(owner, "ReclaimAll");
            AllocatorGatherHost434Access.Require((bool)AllocatorGatherHost434Access.Call(owner, "TryEndDomainMembership")!);
        }
        AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.Driver,
            true
        )).Length == 0);
    }

    internal static async Task ArrayAndPersistent()
    {
        await using var runtime = await AllocatorGatherHost434Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object owner = AllocatorGatherHost434Access.Get(runtime, "AtomicsOwner413");
        object array;
        using ((IDisposable)AllocatorGatherHost434Access.Call(owner, "Enter")!) array = AllocatorGatherHost434Access.Call(
            owner,
            "Create",
            (ulong)3,
            true
        )!;
        try
        {
            ulong bytes = (ulong)AllocatorGatherHost434Access.Get(array, "AllocatedBytes");
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.Standard,
                true
            )).SequenceEqual(new[] { bytes }));
        }
        finally
        {
            AllocatorGatherHost434Access.Call(owner, "Retire", array);
        }
        AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.Standard,
            true
        )).Length == 0);
        object counterOwner = AllocatorGatherHost434Access.Get(runtime, "CountersOwner421");
        object counter;
        using ((IDisposable)AllocatorGatherHost434Access.Call(counterOwner, "Enter")!) counter = AllocatorGatherHost434Access.Call(
            counterOwner,
            "Create",
            (ulong)2,
            2
        )!;
        try
        {
            ulong bytes = (ulong)AllocatorGatherHost434Access.Get(counter, "AllocatedBytes");
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.Standard,
                true
            )).SequenceEqual(new[] { bytes }));
        }
        finally
        {
            AllocatorGatherHost434Access.Call(counterOwner, "Retire", counter);
        }
        object persistent = AllocatorGatherHost434Access.Get(runtime, "PersistentTerms");
        await actor.InvokeAsync(async context => await (ValueTask<Term>)AllocatorGatherHost434Access.Call(
            persistent,
            "Put",
            context,
            Term.A("actual_allocator_key434"),
            Term.Tuple(Term.I(1), Term.I(2)),
            false
        )!);
        ulong[] pair = AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.Literal,
            true
        ));
        ulong[] table = AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.LongLived,
            true
        ));
        MapTerm info = await actor.InvokeAsync(context => Task.FromResult((MapTerm)AllocatorGatherHost434Access.Call(persistent, "Info", context)!));
        AllocatorGatherHost434Access.Require(pair.Length > 0 && table.Length > 0 && info.TryGet(Term.A("memory"), out Term? memory)
         && ((Integer)memory!).Value == pair.Concat(table).Aggregate(BigInteger.Zero, (sum, value) => sum + value));
        object arena = AllocatorGatherHost434Access.Get(persistent, "arena");
        using ((IDisposable)AllocatorGatherHost434Access.Call(arena, "AcquireSnapshot")!)
        {
            _ = await actor.InvokeAsync(async context => await (ValueTask<Term>)AllocatorGatherHost434Access.Call(
                persistent,
                "Erase",
                context,
                Term.A("actual_allocator_key434")
            )!);
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.Literal,
                true
            )).SequenceEqual(pair));
            AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
                actor,
                runtime,
                AllocatorGatherHost434Names.LongLived,
                true
            )).Length == table.Length + 1);
        }
        AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.Literal,
            true
        )).Length == 0);
        AllocatorGatherHost434Access.Require(AllocatorGatherHost434Access.Extents(await AllocatorGatherHost434Access.Query(
            actor,
            runtime,
            AllocatorGatherHost434Names.LongLived,
            true
        )).Length == table.Length);
    }

    internal static async Task PhysicalRetirement()
    {
        await using var runtime = await AllocatorGatherHost434Access.Create();
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext a = await first.Ready, b = await second.Ready;
        int participants = (int)AllocatorGatherHost434Access.Call(runtime, "AuxiliaryParticipants430", a)!;
        AllocatorGatherHost434Access.Require(participants == 2);
        Term valid = await first.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            AllocatorGatherHost434Names.Module,
            AllocatorGatherHost434Names.Hist,
            Term.Tuple(
                Term.A(AllocatorGatherHost434Names.Standard),
                Term.I(participants - 1),
                Term.I(5),
                Term.I(8),
                Term.I(0),
                runtime.MakeReference()
            )
        ));
        AllocatorGatherHost434Access.Require(valid.Equals(Term.I(0)));
        try
        {
            _ = await first.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                AllocatorGatherHost434Names.Module,
                AllocatorGatherHost434Names.Hist,
                Term.Tuple(
                    Term.A(AllocatorGatherHost434Names.Standard),
                    Term.I(participants),
                    Term.I(5),
                    Term.I(8),
                    Term.I(0),
                    runtime.MakeReference()
                )
            ));
            throw new InvalidOperationException(AllocatorGatherHost434Names.Failure);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg"))) { }

        object dispatcher = AllocatorGatherHost434Access.Get(runtime, "actorDispatcher");
        RuntimeNodeFoundationHostActor target = (int)AllocatorGatherHost434Access.Call(dispatcher, "AssignedLane420", a)! == 2 ? first : second;
        AllocatorGatherHost434Access.Require((int)AllocatorGatherHost434Access.Call(dispatcher, "AssignedLane420", await target.Ready)! == 2);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        object batch = AllocatorGatherHost434Access.Call(dispatcher, "PrepareAuxiliary420", (Action<int>)(lane =>
      {
          if (lane == 1)
          {
              entered.Set();
              release.Wait();
          }
      }))!;
        AllocatorGatherHost434Access.Call(batch, "Publish");
        try
        {
            AllocatorGatherHost434Access.Require(entered.Wait(AllocatorGatherHost434Names.Deadline));
            Term count = await target.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                AllocatorGatherHost434Names.Module,
                AllocatorGatherHost434Names.Hist,
                Term.Tuple(
                    Term.A(AllocatorGatherHost434Names.Standard),
                    Term.I(0),
                    Term.I(5),
                    Term.I(8),
                    Term.I(0),
                    runtime.MakeReference()
                )
            ));
            AllocatorGatherHost434Access.Require(count.Equals(Term.I(1)));
            runtime.Exit(ReferenceEquals(target, first) ? b : a, target.Process.Pid, Term.A(AllocatorGatherHost434Names.Kill));
            AllocatorGatherHost434Access.Require(!target.Process.Completion.IsCompleted);
        }
        finally
        {
            release.Set();
            await ((Task)AllocatorGatherHost434Access.Get(batch, "Completion")).WaitAsync(AllocatorGatherHost434Names.Deadline);
        }
        await target.Process.Completion.WaitAsync(AllocatorGatherHost434Names.Deadline);
        object gather = AllocatorGatherHost434Access.Get(runtime, "allocatorGather434");
        AllocatorGatherHost434Access.Require((bool)AllocatorGatherHost434Access.Get(gather, "Drained"));
    }
}
