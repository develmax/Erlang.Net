using System.Buffers.Binary;
using System.Text;

namespace Erlang;

internal static class BeamUnicodeFields
{
    internal static bool Get(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands, bool utf32)
        => Read(
            registers,
            operands,
            utf32,
            false
        );

    internal static bool Skip(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands, bool utf32)
        => Read(
            registers,
            operands,
            utf32,
            true
        );

    private static bool Read(
        BeamRegisterState registers,
        IReadOnlyList<BeamOperand> operands,
        bool utf32,
        bool skip
    )
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        bool little = BeamBinaryWireValues.Little(BeamBinaryWireValues.NumericFlags((int)operands[3].Value));
        int remaining = match.Source.BitLength - match.Position;
        int bits = utf32 ? BeamUnicodeFieldNames.ScalarBits : BeamUnicodeFieldNames.WordBits;
        if (remaining < bits)
            return false;
        uint value = Word(
            match,
            0,
            utf32 ? sizeof(uint) : sizeof(ushort),
            little
        );
        if (!utf32 && value is >= BeamBinaryFieldNames.SurrogateMinimum and <= BeamBinaryFieldNames.SurrogateMaximum)
        {
            if (value > BeamUnicodeFieldNames.HighSurrogateMaximum || remaining < BeamUnicodeFieldNames.ScalarBits)
                return false;
            uint second = Word(
                match,
                sizeof(ushort),
                sizeof(ushort),
                little
            );
            if (second is < BeamUnicodeFieldNames.LowSurrogateMinimum or > BeamBinaryFieldNames.SurrogateMaximum)
                return false;
            value = ((value & BeamUnicodeFieldNames.SurrogatePayloadMask) << BeamUnicodeFieldNames.SurrogatePayloadBits)
                + (second & BeamUnicodeFieldNames.SurrogatePayloadMask) + BeamUnicodeFieldNames.SupplementaryMinimum;
            bits = BeamUnicodeFieldNames.ScalarBits;
        }
        if (value > BeamBinaryFieldNames.UnicodeScalarMaximum
            || value is >= BeamBinaryFieldNames.SurrogateMinimum and <= BeamBinaryFieldNames.SurrogateMaximum)
            return false;
        registers.CheckLive((int)operands[2].Value);
        match.Position += bits;
        if (!skip)
            registers.Write(operands[4], Term.I(value));

        return true;
    }

    internal static bool LittleEndian(BeamStructure structure, BeamOperand flags)
    {
        return BeamBinaryWireValues.Little(BeamBinaryWireValues.ListFlags(structure, flags, false));
    }

    internal static int Utf16Bits(Term value)
        => value is Integer integer && integer.Value >= 0 && integer.Value < BeamUnicodeFieldNames.SupplementaryMinimum
            ? BeamUnicodeFieldNames.WordBits : BeamUnicodeFieldNames.ScalarBits;

    internal static BitString Encode(Term value, bool utf32, bool little)
    {
        if (value is not Integer integer || integer.Value < 0 || integer.Value > BeamBinaryFieldNames.UnicodeScalarMaximum
            || !Rune.TryCreate((int)integer.Value, out Rune scalar))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (utf32)
        {
            byte[] result = new byte[sizeof(uint)];
            if (little)
                BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)scalar.Value);
            else
                BinaryPrimitives.WriteUInt32BigEndian(result, (uint)scalar.Value);

            return new BitString(result);
        }
        Span<char> words = stackalloc char[2];
        int count = scalar.EncodeToUtf16(words);
        byte[] bytes = new byte[count * sizeof(ushort)];
        for (int index = 0; index < count; index++)
            if (little)
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(index * sizeof(ushort)), words[index]);
            else
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(index * sizeof(ushort)), words[index]);

        return new BitString(bytes);
    }

    // Bounded value reads only. Mutable/resource-origin/unknown families keep
    // the original per-bit route; no pointer or span survives this method.
    private static bool TryStableWord492(
        BitString source,
        int position,
        int bytes,
        bool little,
        out uint value
    )
    {
        value = 0;
        BinaryBacking backing = source.ResourceBacking577;
        if (source.ResourceSliceOrigin577 is not null || backing.HasWritableOrigin) return false;
        if (backing.NativeStorage524 is null)
            return source.TryReadAlignedWord492(
                position,
                bytes,
                little,
                out value
            );
        if (backing.NativeStorage524 is not OrdinaryBinaryBacking ordinary)
            return false;
        if (source.OwnedStorage577 is ProcessTermSequence506 block && block.Owner is ProcessTermHeap506 heap)
        {
            if (heap.LiteralDomain800 is not null) return false;
            lock (heap.Gate)
            {
                _ = heap.IsOwnedLocal(source);
                nuint flags = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + ResourceBinary577Names.BaseWord)
                    & ResourceBinary577Names.FlagMask;
                if (flags != 0 || ordinary.IsWritable || ordinary.WritableBinding is not null) return false;

                return source.TryReadAlignedWord492(
                    position,
                    bytes,
                    little,
                    out value
                );
            }
        }
        if (source.OwnedStorage577 is not null || ordinary.IsWritable || ordinary.WritableBinding is not null)
            return false;

        return source.TryReadAlignedWord492(
            position,
            bytes,
            little,
            out value
        );
    }

    private static uint Word(
        BeamBinaryMatchContext match,
        int byteOffset,
        int bytes,
        bool little
    )
    {
        int start492 = match.Position + byteOffset * BeamBinaryNames.BitsPerByte;
        if (TryStableWord492(
            match.Source,
            start492,
            bytes,
            little,
            out uint stable492
        ))
            return stable492;
        uint value = 0;
        for (int index = 0; index < bytes; index++)
        {
            uint next = 0;
            int position = match.Position + (byteOffset + index) * BeamBinaryNames.BitsPerByte;
            for (int bit = 0; bit < BeamBinaryNames.BitsPerByte; bit++)
                next = (next << 1) | (uint)match.Source.Bit(position + bit);
            if (little)
                value |= next << (index * BeamBinaryNames.BitsPerByte);
            else
                value = (value << BeamBinaryNames.BitsPerByte) | next;
        }

        return value;
    }
}
