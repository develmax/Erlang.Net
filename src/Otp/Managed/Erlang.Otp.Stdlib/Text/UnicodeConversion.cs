// Iterative port of OTP unicode:ml_map/3, i_trans/1 and o_trans/1 (Apache-2.0).
namespace Erlang;

internal sealed class UnicodeConversion(ProcessContext context, UnicodeEncoding input, UnicodeEncoding output)
{
    private readonly List<byte> bytes = new();
    private int capacity;
    private BitString? incomplete;
    private int missing;
    private int work;

    internal async ValueTask<Term> Run(Term data)
    {
        var parents = new Stack<Term>();
        Term cursor = data;
        Term? error = null;
        while (true)
        {
            if (++work % UnicodeNames.WorkSlice == 0)
            {
                await context.BumpReductionsAsync(UnicodeNames.WorkSlice);
            }
            if (cursor is Nil)
            {
                if (parents.TryPop(out Term? parent))
                {
                    cursor = parent;
                    continue;
                }
                break;
            }
            if (cursor is Cons cell)
            {
                if (cell.Head is Integer integer)
                {
                    if (incomplete is null && output.Width == 1 && integer.Value < 0)
                    {
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    if (incomplete is not null)
                    {
                        error = new Cons(incomplete, cursor);
                    }
                    else if (integer.Value < 0 || integer.Value > (input.Width == 1 || output.Width == 1 ? byte.MaxValue : UnicodeNames.Maximum)
                        || integer.Value >= UnicodeNames.SurrogateStart && integer.Value <= UnicodeNames.SurrogateEnd)
                    {
                        error = new Cons(Term.List(integer), cell.Tail);
                    }
                    else
                    {
                        output.Append(bytes, (int)integer.Value);
                        Commit();
                    }
                    if (error is not null)
                    {
                        break;
                    }
                    cursor = cell.Tail;
                    continue;
                }
                if (cell.Head is Nil or Cons)
                {
                    parents.Push(cell.Tail);
                    cursor = cell.Head;
                    continue;
                }
                if (cell.Head is not BitString { IsBinary: true } part)
                {
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
                if (incomplete is not null)
                {
                    int take = Math.Min(missing, part.ByteSpan.Length);
                    byte[] joined = new byte[incomplete.ByteSpan.Length + take];
                    incomplete.ByteSpan.CopyTo(joined);
                    part.ByteSpan[..take].CopyTo(joined.AsSpan(incomplete.ByteSpan.Length));
                    if (take < missing)
                    {
                        incomplete = new BitString(joined);
                        missing -= take;
                        cursor = cell.Tail;
                        continue;
                    }
                    incomplete = null;
                    cursor = new Cons(new BitString(joined), new Cons(Slice(part, take), cell.Tail));
                    continue;
                }
                if (part.ByteSpan.Length > UnicodeNames.ChunkBytes)
                {
                    cursor = new Cons(
                        part.SliceBits(0, UnicodeNames.ChunkBytes * UnicodeNames.BitsPerByte),
                        new Cons(Slice(part, UnicodeNames.ChunkBytes), cell.Tail)
                    );
                    continue;
                }
                error = Decode(part);
                if (error is not null)
                {
                    error = new Cons(error, cell.Tail);
                    break;
                }
                cursor = cell.Tail;
                continue;
            }
            if (cursor is not BitString { IsBinary: true } binary)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            if (incomplete is not null)
            {
                if (binary.ByteSpan.Length >= missing)
                {
                    BitString prefix = incomplete;
                    incomplete = null;
                    cursor = Term.List(prefix, binary);
                    continue;
                }
                byte[] joined = new byte[incomplete.ByteSpan.Length + binary.ByteSpan.Length];
                incomplete.ByteSpan.CopyTo(joined);
                binary.ByteSpan.CopyTo(joined.AsSpan(incomplete.ByteSpan.Length));
                missing -= binary.ByteSpan.Length;
                incomplete = new BitString(joined);
                cursor = Nil.Value;
                continue;
            }
            if (binary.ByteSpan.Length > UnicodeNames.ChunkBytes)
            {
                cursor = Term.List(binary.SliceBits(0, UnicodeNames.ChunkBytes * UnicodeNames.BitsPerByte), Slice(binary, UnicodeNames.ChunkBytes));
                continue;
            }
            error = Decode(binary);
            if (error is not null)
            {
                break;
            }
            cursor = Nil.Value;
        }
        Term converted = Build();
        if (error is not null)
        {
            while (parents.TryPop(out Term? parent))
            {
                error = new Cons(error, parent);
            }

            return Term.Tuple(Term.A(CommonAtomNames.Error), converted, error);
        }

        return incomplete is null ? converted : Term.Tuple(Term.A(UnicodeNames.Incomplete), converted, incomplete);
    }

    private Term? Decode(BitString binary)
    {
        Term? rest = DecodeCore(binary);
        Commit();

        return rest;
    }

    private void Commit()
    {
        // Each OTP append determines capacity at a semantic segment boundary.
        // Track those boundaries without copying the accumulated payload.
        if (bytes.Count <= capacity)
        {
            return;
        }
        double desired = bytes.Count <= BinaryBackingParameters.LargeGrowthThreshold
            ? bytes.Count * 2d : bytes.Count * BinaryBackingParameters.LargeGrowthFactor;
        if (desired > Array.MaxLength)
        {
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        }
        capacity = Math.Max(BinaryBackingParameters.MinimumAppendBytes, (int)desired);
    }

    private BitString Build()
    {
        if (bytes.Count == 0)
        {
            return new BitString(Array.Empty<byte>());
        }
        // Materialize once, retaining a genuine writable result and its exact
        // OTP capacity for both inspection and subsequent guest appends.
        BitString writable = BitString.InitializeWritable(capacity);

        return BitString.FromValidatedAppend(writable, new BitString(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(bytes)));
    }

    private Term? DecodeCore(BitString binary)
    {
        int position = 0;
        while (position < binary.ByteSpan.Length)
        {
            if (!input.Decode(binary.ByteSpan[position..], out int scalar, out int used))
            {
                BitString rest = Slice(binary, position);
                missing = input.Missing(rest.ByteSpan);
                if (missing == 0)
                {
                    return rest;
                }
                incomplete = rest;

                return null;
            }
            if (output.Width == 1 && scalar > byte.MaxValue)
            {
                return Slice(binary, position);
            }
            output.Append(bytes, scalar);
            position += used;
        }

        return null;
    }

    private static BitString Slice(BitString value, int offset)
        => value.SliceBits(offset * UnicodeNames.BitsPerByte, value.BitLength - offset * UnicodeNames.BitsPerByte);
}
