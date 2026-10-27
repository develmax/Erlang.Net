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
        Call(
            frameA,
            Set,
            slotA,
            binary
        );
        Call(
            frameB,
            Set,
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
        // A pending mailbox value and exported host wrapper own the actual
        // managed backing independently of explicit guest-frame root leases.
        contextB.Mailbox.Send(slice);
        Call(frameB, Clear, slotB);
        Call(heapA, Collect);
        Call(heapB, Collect);
        Check((long)Property(Total(runtime), ReferenceBytes) == 0);
        Term? received = await contextB.Mailbox.ReceiveAsync<Term>(value => value, TimeSpan.Zero);
        Check(received is BitString message && ReferenceEquals(Backing(message), Backing(binary)));
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
            Check(arguments[0] is BitString value && ReferenceEquals(Backing(value), Backing(binary)));
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
        Check(observed && result is TupleTerm tuple && ReferenceEquals(Backing((BitString)tuple.Items[0]), Backing(binary)));
        ProcessContext owner = await actor.Ready;
        object heap = Property(owner, Heap);
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
        object mirror = frame.GetType().GetField(BinaryRootsField, Members)!.GetValue(frame)!;
        using (var publication = (IDisposable)Call(
            mirror,
            Prepare,
            slot,
            new BitString(new byte[PayloadBytes * 2])
        )!)
            Check((int)Property(Total(runtime), Distinct) == 2);
        Check((int)Property(Total(runtime), Distinct) == 1);
        Check(ReferenceEquals(Backing((BitString)Call(frame, Read, slot)!), Backing(binary)));
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
