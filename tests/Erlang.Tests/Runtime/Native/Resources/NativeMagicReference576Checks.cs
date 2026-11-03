using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeMagicReference576Names;

namespace Erlang.Tests;

// These finite controls use the real native allocation provider; they are not
// original-OTP observations or a public heap-size compatibility claim.
internal static class NativeMagicReference576Checks
{
    internal static Task PhysicalAliasAndReader() => NativeMagicReference576Fixture.Run(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = await fixture.CreateHeap();
        object payload = fixture.CreatePayload();
        ReferenceTerm source = Make(runtime, payload);
        ReferenceTerm second = Make(runtime, payload);
        Verify(!ReferenceEquals(source, second) && source.Equals(second));
        var roots = fixture.EnterRoot(heap);
        int slot = (int)Call(roots, "Reserve")!;
        var tuple = (TupleTerm)Call(
            roots,
            "AdoptGuestInto507",
            slot,
            Term.Tuple(source, source, second)
        )!;
        var firstOwned = (ReferenceTerm)tuple.Items[0];
        var secondOwned = (ReferenceTerm)tuple.Items[2];
        Verify(ReferenceEquals(firstOwned, tuple.Items[1]) && !ReferenceEquals(firstOwned, secondOwned));
        Verify(firstOwned.Equals(secondOwned) && !ReferenceEquals(Property(firstOwned, Storage), Property(secondOwned, Storage)));
        Verify((long)Property(Property(heap, Snapshot), Physical) == 10);
        Verify((long)Property(Property(heap, Snapshot), Numeric) == 4);
        Verify((long)Property(Property(heap, Snapshot), Header) == 3);
        object block = Property(firstOwned, Storage);
        object cells = Property(Property(block, "Slab"), "Cells");
        int offset = (int)Property(block, "AllocationOffset526");
        nint binary = (nint)Call(payload, "MagicBinaryPointer576")!;
        lock (Property(heap, Gate))
        {
            Verify((nuint)Call(cells, "ReadHeader526", offset)! == 0x90);
            Verify((nuint)Call(cells, "ReadNumeric556", offset + 1)! == (nuint)binary);
        }
        uint original = firstOwned.Words[0];
        int saved = Marshal.ReadInt32(binary, NativeResource527TestLayout.Reference0);
        int prefixRestore = fixture.TrackRestore(() => Marshal.WriteInt32(binary, NativeResource527TestLayout.Reference0, saved));
        Marshal.WriteInt32(binary, NativeResource527TestLayout.Reference0, saved ^ 1);
        Verify(firstOwned.Words[0] == (original ^ 1u) && secondOwned.Words[0] == (original ^ 1u));
        Verify(source.Words[0] == original);
        fixture.Restore(prefixRestore);
        nint user = (nint)Property(payload, "Pointer524");
        int pointerRestore = fixture.TrackRestore(() =>
        {
            lock (Property(heap, Gate))
                Call(
                    cells,
                    "WriteNumeric556",
                    new nuint[] { (nuint)binary },
                    offset + 1
                );
        });
        lock (Property(heap, Gate))
            Call(
                cells,
                "WriteNumeric556",
                new nuint[] { (nuint)user },
                offset + 1
            );
        Expect<InvalidOperationException>(() => _ = firstOwned.Words);
        fixture.Restore(pointerRestore);
        fixture.ReleaseNative();
        Call(roots, "Clear", slot);
        Call(heap, "Collect");
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
        Verify((long)Property(Property(heap, Snapshot), Physical) == 0);
        Expect<ObjectDisposedException>(() => _ = firstOwned.Words);
    });

    internal static Task GenerationsAndRollback() => NativeMagicReference576Fixture.Run(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = await fixture.CreateHeap();
        object payload = fixture.CreatePayload();
        ReferenceTerm source = Make(runtime, payload);
        var roots = fixture.EnterRoot(heap);
        int slot = (int)Call(roots, "Reserve")!;
        Call(
            roots,
            "Set",
            slot,
            source
        );
        var owned = (ReferenceTerm)Call(roots, "Read", slot)!;
        Call(
            heap,
            "CollectGeneration508",
            false,
            ushort.MaxValue
        );
        Call(
            heap,
            "CollectGeneration508",
            false,
            ushort.MaxValue
        );
        Call(roots, "Clear", slot);
        Call(
            heap,
            "CollectGeneration508",
            false,
            ushort.MaxValue
        );
        Verify((long)Property(Property(heap, Snapshot), Physical) == 3 && owned.Equals(source));
        Call(heap, "Collect");
        Verify((long)Property(Property(heap, Snapshot), Physical) == 0);
        Expect<ObjectDisposedException>(() => _ = owned.Words);
        {
            var provisional = fixture.EnterRoot(heap);
            object custody = Call(provisional, "BeginConstruction514", source)!;
            lock (Property(heap, Gate))
                Call(
                    heap,
                    "CreateMagicReference576",
                    source,
                    custody
                );
            Verify((long)Property(Property(heap, Snapshot), Physical) == 3);
            fixture.DisposeFrame(provisional);
        }
        Verify((long)Property(Property(heap, Snapshot), Physical) == 0);
        // A different value-equal wrapper is not an input capability.
        {
            var provisional = fixture.EnterRoot(heap);
            object custody = Call(provisional, "BeginConstruction514", source)!;
            ReferenceTerm impostor = Make(runtime, payload);
            lock (Property(heap, Gate))
                Expect<InvalidOperationException>(() => Call(
                    heap,
                    "CreateMagicReference576",
                    impostor,
                    custody
                ));
            fixture.DisposeFrame(provisional);
        }
        Verify((long)Property(Property(heap, Snapshot), Physical) == 0);
        fixture.ReleaseNative();
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
    });

    internal static Task HostEscapeAndForeignCustody() => NativeMagicReference576Fixture.Run(async fixture =>
    {
        ProcessRuntime runtime = fixture.Runtime;
        object heap = await fixture.CreateHeap();
        object destination = await fixture.CreateHeap();
        object payload = fixture.CreatePayload();
        ReferenceTerm source = Make(runtime, payload);
        var roots = fixture.EnterRoot(heap);
        var received = fixture.EnterRoot(destination);
        int slot = (int)Call(roots, "Reserve")!;
        int otherSlot = (int)Call(received, "Reserve")!;
        Call(
            roots,
            "Set",
            slot,
            source
        );
        var owned = (ReferenceTerm)Call(roots, "Read", slot)!;
        Expect<InvalidOperationException>(() => Call(
            received,
            "Set",
            otherSlot,
            owned
        ));
        Verify((long)Property(Property(destination, Snapshot), Physical) == 0);
        Expect<NotSupportedException>(() => AdmitHost(runtime, (Term)Call(
            heap,
            "Export",
            roots,
            owned
        )!));
        fixture.WithHost(() =>
        {
            var portable = (ReferenceTerm)AdmitHost(runtime, (Term)Call(
                heap,
                "Export",
                roots,
                owned
            )!);
            Verify(Property(portable, Storage) is null && portable.Equals(source));
            fixture.ReleaseNative();
            Call(roots, "Clear", slot);
            Call(heap, "Collect");
            Expect<ObjectDisposedException>(() => _ = owned.Words);
            Verify(portable.Equals(source));
            Call(
                received,
                "Set",
                otherSlot,
                portable
            );
        });
        var copied = (ReferenceTerm)Call(received, "Read", otherSlot)!;
        Verify(copied.Equals(source) && Property(copied, Storage) is not null);
        Verify((long)Property(Property(destination, Snapshot), Physical) == 3);
        Call(received, "Clear", otherSlot);
        Call(destination, "Collect");
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
        Expect<ObjectDisposedException>(() => _ = copied.Words);
    });

    private static Term AdmitHost(ProcessRuntime runtime, Term value)
    {
        var method = typeof(NativeTermHostScope524).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == "AdmitCurrent524" && candidate.GetParameters().Length == 2);
        try
        {
            return (Term)method.Invoke(null, new object[] { runtime, value })!;
        }
        catch (System.Reflection.TargetInvocationException error) when (error.InnerException is { } inner)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static ReferenceTerm Make(ProcessRuntime runtime, object payload)
        => (ReferenceTerm)Call(Property(runtime, "ReferenceResources"), "MakeNativeResource524", payload)!;

    private static void Verify(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(Failure);
    }
}
