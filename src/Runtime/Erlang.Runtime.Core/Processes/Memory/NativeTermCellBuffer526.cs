using System.Runtime.InteropServices;

namespace Erlang;

// Heap-local physical words. Payload words own real GCHandle roots; raw header
// words contain source-defined numeric bits. Payload tagging is not yet ERTS.
// All ordinary accesses are serialized by the owning ProcessTermHeap506.Gate.
internal sealed unsafe partial class NativeTermCellBuffer526 : IDisposable
{
    private nuint* pointer;
    private readonly byte[] kinds;
    private int rooted;
    private int headers;
    private int numericWords556;
    internal int Length => kinds.Length;
    internal int RootedTerms526 => rooted;
    internal int RawHeaders526 => headers;
    internal int RawNumericWords556 => numericWords556;
    internal bool Disposed526 => pointer == null;

    internal NativeTermCellBuffer526(int words)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(words);
        if (IntPtr.Size != CellStorage526Names.WordBytes)
            throw new NotSupportedException(CellStorage526Names.WordSize);
        kinds = new byte[words];
        pointer = (nuint*)NativeMemory.AllocZeroed((nuint)words, (nuint)IntPtr.Size);
        if (pointer == null)
            throw new OutOfMemoryException();
    }

    ~NativeTermCellBuffer526() => DisposeCore526();

    internal Term? this[int index]
    {
        get
        {
            Require526(index);
            Term? result = kinds[index] == CellStorage526Names.TermKind
                ? (Term?)GCHandle.FromIntPtr((nint)pointer[index]).Target : null;
            GC.KeepAlive(this);

            return result;
        }
    }

    internal void WriteTerms526(Term[] values, int offset)
    {
        for (int index = 0; index < values.Length; index++)
        {
            int target = checked(offset + index);
            Require526(target);
            ArgumentNullException.ThrowIfNull(values[index]);
            GCHandle replacement = GCHandle.Alloc(values[index], GCHandleType.Normal);
            ClearCell526(target);
            pointer[target] = (nuint)GCHandle.ToIntPtr(replacement);
            kinds[target] = CellStorage526Names.TermKind;
            rooted++;
        }
        GC.KeepAlive(this);
    }

    internal void WriteHeaders526(nuint[] values, int offset)
    {
        for (int index = 0; index < values.Length; index++)
        {
            int target = checked(offset + index);
            Require526(target);
            ClearCell526(target);
            pointer[target] = values[index];
            kinds[target] = CellStorage526Names.HeaderKind;
            headers++;
        }
        GC.KeepAlive(this);
    }

    internal nuint ReadHeader526(int index)
    {
        Require526(index);
        if (kinds[index] != CellStorage526Names.HeaderKind)
            throw new InvalidOperationException(CellStorage526Names.NotHeader);
        nuint result = pointer[index];
        GC.KeepAlive(this);

        return result;
    }

    internal void WriteNumeric556(nuint[] values, int offset)
    {
        for (int index = 0; index < values.Length; index++)
        {
            int target = checked(offset + index);
            Require526(target);
            ClearCell526(target);
            pointer[target] = values[index];
            kinds[target] = CellStorage526Names.NumericDataKind556;
            numericWords556++;
        }
        GC.KeepAlive(this);
    }

    internal nuint ReadNumeric556(int index)
    {
        Require526(index);
        if (kinds[index] != CellStorage526Names.NumericDataKind556)
            throw new InvalidOperationException(CellStorage526Names.NotNumeric556);
        nuint result = pointer[index];
        GC.KeepAlive(this);

        return result;
    }

    internal void Clear526(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (offset > Length - length)
            throw new ArgumentOutOfRangeException(nameof(length));
        ObjectDisposedException.ThrowIf(pointer == null, this);
        for (int index = offset; index < offset + length; index++)
            ClearCell526(index);
        GC.KeepAlive(this);
    }

    private void Require526(int index)
    {
        ObjectDisposedException.ThrowIf(pointer == null, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
    }

    private void ClearCell526(int index)
    {
        if (kinds[index] == CellStorage526Names.TermKind)
        {
            GCHandle.FromIntPtr((nint)pointer[index]).Free();
            rooted--;
        }
        else if (kinds[index] == CellStorage526Names.HeaderKind)
            headers--;
        else if (kinds[index] == CellStorage526Names.NumericDataKind556)
            numericWords556--;
        pointer[index] = 0;
        kinds[index] = CellStorage526Names.EmptyKind;
    }

    private void DisposeCore526()
    {
        if (pointer == null)
            return;
        for (int index = 0; index < kinds.Length; index++)
            ClearCell526(index);
        NativeMemory.Free(pointer);
        pointer = null;
    }

    public void Dispose()
    {
        DisposeCore526();
        GC.SuppressFinalize(this);
    }
}
