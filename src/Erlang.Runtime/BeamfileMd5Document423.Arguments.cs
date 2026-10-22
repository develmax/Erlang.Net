using System.Numerics;

namespace Erlang;

internal sealed partial class BeamfileMd5Document423
{
    private BeamfileMd5Argument423[] ReadMetadataOp(BeamCursor cursor, int opcode, int fixedArity)
    {
        Require(cursor.ReadByte() == opcode);
        int count = fixedArity;
        var result = new List<BeamfileMd5Argument423>();
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            BeamCompactValue raw = BeamCompactReader.Read(cursor);
            if (raw.WireTag == BeamOperandParameters.ExtendedTag && raw.Value == BeamOperandParameters.ArgumentList)
            {
                Require(raw.FitsNativeWord && index + 1 == count);
                long nativeExtra = ReadNativeUnsigned(cursor);
                int extra = unchecked((int)nativeExtra);
                Require(extra >= 0 && extra <= int.MaxValue - count);
                count += extra;
                result.Add(new(BeamWire.UnsignedTag, nativeExtra));
                continue;
            }
            result.Add(Marshal(cursor, raw));
        }

        return result.ToArray();
    }

    private static long ReadNativeUnsigned(BeamCursor cursor)
    {
        BeamCompactValue raw = BeamCompactReader.Read(cursor);
        Require(raw.WireTag == BeamWire.UnsignedTag && raw.FitsNativeWord && raw.Value >= 0);

        return (long)raw.Value;
    }

    private static int ReadUnsigned(BeamCursor cursor)
    {
        BeamCompactValue raw = BeamCompactReader.Read(cursor);
        Require(raw.WireTag == BeamWire.UnsignedTag && raw.FitsNativeWord && raw.Value >= 0 && raw.Value <= int.MaxValue);

        return (int)raw.Value;
    }

    private BeamfileMd5Argument423 Marshal(BeamCursor cursor, BeamCompactValue raw)
    {
        int tag = raw.WireTag;
        BigInteger value = raw.Value;
        if (tag == BeamWire.IntegerTag)
        {
            Require(BeamIntegerNormalization.IsNormalized(raw));

            return raw.FitsNativeWord ? new(tag, value)
                : new(BeamfileMd5Names423.LiteralArgument, 0, new Integer(value));
        }
        if (!raw.FitsNativeWord) return new(BeamfileMd5Names423.OverflowArgument, 0);
        switch (tag)
        {
            case BeamWire.UnsignedTag:
                return new(tag, value);
            case BeamOperandParameters.AtomTag:
                Require(value >= 0 && value < atoms.Count);

                return value == 0 ? new(BeamfileMd5Names423.NilArgument, 0)
                    : new(tag, value, Term.A(atoms[(int)value]!));
            case BeamOperandParameters.LabelTag:
                Require(value >= 0 && (value == 0 || value < code.LabelCount));

                return new(value == 0 ? BeamfileMd5Names423.FailureArgument : tag, value);
            case BeamOperandParameters.XTag:
            case BeamOperandParameters.YTag:
                Require(value >= 0 && value < BeamOperandParameters.MaximumRegister);

                return new(tag, value);
            case BeamOperandParameters.ExtendedTag:
                if (value == BeamOperandParameters.Literal)
                {
                    int literal = ReadUnsigned(cursor);
                    Require(literal < literals.Count);

                    return new(BeamfileMd5Names423.LiteralArgument, literal, literals[literal]);
                }
                if (value == BeamOperandParameters.FloatRegister)
                {
                    int register = ReadUnsigned(cursor);
                    Require(register < BeamOperandParameters.MaximumRegister);

                    return new(BeamfileMd5Names423.FloatArgument, register);
                }
                if (value == BeamOperandParameters.TypedRegister)
                {
                    BeamCompactValue register = BeamCompactReader.Read(cursor);
                    Require(register.FitsNativeWord && register.WireTag is BeamOperandParameters.XTag or BeamOperandParameters.YTag);
                    long hint = ReadNativeUnsigned(cursor);
                    Require(types.Fallback || hint < types.Entries.Count);

                    return new(register.WireTag, register.Value);
                }
                if (value == BeamOperandParameters.AllocationList)
                {
                    int count = ReadUnsigned(cursor);
                    Require(count <= BeamfileMd5Names423.MaximumAllocationEntries);
                    long total = 0;
                    for (int index = 0; index < count; index++)
                    {
                        int kind = ReadUnsigned(cursor), number = ReadUnsigned(cursor);
                        Require(number <= int.MaxValue / (BeamAllocationWire.NativeFunWords64 + 1));
                        total += kind switch
                        {
                            0 => number,
                            1 => (long)BeamAllocationWire.NativeFloatWords64 * number,
                            2 => (long)BeamAllocationWire.NativeFunWords64 * number,
                            _ => throw new InvalidDataException(BeamfileMd5Names423.Invalid)
                        };
                        Require(total <= int.MaxValue);
                    }

                    return new(BeamWire.UnsignedTag, total);
                }
                break;
        }
        throw new InvalidDataException(BeamfileMd5Names423.Invalid);
    }
}
