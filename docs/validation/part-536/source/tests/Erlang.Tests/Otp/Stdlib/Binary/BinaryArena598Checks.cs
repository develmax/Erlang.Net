using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.BinaryArena598Names;

namespace Erlang.Tests;

internal static class BinaryArena598Checks
{
    internal static async Task Physical()
    {
        foreach (Term input in new Term[] { new BitString([1]), new BitString([1, 2, 1]), Term.List(new BitString([1, 2]), new BitString([2])) })
            await BinaryArena598Fixture.Run(
                input,
                async fixture =>
            {
                object frame = fixture.Frame();
                int aSlot = (int)Call(frame, Reserve)!, bSlot = (int)Call(frame, Reserve)!;
                Call(
                    frame,
                    Set,
                    aSlot,
                    fixture.Reference
                );
                Call(
                    frame,
                    Set,
                    bSlot,
                    fixture.Reference
                );
                var a = (ResourceReferenceTerm)Call(frame, Read, aSlot)!;
                var b = (ResourceReferenceTerm)Call(frame, Read, bSlot)!;
                nint aAddress = AddressOf(fixture.HeapOwner, Property(a, Storage));
                nint bAddress = AddressOf(fixture.HeapOwner, Property(b, Storage));
                nint binary = (nint)Marshal.ReadInt64(aAddress, IntPtr.Size);
                Require(aAddress != bAddress && Marshal.ReadInt64(aAddress) == Header);
                Require(Marshal.ReadInt64(bAddress, IntPtr.Size) == binary && Marshal.ReadInt64(bAddress, 2 * IntPtr.Size) == aAddress);
                Require(Marshal.ReadInt64(binary, RefCountOffset) == 2);
                object arena = Property(fixture.BackingOwner, Arena);
                nuint bytes = (nuint)Property(arena, TotalBytes);
                Require((ulong)Marshal.ReadInt64(binary, OriginalSizeOffset) == (ulong)bytes - 32 && Marshal.ReadInt64(binary, DestructorOffset) != 0);
                int saved = Marshal.ReadInt32(binary, Ref0Offset);
                fixture.Restore(() =>
 {
     if (!(bool)Property(fixture.BackingOwner, Freed)) Marshal.WriteInt32(binary, Ref0Offset, saved);
 });
                Marshal.WriteInt32(binary, Ref0Offset, saved ^ 1);
                Require(a.Words[0] == (uint)(saved ^ 1) && b.Words[0] == (uint)(saved ^ 1));
                Marshal.WriteInt32(binary, Ref0Offset, saved);
                fixture.Release();
                Call(frame, Clear, aSlot);
                Call(fixture.HeapOwner, Collect);
                Require(!(bool)Property(fixture.BackingOwner, Freed));
                Call(frame, Clear, bSlot);
                Call(fixture.HeapOwner, Collect);
                await ((Task)Property(fixture.BackingOwner, Retired)).WaitAsync(Deadline);
                Require((bool)Property(fixture.BackingOwner, Freed));
                GC.KeepAlive(a);
                GC.KeepAlive(b);
            }
            );
    }

