using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

internal sealed class OtpMapHashState64(OtpMapHashIdentityProvider identities)
{
    internal ProcessRuntime? Runtime505 => (identities as OtpMapRuntimeIdentityProvider)?.Runtime505;

    private ulong alpha;
    private ulong beta;
    private ulong ticks;
    private readonly Stack<(Term? Term, bool Cdr, Action<OtpMapHashState64>? Protocol)> pending = new();

    internal bool TryImmediateWord(Term term, out ulong word)
    {
        if (term is Nil)
        {
            word = OtpMapHashNames.NilWord;

            return true;
        }
        if (term is Integer integer && integer.Value >= -(BigInteger.One << (OtpMapHashNames.SmallPayloadBits - 1))
            && integer.Value < BigInteger.One << (OtpMapHashNames.SmallPayloadBits - 1))
        {
            word = unchecked(((ulong)(long)integer.Value << OtpMapHashNames.SmallShift) | OtpMapHashNames.SmallTag);

            return true;
        }

        return identities.TryImmediateWord(term, out word);
    }

    internal void Alpha(ulong value)
    {
        unchecked
        {
            value *= OtpMapHashNames.FirstMultiplier;
            value = BitOperations.RotateLeft(value, 31);
            value *= OtpMapHashNames.SecondMultiplier;
            alpha ^= value;
            alpha = BitOperations.RotateLeft(alpha, 27);
            alpha += beta;
            alpha = alpha * 5 + OtpMapHashNames.FirstAddend;
            ticks++;
        }
    }

    internal void Beta(ulong value)
    {
        unchecked
        {
            value *= OtpMapHashNames.SecondMultiplier;
            value = BitOperations.RotateLeft(value, 33);
            value *= OtpMapHashNames.FirstMultiplier;
            beta ^= value;
            beta = BitOperations.RotateLeft(beta, 31);
            beta += alpha;
            beta = beta * 5 + OtpMapHashNames.SecondAddend;
            ticks++;
        }
    }

    internal void AlphaPair(uint low, uint high) => Alpha(low | ((ulong)high << OtpMapHashNames.HalfWordBits));

    internal void BetaPair(uint low, uint high) => Beta(low | ((ulong)high << OtpMapHashNames.HalfWordBits));

    internal void Defer(Term term) => pending.Push((term, false, null));

    internal void DeferProtocol(Action<OtpMapHashState64> protocol) => pending.Push((null, false, protocol));

    internal void Push(Term term)
    {
        if (TryImmediateWord(term, out ulong word))
        {
            Alpha(OtpMapHashNames.Immediate);
            Beta(word);
        }
        else
        {
            Alpha(OtpMapHashNames.ArrayElement);
            Defer(term);
        }
    }

    internal ulong HashStructured(Term term)
    {
        Defer(term);
        while (pending.TryPop(out var work))
        {
            if (work.Protocol is { } protocol)
            {
                protocol(this);
                continue;
            }
            if (work.Cdr)
            {
                Beta(OtpMapHashNames.Cdr);
                continue;
            }
            Term current = work.Term!;
            if (TryImmediateWord(current, out ulong word))
            {
                Alpha(OtpMapHashNames.Immediate);
                Beta(word);
                continue;
            }
            switch (current)
            {
                case Integer integer:
                    {
                        BigInteger magnitude = BigInteger.Abs(integer.Value);
                        var digits = new List<ulong>();
                        do
                        {
                            digits.Add((ulong)(magnitude & ulong.MaxValue));
                            magnitude >>= OtpMapHashNames.WordBits;
                        }
                        while (!magnitude.IsZero);
                        AlphaPair((uint)(integer.Value.Sign < 0 ? OtpMapHashNames.NegativeBig : OtpMapHashNames.PositiveBig), (uint)digits.Count);
                        int index = 0;
                        for (; index + 1 < digits.Count; index += 2)
                        {
                            Alpha(digits[index]);
                            Beta(digits[index + 1]);
                        }
                        if (index < digits.Count)
                            Beta(digits[index]);
                        break;
                    }
                case FloatTerm floating:
                    {
                        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(floating.Value);
                        Alpha(OtpMapHashNames.Float);
                        Beta(bits);
                        break;
                    }
                case TupleTerm tuple:
                    {
                        Alpha(OtpMapHashNames.Tuple);
                        Beta((ulong)tuple.Items.Count);
                        for (int index = 0; index + 1 < tuple.Items.Count; index++)
                            Push(tuple.Items[index]);
                        if (tuple.Items.Count != 0)
                            Defer(tuple.Items[^1]);
                        break;
                    }
                case Cons:
                    List(current);
                    break;
                case BitString binary:
                    Binary(binary);
                    break;
                default:
                    identities.AppendStructured(current, this);
                    break;
            }
        }
        unchecked
        {
            alpha ^= ticks;
            beta ^= ticks;
            alpha += beta;
            beta += alpha;
            alpha = OtpMapInternalHash64.Mix64(alpha);
            beta = OtpMapInternalHash64.Mix64(beta);
            alpha += beta;
            beta += alpha;

            return alpha ^ beta;
        }
    }

