using System.Numerics;

namespace Erlang;

internal static class BeamBinaryExecution
{
    private sealed record Segment(
        Term Value,
        int Bits,
        bool IsInteger,
        int Unit,
        bool IsAll,
        string Kind,
        int Number,
        bool LittleEndian
    );

    internal static BitString Create(
        ProcessContext? context,
        BeamLinkedCode code,
        BeamRegisterState registers,
        IReadOnlyList<BeamOperand> operands
    )
    {
        var segments = new List<Segment>();
        BitString? appendPrefix = null;
        int total = 0;
        for (int index = BeamBinaryNames.CreatePrefix; index < operands.Count; index += BeamBinaryNames.SegmentOperands)
        {
            string kind = code.Structure.Atoms[(int)operands[index].Value]!;
            int unit = (int)operands[index + 2].Value;
            Term value;
            BigInteger length;
            bool all = false;
            bool littleEndian = kind is BeamBinaryNames.IntegerSegment or BeamBinaryNames.FloatSegment
                && BeamBinaryWireValues.Little(BeamBinaryWireValues.ListFlags(code.Structure, operands[index + 3], kind == BeamBinaryNames.IntegerSegment));
            if (kind == BeamBinaryNames.StringSegment)
            {
                ReadOnlyMemory<byte> strings = code.Structure.Chunks.Single(chunk => chunk.Id == BeamBinaryNames.StringTable).Payload;
                int count = (int)operands[index + 5].Value;
                value = new BitString(strings.Span.Slice((int)operands[index + 4].Value, count));
                length = (BigInteger)count * BeamBinaryNames.BitsPerByte;
            }
            else
            {
                value = BeamRegisterInterpreter.Read(code, registers, operands[index + 4]);
                Term size = BeamRegisterInterpreter.Read(code, registers, operands[index + 5]);
                if (kind is BeamBinaryNames.Utf8Segment or BeamBinaryNames.Utf32Segment)
                {
                    // Pinned x86 JIT validates UTF8 and UTF32 during its size pass.
                    try
                    {
                        BitString encoded = kind == BeamBinaryNames.Utf8Segment
                            ? BeamBinaryConstructionValues.Utf8(value)
                            : BeamUnicodeFields.Encode(value, true, BeamUnicodeFields.LittleEndian(code.Structure, operands[index + 3]));
                        value = encoded;
                        length = encoded.BitLength;
                    }
                    catch (ErlangException error)
                    {
                        throw BeamBinaryConstructionValues.CaptureFailure(
                            context,
                            error,
                            (int)operands[index + 1].Value,
                            kind,
                            value
                        );
                    }
                }
                else if (kind == BeamBinaryNames.Utf16Segment)
                {
                    // UTF16 validation is deferred until after the complete size pass.
                    littleEndian = BeamUnicodeFields.LittleEndian(code.Structure, operands[index + 3]);
                    length = BeamUnicodeFields.Utf16Bits(value);
                }
                else if (kind is BeamBinaryNames.BinarySegment or BeamBinaryNames.PrivateAppend or BeamBinaryNames.AppendSegment
                    && size is Atom { Name: BeamBinaryNames.AllSize })
                {
                    all = true;
                    if (value is not BitString binary)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    length = binary.BitLength;
                    if (kind == BeamBinaryNames.PrivateAppend)
                        appendPrefix = binary;
                }
                else if (size is Integer count && count.Value >= 0)
                {
                    length = count.Value * unit;
                }
                else
                {
                    throw BeamBinaryConstructionValues.CaptureFailure(
                        context,
                        new ErlangException(ErlangErrorReasons.BadArgument),
                        (int)operands[index + 1].Value,
                        kind,
                        size,
                        BeamBinaryNames.SizeFailure
                    );
                }
            }
            if (length > int.MaxValue - total)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            total += (int)length;
            segments.Add(new(
                value,
                (int)length,
                kind == BeamBinaryNames.IntegerSegment,
                unit,
                all,
                kind,
                (int)operands[index + 1].Value,
                littleEndian
            ));
        }
        // Deferred UTF16 and float conversions follow the complete size pass.
        for (int index = 0; index < segments.Count; index++)
        {
            Segment segment = segments[index];
            if (segment.Kind == BeamBinaryNames.Utf16Segment)
            {
                try
                {
                    segment = segment with { Value = BeamUnicodeFields.Encode(segment.Value, false, segment.LittleEndian) };
                    segments[index] = segment;
                }
                catch (ErlangException error)
                {
                    throw BeamBinaryConstructionValues.CaptureFailure(
                        context,
                        error,
                        segment.Number,
                        segment.Kind,
                        segment.Value
                    );
                }
            }
            if (segment.Kind == BeamBinaryNames.FloatSegment)
            {
                try
                {
                    BitString encoded = BeamBinaryWireValues.Float(segment.Value, segment.Bits, segment.LittleEndian);
                    segment = segment with { Value = encoded };
                    segments[index] = segment;
                }
                catch (ErlangException error)
                {
                    bool invalidSize = segment.Bits is not (BeamFloatStringNames.HalfBits or BeamFloatStringNames.SingleBits or BeamFloatStringNames.DoubleBits);
                    string cause = invalidSize ? BeamBinaryFlagNames.InvalidFloatSize
                        : segment.Value is Integer integer && !integer.TryToDouble(out _)
                            ? BeamBinaryNames.NoFloatFailure : BeamBinaryNames.TypeFailure;
                    throw BeamBinaryConstructionValues.CaptureFailure(
                        context,
                        error,
                        segment.Number,
                        segment.Kind,
                        invalidSize ? Term.I(segment.Bits) : segment.Value,
                        cause
                    );
                }
            }
            if (segment.IsInteger && segment.Value is not Integer)
                throw BeamBinaryConstructionValues.CaptureFailure(
                    context,
                    new ErlangException(ErlangErrorReasons.BadArgument),
                    segment.Number,
                    segment.Kind,
                    segment.Value
                );
            if (!segment.IsInteger && segment.Value is not BitString)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (segment.Value is BitString binary && segment.Bits > binary.BitLength
                || segment.IsAll && segment.Bits % segment.Unit != 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        byte[] bytes = new byte[total / BeamBinaryNames.BitsPerByte + (total % BeamBinaryNames.BitsPerByte == 0 ? 0 : 1)];
        int position = 0;
        foreach (Segment segment in segments)
        {
            if (segment.Value is BitString aligned && position % BeamBinaryNames.BitsPerByte == 0 && segment.Bits % BeamBinaryNames.BitsPerByte == 0)
            {
                aligned.ByteSpan[..(segment.Bits / BeamBinaryNames.BitsPerByte)].CopyTo(bytes.AsSpan(position / BeamBinaryNames.BitsPerByte));
                position += segment.Bits;
                continue;
            }
            for (int bit = 0; bit < segment.Bits; bit++, position++)
            {
                int value = segment.Value is Integer integer
                    ? (int)((integer.Value >> BeamBinaryWireValues.IntegerSourceBit(bit, segment.Bits, segment.LittleEndian)) & BigInteger.One)
                    : ((BitString)segment.Value).Bit(bit);
                bytes[position / BeamBinaryNames.BitsPerByte] |= (byte)(value << (BeamBinaryNames.BitsPerByte - position % BeamBinaryNames.BitsPerByte - 1));
            }
        }

        BitString complete = new(bytes, total);

        return appendPrefix is null ? complete : BitString.FromValidatedAppend(appendPrefix, complete);
    }

    internal static void Start(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        string mode = code.Structure.Atoms[(int)operands[0].Value]!;
        BeamOperand source = operands[2];
        BeamBinaryMatchContext match;
        if (source.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
            && registers.ReadSlot(source).Kind == BeamRegisterSlotKind.BinaryMatchContext)
            match = registers.ReadBinaryMatchContext(source);
        else if (mode == BeamBinaryNames.NoFail && BeamRegisterInterpreter.Read(code, registers, source) is BitString binary)
            match = new(binary);
        else
            throw new InvalidDataException(BeamBinaryNames.InvalidContext);
        registers.CheckLive((int)operands[1].Value);
        registers.WriteBinaryMatchContext(operands[3], match);
    }

    internal static bool StartLegacy(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamOperand source = operands[1];
        BeamBinaryMatchContext match;
        if (source.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
            && registers.ReadSlot(source).Kind == BeamRegisterSlotKind.BinaryMatchContext)
            match = registers.ReadBinaryMatchContext(source);
        else if (BeamRegisterInterpreter.Read(code, registers, source) is BitString binary)
            match = new(binary);
        else
            return false;
        registers.CheckLive((int)operands[2].Value);
        registers.WriteBinaryMatchContext(operands[3], match);

        return true;
    }

    internal static bool ExtractBinary(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        Term size = BeamRegisterInterpreter.Read(code, registers, operands[3]);
        int remaining = match.Source.BitLength - match.Position;
        int unit = (int)operands[4].Value;
        BigInteger bits;
        if (size is Atom { Name: BeamBinaryNames.AllSize })
        {
            if (remaining % unit != 0)
                return false;
            bits = remaining;
        }
        else if (size is Integer count && count.Value >= 0)
            bits = count.Value * unit;
        else
            return false;
        if (bits > remaining)
            return false;
        registers.CheckLive((int)operands[2].Value);
        BitString result = match.Source.SliceBits(match.Position, (int)bits);
        match.Position += (int)bits;
        registers.Write(operands[6], result);

        return true;
    }

    internal static void ExtractTail(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[0]);
        registers.CheckLive((int)operands[2].Value);
        BitString result = match.Source.SliceBits(match.Position, match.Source.BitLength - match.Position);
        registers.Write(operands[1], result);
    }

    internal static void SavePosition(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[0]);
        registers.CheckLive((int)operands[2].Value);
        registers.WriteBinaryPosition(operands[1], new(match.Source, match.Position));
    }

