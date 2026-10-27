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
        lock (Gate)
        {
            words = 0;
            var known = new Dictionary<long, ulong>();
            var active = new HashSet<long>();
            var pending = new Stack<OwnedWordSizeFrame526>();

            bool Enter(Term value, out OwnedWordSizeFrame526? frame, out ulong existingWords)
            {
                frame = null;
                existingWords = 0;
                if (value is Nil or Atom || value is TupleTerm { HasGlobalEmptyStorage428: true }
                    || value is Integer integer && integer.OwnedStorage559 is null
                        && integer.Value >= -SmallBound526
                        && integer.Value < SmallBound526)
                    return true;
                // Reject unrepresented scalar/function/binary/foreign domains;
                // their existing source policy remains the explicit fallback.
                if (value is not (TupleTerm or Cons or MapTerm or HamtBitmapTerm429 or FloatTerm or Integer))
                    return false;
                if (!flat && runtime.IsSharingLiteral425(value))
                    return true;
                if (!IsOwnedLocal(value) || Storage(value) is not ProcessTermSequence506 payload)
                    return false;
                bool sharedHamt = false;
                if (value is FloatTerm)
                {
                    _ = ReadFloat556(payload);
                }
                else if (value is Integer)
                {
                    _ = ReadInteger559(payload);
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
                    OwnedMapHeader517 header = ReadMapHeader517(payload);
                    sharedHamt = !flat && header.Kind is OwnedMapHeaderKind517.HamtHead or OwnedMapHeaderKind517.HamtBitmap;
                }
                if (known.TryGetValue(payload.Identity, out ulong prior))
                {
                    existingWords = flat ? prior : 0;

                    return true;
                }
                if (!active.Add(payload.Identity))
                    return false;
                frame = new(payload);
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
                if (frame.Next < frame.Payload.Count)
                {
                    Term child = frame.Payload[frame.Next++];
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
                active.Remove(frame.Payload.Identity);
                known.Add(frame.Payload.Identity, frame.Words);
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
