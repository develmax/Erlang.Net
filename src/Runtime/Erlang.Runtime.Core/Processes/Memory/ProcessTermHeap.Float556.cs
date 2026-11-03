namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // This guard is exact literal-instance provenance, never double equality.
    // For a FloatTerm the existing CWT lookup performs no registry callback.
    internal bool IsLiteralFloat556(Term value)
        => value is FloatTerm && runtime.IsSharingLiteral425(value);

    internal FloatTerm CreateFloat556(double value, ProcessTermConstruction514 construction)
    {
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);
        ProcessTermSequence506 payload = AllocateEncoded526(
            [],
            [BoxedFloat556Names.Header],
            [unchecked((nuint)BitConverter.DoubleToInt64Bits(value))]
        );
        try
        {
            var result = new FloatTerm(payload);
            construction.Publish(result);

            return result;
        }
        catch
        {
            ReleaseRegisteredHeader526(payload);
            throw;
        }
    }

    internal double ReadFloat556(ProcessTermSequence506 payload)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(payload.Owner, this)
                || !blocks.TryGetValue(payload.Identity, out var active)
                || !ReferenceEquals(active, payload))
                throw new ObjectDisposedException(nameof(ProcessTermSequence506));
            if (payload.Count != 0 || payload.HeaderWords526 != BoxedFloat556Names.HeaderWords
                || payload.RawPayloadWords556 != BoxedFloat556Names.PayloadWords
                || payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526) != BoxedFloat556Names.Header)
                throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
            nuint bits = payload.Slab.Cells.ReadNumeric556(
                checked(payload.AllocationOffset526 + BoxedFloat556Names.HeaderWords)
            );

            return BitConverter.Int64BitsToDouble(unchecked((long)bits));
        }
    }
}
