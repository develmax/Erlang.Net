using System.Collections;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.OpaqueReceiverCollection379Names;

namespace Erlang.Tests;

internal static class OpaqueReceiverCollection379
{
    internal static async Task Collect(OpaqueBoundary602Fixture fixture)
    {
        ProcessContext actual = await fixture.Receiver!.Ready;
        object heap = actual.GetType().GetField(Heap, OpaqueBoundary602Names.Members)!.GetValue(actual)!;
        int before = OpaqueBoundary602Custody367.HeaderLeases(heap, fixture.Atomics).Count;
        long guestBefore = fixture.Atomics.Count(OpaqueBoundary602Names.GuestRoots);
        object tracker = Property(actual.Mailbox, Retirements);
        var pending = new List<Task>();
        lock (tracker.GetType().GetField(Gate, OpaqueBoundary602Names.Members)!.GetValue(tracker)!)
            foreach (string list in new[] { Active, Failed })
                foreach (object work in (IEnumerable)tracker.GetType().GetField(list, OpaqueBoundary602Names.Members)!.GetValue(tracker)!)
                    pending.Add((Task)Property(Property(work, Completion), CompletionTask));
        // Await consumed work outside all gates. Closing/draining this tracker
        // here would reject the real receiver invocation used for collection.
        await Task.WhenAll(pending).WaitAsync(OpaqueBoundary602Names.Deadline);
        await fixture.Receiver.InvokeAsync(async context =>
        {
            Term result = await fixture.Runtime.Modules.Call(context, ErlangModuleNames.Module, GarbageCollect);
            OpaqueBoundary602Fixture.Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        }).WaitAsync(OpaqueBoundary602Names.Deadline);
        int after = OpaqueBoundary602Custody367.HeaderLeases(heap, fixture.Atomics).Count;
        long guestAfter = fixture.Atomics.Count(OpaqueBoundary602Names.GuestRoots);
        Console.WriteLine(
            Diagnostic,
            before,
            after,
            guestBefore,
            guestAfter
        );
        OpaqueBoundary602Fixture.Require(after == 0 && guestAfter == 0);
    }
}
