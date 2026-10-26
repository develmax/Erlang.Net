using System.Buffers;
using System.Text;

namespace Erlang;

internal sealed class PrimFileEncodingWork410(ProcessContext context)
{
    private int work;

    internal async ValueTask Tick()
    {
        context.Cancellation.ThrowIfCancellationRequested();
        if (++work == PrimFileEncodingNames410.WorkSlice)
        {
            work = 0;
            await context.BumpReductionsAsync(PrimFileEncodingNames410.WorkSlice);
        }
    }

    internal async ValueTask<Term> Run(string function, Term input)
    {
        if (function == PrimFileEncodingNames410.ToNative)
            return await ToNative(input);
        byte[] bytes = input is BitString { IsBinary: true } binary
            ? binary.ToArray() : throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (function == PrimFileEncodingNames410.FromNative && OperatingSystem.IsWindows())
        {
            var values = new List<int>();
            int index;
            for (index = 0; index + 1 < bytes.Length; index += PrimFileEncodingNames410.WordBytes)
            {
                await Tick();
                values.Add(bytes[index] | bytes[index + 1] << PrimFileEncodingNames410.ByteBits);
            }
            if (index < bytes.Length) values.Add(bytes[index]);

            return await ToList(values);
        }
        List<int>? points = await Decode(bytes);
        if (function == PrimFileEncodingNames410.Translatable)
            return Term.A(points is null ? ErlangBooleanAtoms.False : ErlangBooleanAtoms.True);
        if (points is null)
        {
            if (function == PrimFileEncodingNames410.Normalize)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(PrimFileEncodingNames410.Warning));
        }
        if (function == PrimFileEncodingNames410.Normalize || OperatingSystem.IsMacOS())
            return await PrimFileComposition410.Normalize(this, points);

        return await ToList(points);
    }

    private async ValueTask<Term> ToList(IReadOnlyList<int> points)
    {
        Term result = Nil.Value;
        for (int index = points.Count - 1; index >= 0; index--)
        {
            await Tick();
            result = new Cons(Term.I(points[index]), result);
        }

        return result;
    }

    private static bool Read(
        byte[] bytes,
        int offset,
        out int point,
        out int consumed
    )
    {
        OperationStatus status = Rune.DecodeFromUtf8(bytes.AsSpan(offset), out Rune scalar, out consumed);
        point = scalar.Value;

        return status == OperationStatus.Done;
    }

    private async ValueTask<List<int>?> Decode(byte[] bytes)
    {
        var points = new List<int>();
        for (int offset = 0; offset < bytes.Length;)
        {
            await Tick();
            if (!Read(
                bytes,
                offset,
                out int point,
                out int consumed
            ))
                return null;
            points.Add(point);
            offset += consumed;
        }

        return points;
    }

    private static void Word(List<byte> result, int point)
    {
        result.Add(unchecked((byte)point));
        result.Add(unchecked((byte)(point >> PrimFileEncodingNames410.ByteBits)));
    }

    private async ValueTask<Term> ToNative(Term input)
    {
        var result = new List<byte>();
        bool windows = OperatingSystem.IsWindows();
        if (input is BitString { IsBinary: true } binary)
        {
            byte[] bytes = binary.ToArray();
            List<int>? decoded = windows ? await Decode(bytes) : null;
            if (windows && decoded is not null)
            {
                foreach (int point in decoded)
                {
                    await Tick();
                    // Exact pinned filename routine truncates to one raw word.
                    Word(result, point);
                }
            }
            else
            {
                foreach (byte value in bytes)
                {
                    await Tick();
                    if (value == 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    if (windows) Word(result, value);
                    else result.Add(value);
                }
            }
        }
        else
        {
            var pending = new Stack<(Term Term, bool Scalar)>();
            pending.Push((input, false));
            while (pending.TryPop(out var item))
            {
                await Tick();
                switch (item.Term)
                {
                    case Nil:
                        break;
                    case Cons cell:
                        pending.Push((cell.Tail, false));
                        pending.Push((cell.Head, true));
                        break;
                    case Integer integer when item.Scalar && integer.Value > 0
                        && integer.Value <= (windows ? PrimFileEncodingNames410.WordMaximum : UnicodeNativeNames.MaximumScalar):
                        int point = (int)integer.Value;
                        if (windows) Word(result, point);
                        else
                        {
                            if (!Rune.IsValid(point)) throw new ErlangException(ErlangErrorReasons.BadArgument);
                            AppendUtf8(result, point);
                        }
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }
        result.Add(0);
        if (windows) result.Add(0);

        return new BitString(result.ToArray());
    }

    private static void AppendUtf8(List<byte> result, int point)
    {
        Span<byte> bytes = stackalloc byte[UnicodeNativeNames.MaximumUtf8Bytes];
        int count = new Rune(point).EncodeToUtf8(bytes);
        for (int index = 0; index < count; index++) result.Add(bytes[index]);
    }
}
