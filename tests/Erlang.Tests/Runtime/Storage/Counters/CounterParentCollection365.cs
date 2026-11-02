using System.Collections;
using System.Reflection;
using System.Text.Json;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.CounterParentCollection365Names;

namespace Erlang.Tests;

internal static class CounterParentCollection365
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Collect(ProcessRuntime runtime, RuntimeNodeFoundationHostActor parent, object array)
    {
        ProcessContext actual = await parent.Ready;
        object heap = typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(actual)!;
        int before = Headers(heap, array);
        long guestBefore = Guests(array);
        // Consumed queue work must release its actual fragment roots before
        // collection of any now-dead on_heap/native argument copies.
        object tracker = Property(actual.Mailbox, Retirements);
        var pending = new List<Task>();
        lock (tracker.GetType().GetField(TrackerGate, Members)!.GetValue(tracker)!)
            foreach (string list in new[] { Active, Failed })
                foreach (object work in (IEnumerable)tracker.GetType().GetField(list, Members)!.GetValue(tracker)!)
                    pending.Add((Task)Property(Property(work, Completion), CompletionTask));
        await Task.WhenAll(pending).WaitAsync(Deadline);
        await parent.InvokeAsync(async context =>
        {
            Term answer = await runtime.Modules.Call(context, ErlangModuleNames.Module, GarbageCollect);
            if (!answer.Equals(Term.A(ErlangBooleanAtoms.True))) throw new InvalidOperationException(Failure);

            return true;
        }).WaitAsync(Deadline);
        int after = Headers(heap, array);
        long guestAfter = Guests(array);
        Console.WriteLine(Prefix + JsonSerializer.Serialize(new
        {
            Parent = actual.Self.ToString(),
            HeadersBefore = before,
            HeadersAfter = after,
            GuestBefore = guestBefore,
            GuestAfter = guestAfter
        }));
        if (after != 0) throw new InvalidOperationException(Failure);
    }

    private static int Headers(object heap, object array)
    {
        int count = 0;
        lock (Property(heap, HeapGate))
            foreach (object header in ((IDictionary)heap.GetType().GetField(MagicReferences, Members)!.GetValue(heap)!).Values)
                if (header.GetType().GetField(CounterLease, Members)!.GetValue(header) is { } lease
                    && (bool)Call(lease, MatchesArray, array)!) count++;

        return count;
    }

    private static long Guests(object array)
    {
        lock (array.GetType().GetField(ArrayGate, Members)!.GetValue(array)!)
            return (long)array.GetType().GetField(GuestRoots, Members)!.GetValue(array)!;
    }
}
