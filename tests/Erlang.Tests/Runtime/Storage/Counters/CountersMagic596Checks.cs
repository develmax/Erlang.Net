using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.CountersMagic596Names;

namespace Erlang.Tests;

internal static class CountersMagic596Checks
{
    internal static Task PhysicalHeaders() => CountersCustody596Checks.Run(async f =>
    {
        IDisposable pin = f.Lease(CountersCustody596Names.GuestKind);
        await using var actor = new RuntimeNodeFoundationHostActor(f.Runtime);
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int firstSlot = (int)Call(frame, Reserve)!;
        int secondSlot = (int)Call(frame, Reserve)!;
        int aliasSlot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            firstSlot,
            f.Reference
        );
        Call(
            frame,
            Set,
            secondSlot,
            f.Reference
        );
        var first = (ReferenceTerm)Call(frame, Read, firstSlot)!;
        var second = (ReferenceTerm)Call(frame, Read, secondSlot)!;
        Call(
            frame,
            Set,
            aliasSlot,
            first
        );
        var alias = (ReferenceTerm)Call(frame, Read, aliasSlot)!;
        object firstBlock = Property(first, OwnedStorage);
        object secondBlock = Property(second, OwnedStorage);
        Require(!ReferenceEquals(firstBlock, secondBlock) && ReferenceEquals(firstBlock, Property(alias, OwnedStorage)));
        nint binary = (nint)Call(f.Array, CountersCustody596Names.ReadBinary, pin)!;
        Require(Word(heap, firstBlock, 0) == Header && Word(heap, secondBlock, BinaryWord) == binary.ToInt64());
        Require(Word(heap, secondBlock, NextWord) == AddressOf(heap, firstBlock).ToInt64());
        uint before = first.Words[0];
        int saved = Marshal.ReadInt32(binary, Reference0Offset);
        try
        {
            Marshal.WriteInt32(binary, Reference0Offset, saved ^ 1);
            Require(first.Words[0] == (before ^ 1) && second.Words[0] == (before ^ 1) && f.Reference.Words[0] == before);
        }
        finally
        {
            Marshal.WriteInt32(binary, Reference0Offset, saved);
        }
        f.ReleaseProducer();
        pin.Dispose();
        Call(frame, Clear, firstSlot);
        Call(frame, Clear, aliasSlot);
        Call(heap, Collect);
        Require((bool)Property(f.Array, CountersCustody596Names.Allocated));
        Call(frame, Clear, secondSlot);
        Call(heap, Collect);
        await ((Task)Property(f.Array, CountersCustody596Names.Retired)).WaitAsync(Deadline);
        Require(!(bool)Property(f.Array, CountersCustody596Names.Allocated));
        GC.KeepAlive(f.Reference);
    });

    internal static Task Generations() => CountersCustody596Checks.Run(async f =>
    {
        IDisposable pin = f.Lease(CountersCustody596Names.GuestKind);
        await using var actor = new RuntimeNodeFoundationHostActor(f.Runtime);
        object heap = Property(await actor.Ready.WaitAsync(Deadline), Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            f.Reference
        );
        f.ReleaseProducer();
        pin.Dispose();
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(frame, Clear, slot);
        Call(
            heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Require((bool)Property(f.Array, CountersCustody596Names.Allocated));
        Call(
            heap,
            CollectGeneration,
            true,
            ushort.MaxValue
        );
        await ((Task)Property(f.Array, CountersCustody596Names.Retired)).WaitAsync(Deadline);
    });

    internal static Task ForeignBeforeStale() => CountersCustody596Checks.Run(async f =>
    {
        IDisposable pin = f.Lease(CountersCustody596Names.GuestKind);
        await using var actor = new RuntimeNodeFoundationHostActor(f.Runtime);
        object heap = Property(await actor.Ready.WaitAsync(Deadline), Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            f.Reference
        );
        var stale = (ReferenceTerm)Call(frame, Read, slot)!;
        f.ReleaseProducer();
        pin.Dispose();
        Call(frame, Clear, slot);
        Call(heap, Collect);
        await ((Task)Property(f.Array, CountersCustody596Names.Retired)).WaitAsync(Deadline);
        await using var foreign = OwnedGuest506RuntimeFactory.Create();
        MethodInfo direct = typeof(ProcessRuntime).Assembly.GetType(RootsType, true)!.GetMethod(DirectRoot, BindingFlags.Static | BindingFlags.NonPublic)!;
        Expect<NotSupportedException>(() => direct.Invoke(null, [stale, foreign]));
        Expect<ObjectDisposedException>(() => direct.Invoke(null, [stale, f.Runtime]));
    });

    private static nint AddressOf(object heap, object block)
    {
        lock (Property(heap, Gate))
            return (nint)Property(Call(
                heap,
                CaptureAddress,
                block,
                0,
                Words
            )!, Address);
    }

    private static long Word(object heap, object block, int index)
    {
        lock (Property(heap, Gate))
            return Marshal.ReadInt64(AddressOf(heap, block), index * IntPtr.Size);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(CountersCustody596Names.Failure);
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (TargetInvocationException failure) when (failure.InnerException is T)
        {
            return;
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(CountersCustody596Names.Failure);
    }
}
