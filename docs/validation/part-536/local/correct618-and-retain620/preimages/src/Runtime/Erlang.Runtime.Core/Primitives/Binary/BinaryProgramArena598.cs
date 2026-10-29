using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

// Complete source-sized magic Binary + actual BMData/ACTrie/node/tables.
// Reader/producer authority belongs to BinaryProgramBacking598, not this view.
internal sealed unsafe class BinaryProgramArena598
{
    private nint memory;
    private readonly nint allocationBase;
    private readonly nuint allocationBytes;
    private nuint cursor;
    private long allocatedNodes;
    internal BinaryArenaKind598 Kind598 { get; }
    internal bool IsAllocated598 => Volatile.Read(ref memory) != 0;
    internal nuint AllocationBytes598 => allocationBytes;
    internal nint Binary598 { get { RequireLive598(); return memory; } }
    internal nint Data598 { get { RequireLive598(); return memory + BinaryArenaNames598.PrefixBytes; } }
    internal static nint BmDestructor598 => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&CleanupBm;
    internal static nint AcDestructor598 => (nint)(delegate* unmanaged[Cdecl]<nint, int>)&CleanupAc;

    internal BinaryProgramArena598(BinaryArenaKind598 kind, long totalPatternBytes, int singleLength)
    {
        if (IntPtr.Size != BinaryArenaNames598.PointerBytes || sizeof(NativeResourceMagicHeader527) != BinaryArenaNames598.PrefixBytes
            || sizeof(BinaryBmData598) != BinaryArenaNames598.BmBytes || sizeof(BinaryAcTrie598) != BinaryArenaNames598.AcTrieBytes
            || sizeof(BinaryAcNode598) != BinaryArenaNames598.AcNodeBytes || BmDestructor598 == AcDestructor598
            || Marshal.OffsetOf<BinaryAcNode598>(nameof(BinaryAcNode598.Transitions)).ToInt32() != BinaryArenaNames598.AcTransitionsOffset)
            throw new PlatformNotSupportedException(BinaryArenaNames598.Invalid);
        Kind598 = kind;
        nuint payload;
        try
        {
            checked
            {
                if (kind == BinaryArenaKind598.BoyerMoore)
                {
                    if (singleLength <= 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    payload = Align598(BinaryArenaNames598.BmBytes) + Align598((nuint)singleLength);
                    if (singleLength > 1)
                        payload += Align598((nuint)singleLength * (nuint)sizeof(nint))
                            + Align598(BinaryArenaNames598.Alphabet * (nuint)sizeof(nint));
                }
                else
                {
                    if (totalPatternBytes <= 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    payload = Align598(BinaryArenaNames598.AcTrieBytes)
                        + Align598(BinaryArenaNames598.AcNodeBytes) * ((nuint)totalPatternBytes + 1);
                }
                allocationBytes = payload + BinaryArenaNames598.PrefixBytes;
                if (allocationBytes > (nuint)nint.MaxValue) throw new OverflowException();
            }
        }
        catch (OverflowException) { throw new ErlangException(ErlangErrorReasons.SystemLimit); }
        void* selected = NativeMemory.AllocZeroed(allocationBytes);
        if (selected == null) throw new OutOfMemoryException();
        memory = allocationBase = (nint)selected;
        var prefix = (NativeResourceMagicHeader527*)selected;
        prefix->Flags = NativeResourceMagic527.Magic;
        prefix->ApparentSize = payload;
        prefix->OriginalSize = allocationBytes - BinaryArenaNames598.BinaryBytesOffset;
        prefix->Destructor = kind == BinaryArenaKind598.BoyerMoore ? BmDestructor598 : AcDestructor598;
        prefix->Allocator = BinaryArenaNames598.AllocatorDispatch;
        cursor = (nuint)BinaryArenaNames598.PrefixBytes;
    }

    private static nuint Align598(nuint count) => checked((count + BinaryArenaNames598.PointerBytes - 1) & ~(nuint)(BinaryArenaNames598.PointerBytes - 1));
    private nint Allocate598(nuint bytes)
    {
        RequireLive598();
        nuint count = Align598(bytes), at = cursor;
        if (count > allocationBytes - at) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        cursor = checked(at + count);
        return memory + (nint)at;
    }

    internal void BeginBm598(int length)
    {
        if (Kind598 != BinaryArenaKind598.BoyerMoore) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var bm = (BinaryBmData598*)Allocate598((nuint)sizeof(BinaryBmData598));
        bm->Length = length;
        bm->Needle = Allocate598((nuint)length);
        if (length > 1)
        {
            bm->GoodShift = Allocate598(checked((nuint)length * (nuint)sizeof(nint)));
            bm->BadShift = Allocate598(BinaryArenaNames598.Alphabet * (nuint)sizeof(nint));
        }
    }

    internal void WriteNeedle598(int index, byte value)
    {
        var bm = Bm598();
        if ((uint)index >= (nuint)bm->Length) throw new ArgumentOutOfRangeException(nameof(index));
        ((byte*)bm->Needle)[index] = value;
    }

    internal byte Needle598(int index)
    {
        var bm = Bm598();
        if ((uint)index >= (nuint)bm->Length) throw new ArgumentOutOfRangeException(nameof(index));
        return ((byte*)bm->Needle)[index];
    }

    internal int BmLength598 => checked((int)Bm598()->Length);
    internal void SetGood598(int index, long value) => *Shift598(true, index) = (nint)value;
    internal void SetBad598(int index, long value) => *Shift598(false, index) = (nint)value;
    internal long Good598(int index) => *Shift598(true, index);
    internal long Bad598(byte index) => *Shift598(false, index);
    private nint* Shift598(bool good, int index)
    {
        var bm = Bm598();
        long limit = good ? bm->Length : BinaryArenaNames598.Alphabet;
        if (bm->Length <= 1 || index < 0 || index >= limit) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        nint pointer = good ? bm->GoodShift : bm->BadShift;
        RequireRange598(pointer, checked((nuint)limit * (nuint)sizeof(nint)));
        return (nint*)pointer + index;
    }

    private BinaryBmData598* Bm598()
    {
        if (Kind598 != BinaryArenaKind598.BoyerMoore) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var bm = (BinaryBmData598*)Data598;
        if (bm->Length <= 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        RequireRange598(bm->Needle, (nuint)bm->Length);
        return bm;
    }

    internal void BeginAc598()
    {
        if (Kind598 != BinaryArenaKind598.AhoCorasick) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var trie = (BinaryAcTrie598*)Allocate598((nuint)sizeof(BinaryAcTrie598));
        trie->Root = NewAcNode598(0, 0);
    }

    internal nint AcRoot598 => Trie598()->Root;
    internal uint NextPattern598()
    {
        var trie = Trie598();
        return trie->Counter = checked(trie->Counter + 1);
    }

    internal nint NewAcNode598(uint depth, nint failure)
    {
        nint pointer = Allocate598((nuint)sizeof(BinaryAcNode598));
        var node = (BinaryAcNode598*)pointer;
        node->Depth = depth;
        node->Failure = failure;
        allocatedNodes++;
        return pointer;
    }

    internal nint Transition598(nint node, byte input)
    {
        var source = Node598(node);
        nint target = (nint)source->Transitions[input];
        if (target != 0) _ = Node598(target);
        return target;
    }

    internal void SetTransition598(nint node, byte input, nint target)
    {
        _ = Node598(target);
        Node598(node)->Transitions[input] = (ulong)target;
    }

    internal uint Depth598(nint node) => Node598(node)->Depth;
    internal int Final598(nint node) => Node598(node)->Final;
    internal void SetFinal598(nint node, int value) => Node598(node)->Final = value;
    internal nint Failure598(nint node)
    {
        nint failure = Node598(node)->Failure;
        if (failure != 0) _ = Node598(failure);
        return failure;
    }

    internal void SetFailure598(nint node, nint failure)
    {
        _ = Node598(failure);
        Node598(node)->Failure = failure;
    }

    internal long Nodes598 => allocatedNodes;
    private BinaryAcTrie598* Trie598()
    {
        if (Kind598 != BinaryArenaKind598.AhoCorasick) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        return (BinaryAcTrie598*)Data598;
    }

    private BinaryAcNode598* Node598(nint pointer)
    {
        RequireRange598(pointer, (nuint)sizeof(BinaryAcNode598));
        nint first = Data598 + BinaryArenaNames598.AcTrieBytes;
        nint distance = pointer - first;
        if (distance < 0 || distance % BinaryArenaNames598.AcNodeBytes != 0
            || distance / BinaryArenaNames598.AcNodeBytes >= allocatedNodes)
            throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        return (BinaryAcNode598*)pointer;
    }

    private void RequireRange598(nint pointer, nuint size)
    {
        RequireLive598();
        nuint offset = unchecked((nuint)(pointer - memory));
        if (offset < BinaryArenaNames598.PrefixBytes || offset > allocationBytes || size > allocationBytes - offset)
            throw new InvalidOperationException(BinaryArenaNames598.Invalid);
    }

    private void RequireLive598()
    {
        if (memory == 0) throw new ObjectDisposedException(BinaryArenaNames598.Closed);
        if (memory != allocationBase) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var h = (NativeResourceMagicHeader527*)memory;
        if (h->Flags != NativeResourceMagic527.Magic || h->OriginalSize != allocationBytes - BinaryArenaNames598.BinaryBytesOffset
            || h->ApparentSize != allocationBytes - BinaryArenaNames598.PrefixBytes
            || h->Destructor != (Kind598 == BinaryArenaKind598.BoyerMoore ? BmDestructor598 : AcDestructor598))
            throw new InvalidOperationException(BinaryArenaNames598.Invalid);
    }

    internal void BindNumbers598(IReadOnlyList<uint> words)
    {
        RequireLive598();
        if (words.Count != ReferenceIdentityLayout.LocalWords || (words[1] & ReferenceNumberLayout.MagicMarker) == 0)
            throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var prefix = (NativeResourceMagicHeader527*)memory;
        prefix->Ref0 = words[0]; prefix->Ref1 = words[1]; prefix->Ref2 = words[2];
    }

    internal IReadOnlyList<uint> ReadNumbers598()
    {
        RequireLive598();
        var prefix = (NativeResourceMagicHeader527*)memory;
        if ((prefix->Ref1 & ReferenceNumberLayout.MagicMarker) == 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        return new uint[] { prefix->Ref0, prefix->Ref1, prefix->Ref2 };
    }

    internal void WritePhysicalRefs598(long count)
    {
        RequireLive598();
        if (count < 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        ((NativeResourceMagicHeader527*)memory)->RefCount = count;
    }

    internal void ValidatePhysicalRefs598(long count)
    {
        RequireLive598();
        if (((NativeResourceMagicHeader527*)memory)->RefCount != count) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
    }
    // Caller owns the backing admission monitor. Detach before any late Host
    // release can write the native prefix; real free occurs outside monitors.
    internal nint DetachForRetirement598()
    {
        RequireLive598();
        return Interlocked.Exchange(ref memory, 0);
    }

    internal void FreeDetachedAllocation598(nint selected)
    {
        if (selected != allocationBase || Volatile.Read(ref memory) != 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        nint destructor = Kind598 == BinaryArenaKind598.BoyerMoore ? BmDestructor598 : AcDestructor598;
        if (((delegate* unmanaged[Cdecl]<nint, int>)destructor)(selected) != 1)
            throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        NativeMemory.Free((void*)selected);
    }

    internal void FreeActualArena598() => FreeDetachedAllocation598(DetachForRetirement598());
    // Pinned cleanup_my_data_bm/ac return1; common magic-binary free owns the
    // actual arena allocation. These are not NIF resource-type callbacks.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int CleanupBm(nint binary) => 1;
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int CleanupAc(nint binary) => 1;
}