    internal static Task Active() => BinaryArena598Fixture.Run(
        Term.List(new BitString([1]), new BitString([2, 1])),
        async fixture =>
    {
        object frame = fixture.Frame();
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            fixture.Reference
        );
        IDisposable? reader = (IDisposable)Call(fixture.BackingOwner, Reader)!;
        try
        {
            fixture.Release();
            Call(
                fixture.HeapOwner,
                CollectGeneration,
                false,
                ushort.MaxValue
            );
            Call(
                fixture.HeapOwner,
                CollectGeneration,
                false,
                ushort.MaxValue
            );
            Call(frame, Clear, slot);
            Call(
                fixture.HeapOwner,
                CollectGeneration,
                false,
                ushort.MaxValue
            );
            Require(!(bool)Property(fixture.BackingOwner, Freed));
            Call(
                fixture.HeapOwner,
                CollectGeneration,
                true,
                ushort.MaxValue
            );
            Require(!(bool)Property(fixture.BackingOwner, Freed) && !((Task)Property(fixture.BackingOwner, Retired)).IsCompleted);
        }
        finally
        {
            reader.Dispose();
        }
        await ((Task)Property(fixture.BackingOwner, Retired)).WaitAsync(Deadline);
        Require((bool)Property(fixture.BackingOwner, Freed));
    }
    );

    internal static Task HostReservation() => BinaryArena598Fixture.Run(
        new BitString([1, 2]),
        async fixture =>
    {
        object frame = fixture.Frame();
        int original = (int)Call(frame, Reserve)!, destination = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            original,
            fixture.Reference
        );
        var local = (ResourceReferenceTerm)Call(frame, Read, original)!;
        RuntimeExternalTermReservation? reservation = fixture.Runtime.ReserveExternalTerm(Term.Tuple(local, local));
        try
        {
            fixture.Release();
            Call(frame, Clear, original);
            Call(fixture.HeapOwner, Collect);
            Require(!(bool)Property(fixture.BackingOwner, Freed));
            var exported = (TupleTerm)reservation.Value;
            Require(exported.Items[0].Equals(exported.Items[1]) && Property(exported.Items[0], Storage) is null
                && Property(exported.Items[1], Storage) is null);
            Call(
                frame,
                Set,
                destination,
                exported
            );
            reservation.Dispose();
            reservation = null;
            Call(fixture.HeapOwner, Collect);
            Require(!(bool)Property(fixture.BackingOwner, Freed));
            Call(frame, Clear, destination);
            Call(fixture.HeapOwner, Collect);
            await ((Task)Property(fixture.BackingOwner, Retired)).WaitAsync(Deadline);
            Require((bool)Property(fixture.BackingOwner, Freed));
        }
        finally
        {
            reservation?.Dispose();
        }
    }
    );

    internal static async Task AcRootMetadata()
    {
        foreach (int distinct in new[] { 2, SourceAcRootSimdMax, SourceAcRootSimdMax + 1, SourceAcRootSimdMax + 8 })
        {
            byte[] roots = Enumerable.Range(0, distinct).Select(index => checked((byte)(index * RootByteStride))).ToArray();
            var patterns = roots.Select(root => (Term)new BitString([root, CommonSuffix])).ToList();
            // A duplicate whole needle and a new interior edge must not add root bytes.
            patterns.Add(new BitString([roots[0], CommonSuffix]));
            patterns.Add(new BitString([roots[0], AlternateSuffix]));
            await BinaryArena598Fixture.Run(
                Term.List(patterns.ToArray()),
                async fixture =>
            {
                object frame = fixture.Frame();
                int slot = (int)Call(frame, Reserve)!;
                Call(
                    frame,
                    Set,
                    slot,
                    fixture.Reference
                );
                var reference = (ResourceReferenceTerm)Call(frame, Read, slot)!;
                object arena = Property(fixture.BackingOwner, Arena);
                nint data = (nint)Property(arena, Data);
                nuint bytes = (nuint)Property(arena, TotalBytes);
                long expectedBytes = PrefixBytes + SourceAcTrieBytes
                    + ((long)PatternByteCount * (distinct + AddedRootDuplicates) + 1) * SourceAcNodeBytes;
                Require(bytes == checked((nuint)expectedBytes));
                Require((nint)Marshal.ReadInt64(data, SourceAcRootOffset) == data + SourceAcTrieBytes);
                long expectedCount = distinct <= SourceAcRootSimdMax ? distinct : SourceAcRootSimdMax + 1;
                Require(Marshal.ReadInt64(data, SourceAcRootCountOffset) == expectedCount);
                for (int index = 0; index < Math.Min(distinct, SourceAcRootSimdMax); index++)
                    Require(Marshal.ReadByte(data, SourceAcRootBytesOffset + index) == roots[index]);
                fixture.Release();
                Call(frame, Clear, slot);
                Call(fixture.HeapOwner, Collect);
                await ((Task)Property(fixture.BackingOwner, Retired)).WaitAsync(Deadline);
                Require((bool)Property(fixture.BackingOwner, Freed));
                GC.KeepAlive(reference);
            }
            );
        }
    }

    private static nint AddressOf(object heap, object block)
    {
        lock (Property(heap, Gate)) return (nint)Property(Call(
            heap,
            CaptureAddress,
            block,
            0,
            HeaderWords
        )!, Address);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
