using System.Reflection;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeMagicCleanup576Names;

namespace Erlang.Tests;

internal static class NativeMagicCleanup576Checks
{
    internal static async Task EarlyFailureRestoresAndDrains()
    {
        var sentinel = new InvalidOperationException(Sentinel);
        Exception? observed = null;
        object? capturedHeap = null;
        object? capturedPayload = null;
        bool corrupted = false;
        bool restored = false;
        try
        {
            await NativeMagicReference576Fixture.Run(async fixture =>
            {
                object heap = await fixture.CreateHeap();
                capturedHeap = heap;
                object payload = fixture.CreatePayload();
                capturedPayload = payload;
                var source = (ReferenceTerm)Call(Property(fixture.Runtime, References), Make, payload)!;
                IDisposable roots = fixture.EnterRoot(heap);
                int slot = (int)Call(roots, Reserve)!;
                var owned = (ReferenceTerm)Call(
                    roots,
                    Adopt,
                    slot,
                    source
                )!;
                object block = Property(owned, Storage);
                object cells = Property(Property(block, Slab), Cells);
                int offset = (int)Property(block, Offset);
                nint binary = (nint)Call(payload, BinaryPointer)!;
                nint user = (nint)Property(payload, UserPointer);
                Check(binary != user && ReadNative(payload) == 1);
                fixture.TrackRestore(() =>
                {
                    lock (Property(heap, HeapGate))
                    {
                        Call(
                            cells,
                            WriteWord,
                            new nuint[] { (nuint)binary },
                            offset + BinaryWord
                        );
                        Check((nuint)Call(cells, ReadWord, offset + BinaryWord)! == (nuint)binary);
                        restored = true;
                    }
                });
                lock (Property(heap, HeapGate))
                {
                    Call(
                        cells,
                        WriteWord,
                        new nuint[] { (nuint)user },
                        offset + BinaryWord
                    );
                    corrupted = (nuint)Call(cells, ReadWord, offset + BinaryWord)! == (nuint)user;
                }
                Check(corrupted && !restored && ReadNative(payload) == 1);
                // The fixture alone must restore cells and release native=1.
                throw sentinel;
            });
        }
        catch (Exception error)
        {
            observed = error;
        }
        Check(ReferenceEquals(observed, sentinel));
        Check(corrupted && restored && capturedHeap is not null && capturedPayload is not null);
        object finalHeap = capturedHeap!;
        object finalPayload = capturedPayload!;
        Check(ReadNative(finalPayload) == 0);
        Check(((Task)Property(finalPayload, Retired)).IsCompletedSuccessfully);
        Check(((Task)Call(finalHeap, Retire)!).IsCompletedSuccessfully);
        object snapshot = Property(finalHeap, Snapshot);
        Check((long)Property(snapshot, Capacity) == 0);
        Check((int)Property(snapshot, Blocks) == 0 && (int)Property(snapshot, Frames) == 0);
    }

    private static long ReadNative(object payload)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = payload.GetType().GetField(PayloadGate, flags)!.GetValue(payload)!;
        lock (gate)
            return (long)payload.GetType().GetField(NativeCount, flags)!.GetValue(payload)!;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
