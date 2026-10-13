using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Erlang;

internal static class ErlangNativeHash
{
    public static uint Atom(string name)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(name);
        uint hash = 0;
        unchecked
        {
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                if (index + 1 < bytes.Length && (value & ErlangHashParameters.Latin1LeadMask) == ErlangHashParameters.Latin1Lead && (bytes[index + 1] & ErlangHashParameters.ContinuationMask) == ErlangHashParameters.Continuation)
                    value = (byte)((value << ErlangHashParameters.Latin1ContinuationBits) | (bytes[++index] & ErlangHashParameters.ContinuationPayloadMask));
                hash = (hash << ErlangHashParameters.AtomNibbleBits) + value;
                uint high = hash & ErlangHashParameters.AtomHighMask;
                if (high != 0)
                {
                    hash ^= high >> ErlangHashParameters.AtomFoldBits;
                    hash ^= high;
                }
            }
        }

        return hash;
    }

    public static uint Portable2(Term term)
    {
        uint hash = 0, pairs = 0;
        var stack = new Stack<Frame>();
        stack.Push(new Frame(FrameKind.Term, term));
        unchecked
        {
            while (stack.TryPop(out Frame frame))
            {
                if (frame.Kind == FrameKind.MapPair)
                {
                    pairs ^= hash;
                    hash = 0;
                    continue;
                }
                if (frame.Kind == FrameKind.MapTail)
                {
                    hash = frame.Hash;
                    Mix(
                        ref hash,
                        pairs,
                        0,
                        ErlangHashParameters.MapPairsMix
                    );
                    pairs = frame.Pairs;
                    continue;
                }
                switch (frame.Term)
                {
                    case Nil:
                        if (hash == 0) hash = ErlangHashParameters.EmptyListHash;
                        else Mix(
                            ref hash,
                            ErlangHashParameters.NilTag,
                            0,
                            ErlangHashParameters.NilMix
                        );
                        break;
                    case Atom atom:
                        if (hash == 0) hash = Atom(atom.Name);
                        else Mix(
                            ref hash,
                            Atom(atom.Name),
                            0,
                            ErlangHashParameters.AtomMix
                        );
                        break;
                    case Integer integer:
                        {
                            BigInteger value = integer.Value;
                            if (value >= -(BigInteger.One << (ErlangHashParameters.IntegerSmallBits - 1)) && value < BigInteger.One << (ErlangHashParameters.IntegerSmallBits - 1))
                            {
                                int small = (int)value;
                                if (small < 0) Mix(
                                    ref hash,
                                    (uint)-small,
                                    0,
                                    ErlangHashParameters.BaseMix
                                );
                                Mix(
                                    ref hash,
                                    (uint)small,
                                    0,
                                    ErlangHashParameters.BaseMix
                                );
                            }
                            else
                            {
                                uint mix = value.Sign < 0 ? ErlangHashParameters.NegativeIntegerMix : ErlangHashParameters.PositiveIntegerMix;
                                byte[] bytes = BigInteger.Abs(value).ToByteArray(true, false);
                                for (int offset = 0; offset < bytes.Length; offset += ErlangHashParameters.IntegerDigitBytes)
                                    Mix(
                                        ref hash,
                                        Word(bytes, offset),
                                        Word(bytes, offset + ErlangHashParameters.IntegerWordBytes),
                                        mix
                                    );
                            }
                            break;
                        }
                    case FloatTerm floating:
                        {
                            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(floating.Value == 0 ? 0 : floating.Value);
                            Mix(
                                ref hash,
                                (uint)(bits >> ErlangHashParameters.IntegerWordBits),
                                (uint)bits,
                                ErlangHashParameters.FloatMix
                            );
                            break;
                        }
                    case NativeRecordTerm record:
                        Mix(
                            ref hash,
                            (uint)record.FieldNames.Count,
                            NativeRecordHash.Definition(record),
                            NativeRecordHash.InstanceMix
                        );
                        Push(stack, NativeRecordHash.Values(record));
                        break;
                    case TupleTerm tuple:
                        Mix(
                            ref hash,
                            (uint)tuple.Items.Count,
                            0,
                            ErlangHashParameters.TupleMix
                        );
                        Push(stack, tuple.Items);
                        break;
                    case MapTerm map:
                        Mix(
                            ref hash,
                            (uint)map.Entries.Count,
                            0,
                            ErlangHashParameters.MapSizeMix
                        );
                        if (map.Entries.Count != 0)
                        {
                            stack.Push(new Frame(
                                FrameKind.MapTail,
                                null,
                                hash,
                                pairs
                            ));
                            hash = 0;
                            pairs = 0;
                            for (int index = map.Entries.Count - 1; index >= 0; index--)
                            {
                                stack.Push(new Frame(FrameKind.MapPair, null));
                                stack.Push(new Frame(FrameKind.Term, map.Entries[index].Value));
                                stack.Push(new Frame(FrameKind.Term, map.Entries[index].Key));
                            }
                        }
                        break;
                    case Cons:
                        {
                            Term cursor = frame.Term;
                            uint packed = 0;
                            int count = 0;
                            while (cursor is Cons { Head: Integer value } cons && value.Value >= 0 && value.Value <= byte.MaxValue)
                            {
                                packed = (packed << ErlangHashParameters.ByteBits) + (uint)value.Value;
                                count++;
                                if (count == ErlangHashParameters.StringGroupBytes)
                                {
                                    Mix(
                                        ref hash,
                                        packed,
                                        0,
                                        ErlangHashParameters.StringMix
                                    );
                                    count = 0;
                                    packed = 0;
                                }
                                cursor = cons.Tail;
                            }
                            if (count != 0) Mix(
                                ref hash,
                                packed,
                                0,
                                ErlangHashParameters.StringMix
                            );
                            if (cursor is Cons remaining)
                            {
                                stack.Push(new Frame(FrameKind.Term, remaining.Tail));
                                stack.Push(new Frame(FrameKind.Term, remaining.Head));
                            }
                            else stack.Push(new Frame(FrameKind.Term, cursor));
                            break;
                        }
                    case BitString binary:
                        {
                            ReadOnlySpan<byte> bytes = binary.ByteSpan;
                            int full = binary.BitLength / ErlangHashParameters.ByteBits;
                            int tail = binary.BitLength % ErlangHashParameters.ByteBits;
                            uint seed = ErlangHashParameters.BinaryMix + hash;
                            hash = full == 0 && tail == 0 ? seed : ErlangBlockHash.Hash(bytes[..full], seed);
                            if (tail != 0) Mix(
                                ref hash,
                                (uint)tail,
                                (uint)(bytes[full] >> (ErlangHashParameters.ByteBits - tail)),
                                ErlangHashParameters.BitTailMix
                            );
                            break;
                        }
                    case Pid pid:
                        Mix(
                            ref hash,
                            (uint)pid.Id,
                            0,
                            ErlangHashParameters.PidMix
                        );
                        break;
                    case PortTerm port:
                        Mix(
                            ref hash,
                            (uint)port.Id,
                            (uint)(port.Id >> ErlangHashParameters.IntegerWordBits),
                            ErlangHashParameters.PortMix
                        );
                        break;
                    case ReferenceTerm reference:
                        Mix(
                            ref hash,
                            reference.Words[0],
                            0,
                            ErlangHashParameters.ReferenceMix
                        );
                        break;
                    case FunctionTerm { ExternalIdentity: not null } function:
                        Mix(
                            ref hash,
                            (uint)function.ExternalIdentity.Arity,
                            Atom(function.ExternalIdentity.Module.Name),
                            ErlangHashParameters.BaseMix
                        );
                        Mix(
                            ref hash,
                            Atom(function.ExternalIdentity.Function.Name),
                            0,
                            ErlangHashParameters.ExternalFunctionMix
                        );
                        break;
                    case FunctionTerm { LocalIdentity: not null } function:
                        Mix(
                            ref hash,
                            (uint)function.Environment.Count,
                            Atom(function.LocalIdentity.Module.Name),
                            ErlangHashParameters.BaseMix
                        );
                        Mix(
                            ref hash,
                            (uint)function.LocalIdentity.Index,
                            (uint)function.LocalIdentity.OldUnique,
                            ErlangHashParameters.BaseMix
                        );
                        Push(stack, function.Environment);
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }

        return hash;
    }

    private static uint Word(ReadOnlySpan<byte> bytes, int offset)
    {
        uint value = 0;
        for (int index = 0; index < ErlangHashParameters.IntegerWordBytes && offset + index < bytes.Length; index++)
            value |= (uint)bytes[offset + index] << (index * ErlangHashParameters.ByteBits);

        return value;
    }

    private static void Mix(
        ref uint hash,
        uint first,
        uint second,
        uint seed
    )
    {
        uint a = unchecked(seed + first), b = unchecked(seed + second);
        ErlangBlockHash.Mix(ref a, ref b, ref hash);
    }

    private static void Push(Stack<Frame> stack, IReadOnlyList<Term> terms)
    {
        for (int index = terms.Count - 1; index >= 0; index--)
            stack.Push(new Frame(FrameKind.Term, terms[index]));
    }

    private enum FrameKind
    {
        Term,
        MapPair,
        MapTail
    }

    private readonly record struct Frame(
        FrameKind Kind,
        Term? Term,
        uint Hash = 0,
        uint Pairs = 0
    );
}
