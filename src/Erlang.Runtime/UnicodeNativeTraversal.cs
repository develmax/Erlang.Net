using System.Text;

namespace Erlang;

internal sealed class UnicodeNativeTraversal(ProcessContext context, bool latin1, bool list)
{
    private readonly List<Term> characters = new();
    private readonly List<byte> bytes = new();
    private readonly List<byte> pending = new(UnicodeNativeNames.MaximumUtf8Bytes);
    private int work;

    internal async ValueTask<Term> Run(Term subject)
    {
        await ValidateShape(subject);
        var parents = new Stack<Term>();
        Term cursor = subject;
        bool error = false;
        while (true)
        {
            await Tick();
            if (cursor is Nil)
            {
                if (parents.TryPop(out Term? rest))
                {
                    cursor = rest;
                    continue;
                }
                break;
            }
            if (cursor is Cons cell)
            {
                if (cell.Head is Nil)
                {
                    cursor = cell.Tail;
                    continue;
                }
                if (cell.Head is Cons)
                {
                    parents.Push(cell.Tail);
                    cursor = cell.Head;
                    continue;
                }
                if (cell.Head is Integer integer)
                {
                    if (pending.Count != 0 || integer.Value < 0 || integer.Value > (latin1 ? byte.MaxValue : UnicodeNativeNames.MaximumScalar)
                        || integer.Value >= UnicodeNativeNames.FirstSurrogate && integer.Value <= UnicodeNativeNames.LastSurrogate)
                    {
                        error = true;
                        break;
                    }
                    Append((int)integer.Value);
                    cursor = cell.Tail;
                    continue;
                }
                if (cell.Head is BitString binary)
                {
                    Term? rest = await Binary(binary);
                    if (rest is not null)
                    {
                        cursor = new Cons(rest, cell.Tail);
                        error = true;
                        break;
                    }
                    cursor = cell.Tail;
                    continue;
                }
                error = true;
                break;
            }
            if (cursor is BitString tail)
            {
                Term? rest = await Binary(tail);
                if (rest is not null)
                {
                    cursor = rest;
                    error = true;
                    break;
                }
                cursor = Nil.Value;
                continue;
            }
            error = true;
            break;
        }
        Term result;
        if (list)
        {
            result = Nil.Value;
            for (int index = characters.Count - 1; index >= 0; index--)
            {
                await Tick();
                result = new Cons(characters[index], result);
            }
        }
        else
            result = new BitString(bytes.ToArray());
        if (error)
        {
            while (parents.TryPop(out Term? parent))
            {
                await Tick();
                cursor = new Cons(cursor, parent);
            }
            if (pending.Count != 0)
                cursor = Term.List(new BitString(pending.ToArray()), cursor);

            return Term.Tuple(Term.A(CommonAtomNames.Error), result, cursor);
        }
        if (pending.Count != 0)
            return Term.Tuple(Term.A(PlausiblePending() ? UnicodeNativeNames.Incomplete : CommonAtomNames.Error),
                result, new BitString(pending.ToArray()));

        return result;
    }

