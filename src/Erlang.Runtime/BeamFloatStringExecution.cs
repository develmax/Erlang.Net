using System.Numerics;

namespace Erlang;

internal static class BeamFloatStringExecution
{
    internal static bool Float(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        Term size = BeamRegisterInterpreter.Read(code, registers, operands[3]);
        if (size is not Integer integer || integer.Value < 0 || integer.Value > BeamFloatStringNames.DoubleBits)
            return false;
        BigInteger length = integer.Value * operands[4].Value;
        if (length != 0 && length != BeamFloatStringNames.HalfBits && length != BeamFloatStringNames.SingleBits && length != BeamFloatStringNames.DoubleBits
            || length > match.Source.BitLength - match.Position)
            return false;
        int bits = (int)length;
        bool little = operands[5].Value == BeamFloatStringNames.LittleFlag
            || operands[5].Value == BeamFloatStringNames.NativeFlag && BitConverter.IsLittleEndian;
        ulong payload = 0;
        for (int index = 0; index < bits / BeamBinaryNames.BitsPerByte; index++)
        {
            ulong next = 0;
            for (int bit = 0; bit < BeamBinaryNames.BitsPerByte; bit++)
                next = (next << 1) | (uint)match.Source.Bit(match.Position + index * BeamBinaryNames.BitsPerByte + bit);
            if (little)
                payload |= next << (index * BeamBinaryNames.BitsPerByte);
            else
                payload = (payload << BeamBinaryNames.BitsPerByte) | next;
        }
        double value = bits switch
        {
            0 => 0.0,
            BeamFloatStringNames.HalfBits => (double)BitConverter.UInt16BitsToHalf((ushort)payload),
            BeamFloatStringNames.SingleBits => BitConverter.UInt32BitsToSingle((uint)payload),
            _ => BitConverter.UInt64BitsToDouble(payload)
        };
        if (!double.IsFinite(value))
            return false;
        registers.CheckLive((int)operands[2].Value);
        match.Position += bits;
        registers.Write(operands[6], new FloatTerm(value));

        return true;
    }

    internal static bool String(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        int bits = (int)operands[2].Value;
        if (bits > match.Source.BitLength - match.Position)
            return false;
        ReadOnlySpan<byte> strings = code.Structure.Chunks.Single(chunk => chunk.Id == BeamBinaryNames.StringTable).Payload.Span;
        int start = (int)operands[3].Value;
        for (int bit = 0; bit < bits; bit++)
        {
            int expected = (strings[start + bit / BeamBinaryNames.BitsPerByte] >> (BeamBinaryNames.BitsPerByte - 1 - bit % BeamBinaryNames.BitsPerByte)) & 1;
            if (match.Source.Bit(match.Position + bit) != expected)
                return false;
        }
        match.Position += bits;

        return true;
    }
}
