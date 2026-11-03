namespace Erlang;

internal sealed unsafe partial class NativeTermCellBuffer526
{
    // Called only through a validated owning heap gate or an authenticated
    // immutable module reader that keeps the exact actual native block live.
    // A raw header view must never expose GCHandle-encoded Term cells.
    internal nint RawAddress571(int offset, int words)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(words);
        Require526(offset);
        if (words > Length - offset)
            throw new ArgumentOutOfRangeException(nameof(words));
        for (int index = offset; index < offset + words; index++)
            if (kinds[index] is not (CellStorage526Names.HeaderKind or CellStorage526Names.NumericDataKind556))
                throw new InvalidOperationException(NativeRawAddress571Names.RawCellsRequired);
        nint result = (nint)(pointer + offset);
        GC.KeepAlive(this);

        return result;
    }
}
