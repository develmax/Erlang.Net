using System.Numerics;

namespace Erlang;

internal static class BeamBinaryFieldExecution
{
    internal static bool GetInteger(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        Term size = BeamRegisterInterpreter.Read(code, registers, operands[3]);
        if (!TryLength(
            match,
            size,
            (int)operands[4].Value,
            out int bits
        ))
            return false;
        registers.CheckLive((int)operands[2].Value);
        // Source/destination may alias. Construct the result before committing.
        int flags = BeamBinaryWireValues.NumericFlags((int)operands[5].Value);
        BigInteger value = BeamBinaryWireValues.Integer(match, bits, flags);
        match.Position += bits;
        registers.Write(operands[6], new Integer(value));

        return true;
    }

    internal static bool Skip(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        Term size = BeamRegisterInterpreter.Read(code, registers, operands[2]);
        if (!TryLength(
            match,
            size,
            (int)operands[3].Value,
            out int bits
        ))
            return false;
        match.Position += bits;

        return true;
    }

    internal static bool Utf8(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands, bool skip)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        if (!TryUtf8(match, out int scalar, out int bits))
            return false;
        registers.CheckLive((int)operands[2].Value);
        match.Position += bits;
        if (!skip)
            registers.Write(operands[4], Term.I(scalar));

        return true;
    }

    private static bool TryLength(
        BeamBinaryMatchContext match,
        Term size,
        int unit,
        out int bits
    )
    {
        bits = 0;
        if (size is not Integer integer || integer.Value < 0)
            return false;
        BigInteger length = integer.Value * unit;
        int remaining = match.Source.BitLength - match.Position;
        if (length > remaining)
            return false;
        bits = (int)length;

        return true;
    }

    private static bool TryUtf8(BeamBinaryMatchContext match, out int scalar, out int bits)
    {
        scalar = 0;
        bits = 0;
        int remaining = match.Source.BitLength - match.Position;
        if (remaining < BeamBinaryNames.BitsPerByte)
            return false;
        int first = ByteAt(match, 0);
        int length;
        int minimum;
        if (first <= BeamBinaryFieldNames.Utf8AsciiMaximum)
        {
            length = 1;
            minimum = 0;
            scalar = first;
        }
        else if (first is >= BeamBinaryFieldNames.Utf8TwoByteMinimum and <= BeamBinaryFieldNames.Utf8TwoByteMaximum)
        {
            length = 2;
            minimum = BeamBinaryFieldNames.Utf8TwoByteScalarMinimum;
            scalar = first & BeamBinaryFieldNames.Utf8TwoByteLeadPayload;
        }
        else if (first is >= BeamBinaryFieldNames.Utf8ThreeByteMinimum and <= BeamBinaryFieldNames.Utf8ThreeByteMaximum)
        {
            length = 3;
            minimum = BeamBinaryFieldNames.Utf8ThreeByteScalarMinimum;
            scalar = first & BeamBinaryFieldNames.Utf8ThreeByteLeadPayload;
        }
        else if (first is >= BeamBinaryFieldNames.Utf8FourByteMinimum and <= BeamBinaryFieldNames.Utf8FourByteMaximum)
        {
            length = 4;
            minimum = BeamBinaryFieldNames.Utf8FourByteScalarMinimum;
            scalar = first & BeamBinaryFieldNames.Utf8FourByteLeadPayload;
        }
        else
            return false;
        if (remaining < length * BeamBinaryNames.BitsPerByte)
            return false;
        for (int index = 1; index < length; index++)
        {
            int next = ByteAt(match, index);
            if ((next & BeamBinaryFieldNames.Utf8ContinuationMask) != BeamBinaryFieldNames.Utf8ContinuationTag)
                return false;
            scalar = (scalar << BeamBinaryFieldNames.Utf8ContinuationBits) | (next & BeamBinaryFieldNames.Utf8ContinuationPayload);
        }
        if (scalar < minimum || scalar > BeamBinaryFieldNames.UnicodeScalarMaximum
            || scalar is >= BeamBinaryFieldNames.SurrogateMinimum and <= BeamBinaryFieldNames.SurrogateMaximum)
            return false;
        bits = length * BeamBinaryNames.BitsPerByte;

        return true;
    }

    private static int ByteAt(BeamBinaryMatchContext match, int index)
    {
        int position = match.Position + index * BeamBinaryNames.BitsPerByte;
        int value = 0;
        for (int bit = 0; bit < BeamBinaryNames.BitsPerByte; bit++)
            value = (value << 1) | match.Source.Bit(position + bit);

        return value;
    }
}
