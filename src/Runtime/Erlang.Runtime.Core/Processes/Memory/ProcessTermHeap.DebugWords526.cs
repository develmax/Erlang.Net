using System.Numerics;

namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal long PhysicalSizeQueries526 { get; private set; }
    private static readonly BigInteger SmallBound526 = BigInteger.One << DebugSharingWordLayout425.SmallBits;

    internal bool TryReadPhysicalSize526(
        ProcessContext context,
        Term source,
        bool flat,
        out ulong words
    )
    {
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(this, source);
        lock (Gate)
        {
            using var literalAdmission = EnterLiteralAdmission801(literalRoots);
            words = 0;
            var known = new Dictionary<ProcessTermSequence506, ulong>(ReferenceEqualityComparer.Instance);
            var active = new HashSet<ProcessTermSequence506>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<OwnedWordSizeFrame526>();

            bool Enter(Term value, out OwnedWordSizeFrame526? frame, out ulong existingWords)
            {
                frame = null;
                existingWords = 0;
                if (IsAuthenticatedEmptyTuple803(value) || value is Nil or Atom || value is TupleTerm { HasGlobalEmptyStorage428: true }
                    || value is Integer integer && integer.OwnedStorage559 is null
                        && integer.Value >= -SmallBound526
                        && integer.Value < SmallBound526)
                    return true;
                // Reject unrepresented scalar/function/binary/foreign domains;
                // their existing source policy remains the explicit fallback.
                if (value is not (TupleTerm or Cons or MapTerm or HamtBitmapTerm429 or FloatTerm or Integer or ReferenceTerm or BitString or NativeRecordTerm))
                    return false;
                bool literal = IsAuthenticatedLiteral801(value);
                if (!flat && (literal || runtime.IsSharingLiteral425(value))) return true;
                if (!literal && !IsOwnedLocal(value) || Storage(value) is not ProcessTermSequence506 payload)
                    return false;
                ProcessTermHeap506 payloadOwner = (ProcessTermHeap506)payload.Owner;
                using ModuleLiteralRead800? literalRead = literal ? payloadOwner.LiteralDomain800!.EnterRead800(payload) : null;
                bool sharedHamt = false;
                ProcessTermSequence506? binaryReference577 = null;
                if (value is FloatTerm)
                {
                    _ = payloadOwner.ReadFloat556(payload);
                }
                else if (value is Integer)
                {
                    _ = payloadOwner.ReadInteger559(payload);
                }
                else if (value is ReferenceTerm)
                {
                    _ = ReadReferenceWords562(payload);
                }
                else if (value is BitString binary)
                {
                    if (binary.OwnedHeapBits is not null) _ = payloadOwner.ReadHeapBitLength(payload);
                    else
                    {
                        _ = payloadOwner.ReadResourceBits577(payload);
                        binaryReference577 = payloadOwner.RequireResourceBitsDescriptor577(payload).BinRef.Block;
                    }
                }
                else if (value is NativeRecordTerm)
                {
                    _ = RequireRecordDefinition(payload);
                }
                else if (value is TupleTerm)
                {
                    if (payload.HeaderWords526 != 1
                        || payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526) != EncodedMapHeader526.Tuple(payload.Count))
                        throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
                }
                else if (value is Cons)
                {
                    if (payload.HeaderWords526 != 0 || payload.Count != 2)
                        throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
                }
                else
                {
                    OwnedMapHeader517 header = payloadOwner.ReadMapHeader517(payload);
                    sharedHamt = !flat && header.Kind is OwnedMapHeaderKind517.HamtHead or OwnedMapHeaderKind517.HamtBitmap;
                }
                if (known.TryGetValue(payload, out ulong prior))
                {
                    existingWords = flat ? prior : 0;

                    return true;
                }
                if (!active.Add(payload))
                    return false;
                frame = new(payload);
                if (value is NativeRecordTerm) frame = new(payload) { Children = [RequireRecordDefinition(payload), .. payload] };
                if (binaryReference577 is { } binaryReference
                    && (flat || !known.ContainsKey(binaryReference)))
                {
                    ulong binaryWords = checked((ulong)binaryReference.AllocationWords526);
                    frame.Words = checked(frame.Words + binaryWords);
                    if (!flat) known[binaryReference] = binaryWords;
                }
                if (sharedHamt)
                    frame.Words = DebugSharingSizePolicy528.SharedHamtWords(frame.Words);

                return true;
            }
            if (!Enter(source, out var root, out words))
                return false;
            if (root is null)
            {
                unchecked
                {
                    PhysicalSizeQueries526++;
                }

                return true;
            }
            pending.Push(root);
            while (pending.TryPeek(out var frame))
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (frame.Next < frame.Children.Count)
                {
                    Term child = frame.Children[frame.Next++];
                    if (!Enter(child, out var next, out ulong repeatedWords))
                    {
                        words = 0;

                        return false;
                    }
                    frame.Words = unchecked(frame.Words + repeatedWords);
                    if (next is not null)
                        pending.Push(next);
                    continue;
                }
                pending.Pop();
                active.Remove(frame.Payload);
                known.Add(frame.Payload, frame.Words);
                if (pending.TryPeek(out var parent))
                    parent.Words = unchecked(parent.Words + frame.Words);
                else
                    words = frame.Words;
            }

            unchecked
            {
                PhysicalSizeQueries526++;
            }

            return true;
        }
    }
}
