using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.BinaryOwned512Names;

namespace Erlang.Tests;

internal static class BinaryOwned512Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task SharingAndEscapes()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actorA = new RuntimeNodeFoundationHostActor(runtime);
        await using var actorB = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext contextA = await actorA.Ready, contextB = await actorB.Ready;
        object heapA = Property(contextA, Heap), heapB = Property(contextB, Heap);
        using var frameA = (IDisposable)Call(heapA, Enter)!;
        using var frameB = (IDisposable)Call(heapB, Enter)!;
        int slotA = (int)Call(frameA, Reserve)!, slotB = (int)Call(frameB, Reserve)!;
        byte[] data = new byte[PayloadBytes];
        var binary = (BitString)Call(
            frameA,
            Allocate,
            slotA,
            PayloadBytes
        )!;
        BitString slice = binary.SliceBits(512, 1024);
        Check(ReferenceEquals(Backing(binary), Backing(slice)));
        // AllocateBinaryInto512 deliberately returns a portable shared backing.
        // Keep this512 provider test on its real non-copying local-root API;
        // Set now performs native adoption, which is a separate contract.
        Call(
            frameA,
            SetLocal,
            slotA,
            binary
        );
        Call(
            frameB,
            SetLocal,
            slotB,
            Term.Tuple(slice, slice)
        );
        object total = Total(runtime);
        Check((long)Property(total, ReferenceBytes) == PayloadBytes);
        Check((int)Property(total, Distinct) == 1);
        Check((long)Property(total, References) == 2);
        Call(frameA, Clear, slotA);
        Check((long)Property(Property(heapA, BinarySnapshot), ReferenceBytes) == 0);
        Check((long)Property(Total(runtime), ReferenceBytes) == PayloadBytes);
        // Public send prepares a real recipient fragment. Its queued native
        // backing must remain rooted after the explicit provider slots clear.
        contextB.Mailbox.Send(slice);
        BitString queued = QueuedBinary(contextB.Mailbox);
        object queuedPayload = Call(queued, Capture)!;
        long queuedCapacity = (long)Property(queuedPayload, CapacityBytes);
        Check(queued.Equals(slice)
            && Property(queuedPayload, NativeIdentity) is not null
            && queuedCapacity >= slice.ToArray().Length);
        Call(frameB, Clear, slotB);
        Call(heapA, Collect);
        Call(heapB, Collect);
        object pending = Total(runtime);
        Check((long)Property(pending, ReferenceBytes) == queuedCapacity
            && (int)Property(pending, Distinct) == 1
            && (long)Property(pending, References) > 0);
        Term? received = await contextB.Mailbox.ReceiveAsync<Term>(value => value, TimeSpan.Zero);
        Check(received is BitString);
        var message = (BitString)received!;
        Check(message.Equals(slice)
            && Property(Call(message, Capture)!, NativeIdentity) is null
            && heapB.GetType().GetMethod(Storage, Static)!.Invoke(null, [message]) is null);
        // Drop the external test frames before joining actual process/queue
        // retirement; otherwise those frames themselves keep the heap open.
        frameA.Dispose();
        frameB.Dispose();
        await actorB.DisposeAsync();
        Check((long)Property(Total(runtime), ReferenceBytes) == 0
            && (int)Property(Total(runtime), Distinct) == 0
            && (long)Property(Total(runtime), References) == 0);
        Check(message.Equals(slice) && message.ToArray().All(value => value == 0));
        Check(binary.ToArray().SequenceEqual(data) && slice.ToArray().All(value => value == 0));
    }

    internal static async Task SourceAndRollback()
    {
        using var artifact = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        artifact.Definition.Register(runtime.Modules);
        var binary = new BitString(new byte[PayloadBytes]);
        bool observed = false;
        runtime.Modules.Register(
            ProbeModule,
            Probe,
            1,
            (context, arguments) =>
        {
            Check(arguments[0] is BitString value && value.Equals(binary)
                && Property(Call(value, Capture)!, NativeIdentity) is null);
            // Public opaque callbacks receive a detached Host value. Inspect
            // the genuine Source dictionary cell separately for native custody.
            object dictionary = context.Dictionary;
            object dictionaryGate = dictionary.GetType().GetField(DictionaryGate, Members)!.GetValue(dictionary)!;
            TupleTerm retained;
            lock (dictionaryGate)
            {
                var data = (Dictionary<Term, Term>)dictionary.GetType().GetField(DictionaryData, Members)!.GetValue(dictionary)!;
                retained = (TupleTerm)data[Term.A(DictionaryKey)];
            }
            // This real actor's synchronous callback leaves the dictionary
            // entry rooted. Drop its metadata gate before native tuple reads,
            // which enter the actual process heap gate.
            Check(retained.Items.Count == 2 && retained.Items[0] is BitString
                && ReferenceEquals(retained.Items[0], retained.Items[1]));
            var guest = (BitString)retained.Items[0];
            Check(guest.Equals(binary) && Property(Call(guest, Capture)!, NativeIdentity) is not null
                && !ReferenceEquals(Backing(guest), Backing((BitString)arguments[0])));
            Check((long)Property(Property(Property(context, Heap), BinarySnapshot), ReferenceBytes) == PayloadBytes);
            observed = true;

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                SourceModule,
                Run,
                binary
            ));
        ProcessContext owner = await actor.Ready;
        object heap = Property(owner, Heap);
        Check(observed && result is TupleTerm { Items.Count: 2 });
        // copy_struct deliberately copies each boxed occurrence on Host escape.
        // Source dictionary B/B aliasing was checked above against actual native
        // cells; returned CLR wrappers need not retain that reference identity.
        foreach (Term returned in ((TupleTerm)result).Items)
        {
            Check(returned is BitString);
            var actual = (BitString)returned;
            object payload = Call(actual, Capture)!;
            Check(actual.BitLength == binary.BitLength
                && actual.ToArray().AsSpan().SequenceEqual(binary.ToArray()));
            Check(Property(payload, NativeIdentity) is null
                && (bool)Property(payload, ReferenceCounted)
                && (long)Property(payload, CapacityBytes) == PayloadBytes);
            Check(heap.GetType().GetMethod(Storage, Static)!.Invoke(null, [actual]) is null);
        }
        Call(heap, Collect);
        Check((long)Property(Total(runtime), ReferenceBytes) == 0);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            binary
        );
        var previous = (BitString)Call(frame, Read, slot)!;
        Check(previous.Equals(binary) && Property(Call(previous, Capture)!, NativeIdentity) is not null);
        object previousBacking = Backing(previous);
        object mirror = frame.GetType().GetField(BinaryRootsField, Members)!.GetValue(frame)!;
        using (var publication = (IDisposable)Call(
            mirror,
            Prepare,
            slot,
            new BitString(new byte[PayloadBytes * 2])
        )!)
            Check((int)Property(Total(runtime), Distinct) == 2);
        Check((int)Property(Total(runtime), Distinct) == 1);
        Check(ReferenceEquals(Call(frame, Read, slot), previous)
            && ReferenceEquals(Backing((BitString)Call(frame, Read, slot)!), previousBacking)
            && previous.Equals(binary));
        Call(frame, Clear, slot);
        Check((long)Property(Total(runtime), ReferenceBytes) == 0);
    }

    internal static Task NativeLifetime()
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(NativeStorageType, true)!;
        long before = (long)type.GetProperty(NativeCount, Static)!.GetValue(null)!;
        object storage = type.GetMethod(AllocateNative, Static)!.Invoke(null, new object[] { (nuint)PayloadBytes, true })!;
        using var lease = (IDisposable)Call(storage, NativeRetain)!;
        Call(storage, NativeRelease);
        Check((long)type.GetProperty(NativeCount, Static)!.GetValue(null)! == before + 1);
        Check((nuint)Property(lease, NativeSize) == (nuint)PayloadBytes);
        byte[] copied = (byte[])Call(
            lease,
            NativeCopy,
            (nuint)0,
            PayloadBytes
        )!;
        Check(copied.Length == PayloadBytes);
        lease.Dispose();
        Check((long)type.GetProperty(NativeCount, Static)!.GetValue(null)! == before);
        Check(copied.Length == PayloadBytes);

        return Task.CompletedTask;
    }

    private static BitString QueuedBinary(Mailbox mailbox)
    {
        // This test has one sender and no concurrent receiver. Snapshot only
        // the Entry reference under M, then inspect native payload outside M.
        object entry;
        object mailboxGate = mailbox.GetType().GetField(MailboxGate, Members)!.GetValue(mailbox)!;
        lock (mailboxGate)
        {
            var messages = (System.Collections.IEnumerable)mailbox.GetType().GetField(MailboxMessages, Members)!.GetValue(mailbox)!;
            entry = messages.Cast<object>().Single();
        }
        var value = (BitString)Property(entry, MailboxEntryValue);
        Check(value is not null);

        return value!;
    }

    private static object Backing(BitString value) => Property(Call(value, Capture)!, Identity);

    private static object Total(ProcessRuntime runtime) => Call(Property(runtime, Registry), RegistrySnapshot, new object?[] { null })!;

    private static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name, params object?[] arguments) => value.GetType().GetMethod(name, Members)!.Invoke(value, arguments);

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