    internal static void RestorePosition(BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[0]);
        BeamBinaryMatchPosition position = registers.ReadBinaryPosition(operands[1]);
        if (!ReferenceEquals(match.Source, position.Source) || position.BitOffset < 0 || position.BitOffset > match.Source.BitLength)
            throw new InvalidDataException(BeamBinaryNames.InvalidContext);
        match.Position = position.BitOffset;
    }

    internal static void InitializeWritable(BeamRegisterState registers)
    {
        Term hint = registers.Read(0);
        int capacity = BeamBinaryNames.DefaultWritableBytes;
        if (hint is Integer integer && integer.Value >= 0
            && integer.Value < (BigInteger.One << BeamTypeWire.NativeSmallMagnitudeBits64))
        {
            if (integer.Value > int.MaxValue / BeamBinaryNames.BitsPerByte)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            capacity = (int)integer.Value;
        }
        BitString result = BitString.InitializeWritable(capacity);
        registers.ClearXExcept();
        registers.Write(0, result);
    }

    internal static bool Match(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(operands[1]);
        foreach (BeamBinaryCommand command in BeamBinaryCommands.Read(code.Structure, operands))
        {
            int remaining = match.Source.BitLength - match.Position;
            if (command.Kind == BeamBinaryCommandKind.EnsureAtLeast)
            {
                if (remaining < command.Bits || (remaining - command.Bits) % command.Unit != 0)
                    return false;
            }
            else if (command.Kind == BeamBinaryCommandKind.EnsureExactly)
            {
                if (remaining != command.Bits)
                    return false;
            }
            else if (command.Kind == BeamBinaryCommandKind.Skip)
            {
                if (remaining < command.Bits)
                    return false;
                match.Position += command.Bits;
            }
            else if (command.Kind == BeamBinaryCommandKind.Binary)
            {
                if (remaining < command.Bits)
                    return false;
                registers.CheckLive(command.Live);
                BitString value = match.Source.SliceBits(match.Position, command.Bits);
                match.Position += command.Bits;
                registers.Write(command.Destination!, value);
            }
            else if (command.Kind == BeamBinaryCommandKind.Tail)
            {
                if (remaining % command.Unit != 0)
                    return false;
                registers.CheckLive(command.Live);
                registers.Write(command.Destination!, match.Source.SliceBits(match.Position, remaining));
            }
            else
            {
                if (remaining < command.Bits)
                    return false;
                BigInteger value = command.Kind == BeamBinaryCommandKind.Integer
                    ? BeamBinaryWireValues.Integer(match, command.Bits, command.Flags)
                    : BeamBinaryWireValues.Integer(match, command.Bits, 0);
                match.Position += command.Bits;
                if (command.Kind == BeamBinaryCommandKind.Equal)
                {
                    if (value != command.Expected)
                        return false;
                }
                else
                {
                    registers.CheckLive(command.Live);
                    registers.Write(command.Destination!, new Integer(value));
                }
            }
        }

        return true;
    }
}
