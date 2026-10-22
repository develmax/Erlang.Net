namespace Erlang;

internal static class BeamBinaryInstructionValidation
{
    internal static int MatchLive(IReadOnlyList<BeamOperand> operands)
        => (int)operands[BeamBinaryNames.TailLiveOperand].Value;

    internal static BeamOperand TailDestination(IReadOnlyList<BeamOperand> operands)
        => operands[BeamBinaryNames.TailDestinationOperand];

    internal static void Validate(BeamInstruction instruction, BeamStructure structure)
    {
        var operands = instruction.Operands;
        switch (instruction.Definition.Name)
        {
            case BeamBinaryNames.Start:
                Require(operands.Count == 4 && Atom(structure, operands[0]) is BeamBinaryNames.NoFail or BeamBinaryNames.Resume);
                Unsigned(operands[1]);
                if (Atom(structure, operands[0]) == BeamBinaryNames.Resume)
                    Register(operands[2]);
                else
                    Source(operands[2]);
                Register(operands[3]);
                break;
            case BeamBinaryNames.StartLegacy:
                Require(operands.Count == 4 && operands[0].Kind == BeamOperandParameters.Label);
                Source(operands[1]);
                Unsigned(operands[2]);
                Register(operands[3]);
                break;
            case BeamBinaryNames.ExtractInteger:
            case BeamBinaryNames.ExtractBinary:
                Require(operands.Count == 7 && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Unsigned(operands[2]);
                Source(operands[3]);
                Unsigned(operands[4]);
                Unsigned(operands[5]);
                Require(operands[4].Value > 0 && operands[4].Value <= BeamBinaryNames.MaximumUnit);
                if (instruction.Definition.Name == BeamBinaryNames.ExtractInteger)
                    _ = BeamBinaryWireValues.NumericFlags((int)operands[5].Value);
                else
                    Require(operands[5].Value == 0);
                Register(operands[6]);
                break;
            case BeamBinaryNames.ExtractUtf8:
            case BeamBinaryNames.SkipUtf8:
                Require(operands.Count == (instruction.Definition.Name == BeamBinaryNames.SkipUtf8 ? 4 : 5)
                    && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Unsigned(operands[2]);
                Unsigned(operands[3]);
                Require(operands[3].Value == 0);
                if (instruction.Definition.Name == BeamBinaryNames.ExtractUtf8)
                    Register(operands[4]);
                break;
            case BeamBinaryNames.ExtractUtf16:
            case BeamBinaryNames.ExtractUtf32:
            case BeamBinaryNames.SkipUtf16:
            case BeamBinaryNames.SkipUtf32:
                bool skipUnicode = instruction.Definition.Name is BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32;
                Require(operands.Count == (skipUnicode ? 4 : 5) && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Unsigned(operands[2]);
                Unsigned(operands[3]);
                Require(operands[3].Value == 0 || operands[3].Value == BeamUnicodeFieldNames.LittleFlag || operands[3].Value == BeamBinaryFlagNames.NativeBit);
                if (!skipUnicode)
                    Register(operands[4]);
                break;
            case BeamFloatStringNames.ExtractFloat:
                Require(operands.Count == 7 && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Unsigned(operands[2]);
                Source(operands[3]);
                Unsigned(operands[4]);
                Unsigned(operands[5]);
                Require(operands[4].Value > 0 && operands[4].Value <= BeamBinaryNames.MaximumUnit
                    && (operands[5].Value == 0 || operands[5].Value == BeamFloatStringNames.LittleFlag || operands[5].Value == BeamFloatStringNames.NativeFlag));
                Register(operands[6]);
                break;
            case BeamFloatStringNames.MatchString:
                Require(operands.Count == 4 && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Unsigned(operands[2]);
                Unsigned(operands[3]);
                ReadOnlyMemory<byte> pattern = structure.Chunks.Single(chunk => chunk.Id == BeamBinaryNames.StringTable).Payload;
                Require(operands[3].Value + (operands[2].Value + BeamBinaryNames.BitsPerByte - 1) / BeamBinaryNames.BitsPerByte <= pattern.Length);
                break;
            case BeamBinaryNames.SkipBits:
                Require(operands.Count == 5 && operands[0].Kind == BeamOperandParameters.Label);
                Register(operands[1]);
                Source(operands[2]);
                Unsigned(operands[3]);
                Unsigned(operands[4]);
                Require(operands[3].Value > 0 && operands[3].Value <= BeamBinaryNames.MaximumUnit && operands[4].Value == 0);
                break;
            case BeamBinaryNames.ExtractTail:
            case BeamBinaryNames.SavePosition:
                Require(operands.Count == 3);
                Register(operands[0]);
                Register(operands[1]);
                Unsigned(operands[2]);
                break;
            case BeamBinaryNames.RestorePosition:
                Require(operands.Count == 2);
                Register(operands[0]);
                Register(operands[1]);
                break;
            case BeamBinaryNames.InitializeWritable:
                Require(operands.Count == 0);
                break;
            case BeamBinaryNames.Match:
                _ = BeamBinaryCommands.Read(structure, operands);
                break;
            case BeamBinaryNames.Create:
                Require(operands[0].Kind is BeamOperandParameters.Label or BeamOperandParameters.Failure);
                foreach (int index in new[] { 1, 2, 3 })
                    Unsigned(operands[index]);
                Require(operands[3].Value > 0 && operands[3].Value <= BeamBinaryNames.MaximumUnit);
                Register(operands[4]);
                Require(operands[5].Kind == BeamOperandParameters.Count
                    && operands[5].Value == operands.Count - BeamBinaryNames.CreatePrefix
                    && operands[5].Value % BeamBinaryNames.SegmentOperands == 0);
                for (int index = BeamBinaryNames.CreatePrefix; index < operands.Count; index += BeamBinaryNames.SegmentOperands)
                {
                    string kind = Atom(structure, operands[index]);
                    Require(kind is BeamBinaryNames.IntegerSegment or BeamBinaryNames.BinarySegment or BeamBinaryNames.StringSegment
                        or BeamBinaryNames.PrivateAppend or BeamBinaryNames.AppendSegment or BeamBinaryNames.Utf8Segment
                        or BeamBinaryNames.Utf16Segment or BeamBinaryNames.Utf32Segment or BeamBinaryNames.FloatSegment);
                    Require(kind is not (BeamBinaryNames.PrivateAppend or BeamBinaryNames.AppendSegment) || index == BeamBinaryNames.CreatePrefix);
                    Unsigned(operands[index + 1]);
                    Unsigned(operands[index + 2]);
                    bool unicode = kind is BeamBinaryNames.Utf8Segment or BeamBinaryNames.Utf16Segment or BeamBinaryNames.Utf32Segment;
                    Require(unicode ? operands[index + 2].Value == 0
                        : operands[index + 2].Value > 0 && operands[index + 2].Value <= BeamBinaryNames.MaximumUnit);
                    if (kind is BeamBinaryNames.Utf16Segment or BeamBinaryNames.Utf32Segment or BeamBinaryNames.FloatSegment or BeamBinaryNames.IntegerSegment)
                        _ = BeamBinaryWireValues.ListFlags(structure, operands[index + 3], kind == BeamBinaryNames.IntegerSegment);
                    else
                        Require(operands[index + 3].Kind == BeamOperandParameters.Nil);
                    if (unicode)
                        Require(operands[index + 5].Kind == BeamOperandParameters.Atom
                            && Atom(structure, operands[index + 5]) == CommonAtomNames.Undefined);
                    if (kind == BeamBinaryNames.StringSegment)
                    {
                        Unsigned(operands[index + 4]);
                        Require(operands[index + 2].Value == BeamBinaryNames.BitsPerByte
                            && operands[index + 5].Kind == BeamOperandParameters.Integer && operands[index + 5].Value >= 0);
                        ReadOnlyMemory<byte> strings = structure.Chunks.Single(chunk => chunk.Id == BeamBinaryNames.StringTable).Payload;
                        Require(operands[index + 4].Value + operands[index + 5].Value <= strings.Length);
                    }
                    else
                    {
                        Source(operands[index + 4]);
                        Source(operands[index + 5]);
                    }
                }
                break;
        }
    }

    private static string Atom(BeamStructure structure, BeamOperand operand)
    {
        Require(operand.Kind == BeamOperandParameters.Atom);

        return structure.Atoms[(int)operand.Value]!;
    }

    private static void Register(BeamOperand operand)
        => Require(operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y);

    private static void Source(BeamOperand operand)
        => Require(operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Integer
            or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue);

    private static void Unsigned(BeamOperand operand)
        => Require(operand.Kind == BeamOperandParameters.Unsigned && operand.Value >= 0 && operand.Value <= int.MaxValue);

    private static void Require(bool valid)
    {
        if (!valid)
            throw new NotSupportedException(BeamBinaryNames.Unsupported);
    }
}