    private async ValueTask ValidateShape(Term subject)
    {
        var stack = new Stack<(Term Value, bool Scalar)>();
        stack.Push((subject, false));
        while (stack.TryPop(out var item))
        {
            await Tick();
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons cell:
                    stack.Push((cell.Tail, false));
                    stack.Push((cell.Head, true));
                    break;
                case BitString { IsBinary: true }:
                    break;
                case Integer integer when item.Scalar:
                    // Pinned utf8_need deliberately stops at a bignum: later
                    // malformed structure must not turn its error tuple into badarg.
                    if (integer.Value < UnicodeNativeNames.MinimumSmall || integer.Value > UnicodeNativeNames.MaximumSmall)
                        return;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    private async ValueTask<Term?> Binary(BitString binary)
    {
        if (!binary.IsBinary)
            return binary;
        int length = binary.BitLength / UnicodeNativeNames.BitsPerByte;
        int position = 0;
        if (latin1)
        {
            while (position < length)
            {
                await Tick();
                Append(binary.ByteSpan[position++]);
            }

            return null;
        }
        if (pending.Count != 0)
        {
            int count = Width(pending[0]) - pending.Count;
            if (length < count)
            {
                for (int index = 0; index < length; index++)
                    pending.Add(binary.ByteSpan[index]);

                return null;
            }
            byte[] joined = new byte[pending.Count + count];
            pending.CopyTo(joined);
            for (int index = 0; index < count; index++)
                joined[pending.Count + index] = binary.ByteSpan[index];
            int value = Decode(joined);
            if (value < 0)
                return binary;
            Append(value);
            pending.Clear();
            position = count;
        }
        while (position < length)
        {
            await Tick();
            int width = Width(binary.ByteSpan[position]);
            if (width == 0)
                return Rest(binary, position, length);
            if (length - position < width)
            {
                for (; position < length; position++)
                    pending.Add(binary.ByteSpan[position]);

                return null;
            }
            int value = Decode(binary.ByteSpan.Slice(position, width));
            if (value < 0)
                return Rest(binary, position, length);
            Append(value);
            position += width;
        }

        return null;
    }

    private static Term Rest(BitString binary, int position, int length)
        => binary.SliceBits(position * UnicodeNativeNames.BitsPerByte, (length - position) * UnicodeNativeNames.BitsPerByte);

    private static int Width(byte first) => first switch
    {
        < UnicodeNativeNames.AsciiLimit => 1,
        >= UnicodeNativeNames.TwoByteLead and < UnicodeNativeNames.TwoByteLeadLimit => 2,
        >= UnicodeNativeNames.ThreeByteLead and < UnicodeNativeNames.ThreeByteLeadLimit => 3,
        >= UnicodeNativeNames.FourByteLead and < UnicodeNativeNames.FourByteLeadLimit => 4,
        _ => 0
    };

    private static int Decode(ReadOnlySpan<byte> source)
    {
        int value = source.Length == 1 ? source[0] : source[0] & (UnicodeNativeNames.LeadPayloadMask >> source.Length);
        for (int index = 1; index < source.Length; index++)
        {
            if ((source[index] & UnicodeNativeNames.ContinuationMask) != UnicodeNativeNames.ContinuationTag)
                return -1;
            value = (value << UnicodeNativeNames.ContinuationPayloadBits) | (source[index] & UnicodeNativeNames.ContinuationPayloadMask);
        }
        int minimum = source.Length switch
        {
            1 => 0,
            2 => UnicodeNativeNames.MinimumTwoByteScalar,
            3 => UnicodeNativeNames.MinimumThreeByteScalar,
            _ => UnicodeNativeNames.MinimumFourByteScalar
        };

        return value < minimum || value > UnicodeNativeNames.MaximumScalar
            || value >= UnicodeNativeNames.FirstSurrogate && value <= UnicodeNativeNames.LastSurrogate ? -1 : value;
    }

    private bool PlausiblePending()
    {
        for (int index = 1; index < pending.Count; index++)
            if ((pending[index] & UnicodeNativeNames.ContinuationMask) != UnicodeNativeNames.ContinuationTag)
                return false;

        return Width(pending[0]) > pending.Count;
    }

    private void Append(int value)
    {
        if (list)
            characters.Add(Term.I(value));
        else
        {
            Span<byte> encoded = stackalloc byte[UnicodeNativeNames.MaximumUtf8Bytes];
            int count = new Rune(value).EncodeToUtf8(encoded);
            for (int index = 0; index < count; index++)
                bytes.Add(encoded[index]);
        }
    }

    private async ValueTask Tick()
    {
        if (++work == UnicodeNativeNames.WorkSlice)
        {
            work = 0;
            await context.BumpReductionsAsync(UnicodeNativeNames.WorkSlice);
        }
    }
}
