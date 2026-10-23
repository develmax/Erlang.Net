using System.Numerics;

namespace Erlang;

internal static class ErlangLegacyHash
{
    public static uint Portable(Term term)
    {
        uint hash = 0;
        var stack = new Stack<Frame>();
        stack.Push(new Frame(FrameKind.Term, term));
        unchecked
        {
            while (stack.TryPop(out Frame frame))
            {
                if (frame.Kind == FrameKind.RecordEnd)
                {
                    hash = hash * NativeRecordHash.LegacyInstancePrime + frame.Count;
                    continue;
                }
                if (frame.Kind == FrameKind.TupleEnd)
                {
                    hash = hash * ErlangLegacyHashPrimes.Prime9 + frame.Count;
                    continue;
                }
                if (frame.Kind == FrameKind.TailEnd)
                {
                    hash *= ErlangLegacyHashPrimes.Prime8;
                    continue;
                }
                if (frame.Kind == FrameKind.Tail && frame.Term is not Cons)
                    stack.Push(new Frame(FrameKind.TailEnd, null));
                switch (frame.Term)
                {
                    case Nil:
                        hash = hash * ErlangLegacyHashPrimes.Prime3 + 1;
                        break;
                    case Atom atom:
                        hash = hash * ErlangLegacyHashPrimes.Prime1 + ErlangNativeHash.Atom(atom.Name);
                        break;
                    case Integer integer:
                        {
                            byte[] bytes = BigInteger.Abs(integer.Value).ToByteArray(true, false);
                            int length = ((bytes.Length + ErlangHashParameters.IntegerWordBytes - 1) / ErlangHashParameters.IntegerWordBytes) * ErlangHashParameters.IntegerWordBytes;
                            for (int index = 0; index < length; index++) hash = hash * ErlangLegacyHashPrimes.Prime2 + (index < bytes.Length ? bytes[index] : 0U);
                            hash *= integer.Value.Sign < 0 ? ErlangLegacyHashPrimes.Prime4 : ErlangLegacyHashPrimes.Prime3;
                            break;
                        }
                    case FloatTerm floating:
                        {
                            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(floating.Value == 0 ? 0 : floating.Value);
                            hash = hash * ErlangLegacyHashPrimes.Prime6 + ((uint)bits ^ (uint)(bits >> ErlangHashParameters.IntegerWordBits));
                            break;
                        }
                    case BitString binary:
                        {
                            ReadOnlySpan<byte> bytes = binary.ByteSpan;
                            int full = binary.BitLength / ErlangHashParameters.ByteBits;
                            int tail = binary.BitLength % ErlangHashParameters.ByteBits;
                            for (int index = 0; index < full; index++) hash = hash * ErlangLegacyHashPrimes.Prime1 + bytes[index];
                            if (tail != 0) hash = (hash * ErlangLegacyHashPrimes.Prime1 + (uint)(bytes[full] >> (ErlangHashParameters.ByteBits - tail))) * ErlangLegacyHashPrimes.Prime12 + (uint)tail;
                            hash = hash * ErlangLegacyHashPrimes.Prime4 + (uint)full;
                            break;
                        }
                    case Pid pid:
                        Word(ref hash, (uint)pid.Id, ErlangLegacyHashPrimes.Prime5);
                        hash *= ErlangLegacyHashPrimes.Prime6;
                        break;
                    case PortTerm port:
                        uint high = (uint)(port.Id >> ErlangHashParameters.IntegerWordBits);
                        if (high != 0) Word(ref hash, high, ErlangLegacyHashPrimes.Prime11);
                        Word(ref hash, (uint)port.Id, ErlangLegacyHashPrimes.Prime9);
                        hash *= ErlangLegacyHashPrimes.Prime10;
                        break;
                    case ReferenceTerm reference:
                        Word(ref hash, reference.Words[0], ErlangLegacyHashPrimes.Prime9);
                        hash *= ErlangLegacyHashPrimes.Prime10;
                        break;
                    case NativeRecordTerm record:
                        stack.Push(new Frame(FrameKind.RecordEnd, null, (uint)record.FieldNames.Count + 1));
                        Push(stack, new Term[] { NativeRecordHash.LegacyDefinition(record) }.Concat(NativeRecordHash.Values(record)).ToArray());
                        break;
                    case TupleTerm tuple:
                        stack.Push(new Frame(FrameKind.TupleEnd, null, (uint)tuple.Items.Count));
                        Push(stack, tuple.Items);
                        break;
                    case MapTerm map:
                        hash = hash * ErlangLegacyHashPrimes.Prime13 + ErlangLegacyHashPrimes.Prime14 + ErlangNativeHash.Portable2(map);
                        break;
                    case Cons:
                        {
                            Term cursor = frame.Term;
                            while (cursor is Cons { Head: Integer integer } cons && integer.Value >= 0 && integer.Value <= byte.MaxValue)
                            {
                                hash = hash * ErlangLegacyHashPrimes.Prime2 + (uint)integer.Value;
                                cursor = cons.Tail;
                            }
                            if (cursor is Cons remaining)
                            {
                                stack.Push(new Frame(FrameKind.Tail, remaining.Tail));
                                stack.Push(new Frame(FrameKind.Term, remaining.Head));
                            }
                            else
                            {
                                stack.Push(new Frame(FrameKind.TailEnd, null));
                                stack.Push(new Frame(FrameKind.Term, cursor));
                            }
                            break;
                        }
                    case FunctionTerm { ExternalIdentity: not null } function:
                        hash = hash * ErlangLegacyHashPrimes.Prime11 + (uint)function.ExternalIdentity.Arity;
                        hash = hash * ErlangLegacyHashPrimes.Prime1 + ErlangNativeHash.Atom(function.ExternalIdentity.Module.Name);
                        hash = hash * ErlangLegacyHashPrimes.Prime1 + ErlangNativeHash.Atom(function.ExternalIdentity.Function.Name);
                        break;
                    case FunctionTerm { LocalIdentity: not null } function:
                        hash = hash * ErlangLegacyHashPrimes.Prime10 + (uint)function.Environment.Count;
                        hash = hash * ErlangLegacyHashPrimes.Prime1 + ErlangNativeHash.Atom(function.LocalIdentity.Module.Name);
                        hash = hash * ErlangLegacyHashPrimes.Prime2 + (uint)function.LocalIdentity.Index;
                        hash = hash * ErlangLegacyHashPrimes.Prime2 + (uint)function.LocalIdentity.OldUnique;
                        Push(stack, function.Environment);
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }

        return hash;
    }

    private static void Word(ref uint hash, uint value, uint prime)
    {
        unchecked
        {
            for (int index = 0; index < ErlangHashParameters.IntegerWordBytes; index++)
            {
                hash = hash * prime + (value & byte.MaxValue);
                value >>= ErlangHashParameters.ByteBits;
            }
        }
    }

    private static void Push(Stack<Frame> stack, IReadOnlyList<Term> terms)
    {
        for (int index = terms.Count - 1; index >= 0; index--)
            stack.Push(new Frame(FrameKind.Term, terms[index]));
    }

    private enum FrameKind
    {
        Term,
        RecordEnd,
        TupleEnd,
        Tail,
        TailEnd
    }

    private readonly record struct Frame(FrameKind Kind, Term? Term, uint Count = 0);
}