    private void List(Term term)
    {
        uint value = 0;
        int count = 0;
        while (term is Cons { Head: Integer integer } cell && integer.Value >= 0 && integer.Value <= byte.MaxValue)
        {
            value = (value << OtpMapHashNames.ByteBits) | (uint)integer.Value;
            count++;
            if (count == OtpMapHashNames.StringGroupBytes)
            {
                AlphaPair((uint)(OtpMapHashNames.String | (count << OtpMapHashNames.ByteBits)), value);
                value = 0;
                count = 0;
            }
            term = cell.Tail;
        }
        if (count != 0)
            AlphaPair((uint)(OtpMapHashNames.String | (count << OtpMapHashNames.ByteBits)), value);
        if (term is Cons rest)
        {
            if (TryImmediateWord(rest.Head, out ulong word))
            {
                AlphaPair(OtpMapHashNames.Immediate, OtpMapHashNames.Car);
                Beta(word);
                if (rest.Tail is not Cons)
                    Alpha(OtpMapHashNames.Cdr);
                Defer(rest.Tail);
            }
            else
            {
                Defer(rest.Tail);
                if (rest.Tail is not Cons)
                    pending.Push((null, true, null));
                Alpha(OtpMapHashNames.Car);
                Defer(rest.Head);
            }
        }
        else
            Defer(term);
    }

    private void Binary(BitString binary)
    {
        byte[] bytes = binary.ToArray();
        int complete = binary.BitLength / OtpMapHashNames.ByteBits;
        Alpha(OtpMapHashNames.Binary);
        Beta((ulong)binary.BitLength);
        int offset = 0;
        for (; offset + OtpMapHashNames.BinaryBlockBytes <= complete; offset += OtpMapHashNames.BinaryBlockBytes)
        {
            Alpha(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset)));
            Beta(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset + OtpMapHashNames.WordBytes)));
        }
        int remaining = complete - offset;
        if (remaining > OtpMapHashNames.WordBytes)
        {
            ulong high = Word(bytes, offset + OtpMapHashNames.WordBytes, remaining - OtpMapHashNames.WordBytes);
            unchecked
            {
                high *= OtpMapHashNames.SecondMultiplier;
                high = BitOperations.RotateLeft(high, 33);
                beta ^= high * OtpMapHashNames.FirstMultiplier;
            }
        }
        if (remaining != 0)
        {
            ulong low = Word(bytes, offset, Math.Min(remaining, OtpMapHashNames.WordBytes));
            unchecked
            {
                low *= OtpMapHashNames.FirstMultiplier;
                low = BitOperations.RotateLeft(low, 31);
                alpha ^= low * OtpMapHashNames.SecondMultiplier;
            }
        }
        int tail = binary.BitLength % OtpMapHashNames.ByteBits;
        if (tail != 0)
            Alpha((ulong)(bytes[complete] >> (OtpMapHashNames.ByteBits - tail)));
    }

    private static ulong Word(byte[] bytes, int offset, int count)
    {
        ulong value = 0;
        for (int index = 0; index < count; index++)
            value |= (ulong)bytes[offset + index] << (index * OtpMapHashNames.ByteBits);

        return value;
    }
}
