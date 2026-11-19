using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{

    private byte[]? AnnotateBinaryExtractRanges(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<string> atomNames
    )
    {
        if (!SsaTypeInferenceEnabled || !body.Any(instruction => instruction.Definition.Opcode is
            BinaryMatch or GetBinaryInteger or GetBinaryUtf8 or GetBinaryUtf16 or GetBinaryUtf32))
        {
            return null;
        }

        var facts = new Dictionary<(string Kind, BigInteger Register), (long? Lower, long? Upper)>();
        using var output = new MemoryStream();
        int copied = 0;
        for (int instructionIndex = 0; instructionIndex < body.Count; instructionIndex++)
        {
            BeamInstruction instruction = body[instructionIndex];
            byte opcode = instruction.Definition.Opcode;
            IReadOnlyList<BeamOperand> operands = instruction.Operands;
            bool comparison = opcode is IsLess or IsGreaterOrEqual or ExactEqualTest or ExactNotEqualTest or NumericEqualTest or NumericNotEqualTest;
            bool arithmetic = opcode is GcBifOne or GcBifTwo or GcBifThree;
            bool mayBeLateSwitch = opcode == ExactEqualTest && IsPackedMatchSelectorComparison(body, instructionIndex);
            if (facts.Count != 0 && !mayBeLateSwitch && (comparison || arithmetic || opcode == SelectValue))
            {
                int start = comparison ? 1 : arithmetic ? 3 : 0;
                int end = comparison ? 3 : arithmetic ? operands.Count - 1 : 1;
                var cursor = new BeamCursor(code[(instruction.Offset + 1)..]);
                for (int index = 0; index < end; index++)
                {
                    int operandOffset = instruction.Offset + 1 + cursor.Position;
                    BeamCompactValue wire = BeamCompactReader.Read(cursor);
                    if (wire.WireTag == BeamOperandParameters.ExtendedTag)
                    {
                        _ = BeamCompactReader.Read(cursor);
                        if (operands[index].TypeIndex.HasValue)
                        {
                            _ = BeamCompactReader.Read(cursor);
                        }
                    }

                    BeamOperand operand = operands[index];
                    if (index >= start && operand.TypeIndex is null
                        && facts.TryGetValue((operand.Kind, operand.Value), out var range))
                    {
                        output.Write(code.Span[copied..operandOffset]);
                        output.WriteByte(ExtendedTypedRegisterOperand);
                        int after = instruction.Offset + 1 + cursor.Position;
                        output.Write(code.Span[operandOffset..after]);
                        int type = GetOperandTypeIndex((IntegerTypeBits, range.Lower, range.Upper, null));
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
                        copied = after;
                    }
                }
            }

            switch (opcode)
            {
                case BinaryMatch:
                    ReadMatchCommands(operands);
                    break;
                case GetBinaryInteger:
                    KillUnrootedX(operands[2].Value);
                    SetRange(operands[^1], ReadFixedWidth(operands[3], operands[4],
                        ((int)operands[5].Value & BeamBinaryFlagNames.SignedBit) != 0));
                    break;
                case GetBinaryUtf8 or GetBinaryUtf16 or GetBinaryUtf32:
                    KillUnrootedX(operands[2].Value);
                    SetRange(operands[^1], (0, UtfSegmentLimits.MaximumScalar));
                    break;
                case Move:
                    var source = (operands[0].Kind, operands[0].Value);
                    SetRange(operands[1], facts.TryGetValue(source, out var known) ? known : null);
                    break;
                case TestHeap:
                    KillUnrootedX(operands[1].Value);
                    break;
                case GcBifOne or GcBifTwo or GcBifThree:
                    KillUnrootedX(operands[1].Value);
                    SetRange(operands[^1], null);
                    break;
                case BifZero or BifOne or BifTwo:
                    SetRange(operands[^1], null);
                    break;
                case Line or ExecutableLine:
                    break;
                default:
                    if (!comparison)
                    {
                        // Bound facts describe only this fallthrough block.
                        facts.Clear();
                    }
                    break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void SetRange(BeamOperand operand, (long? Lower, long? Upper)? range)
        {
            if (operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
            {
                var key = (operand.Kind, operand.Value);
                if (range.HasValue)
                {
                    facts[key] = range.Value;
                }
                else
                {
                    facts.Remove(key);
                }
            }
        }

        void KillUnrootedX(BigInteger live)
        {
            foreach (var key in facts.Keys.Where(key => key.Kind == BeamOperandParameters.X && key.Register >= live).ToArray())
            {
                facts.Remove(key);
            }
        }

        void ReadMatchCommands(IReadOnlyList<BeamOperand> operands)
        {
            int cursor = BeamBinaryNames.MatchPrefix;
            while (cursor < operands.Count)
            {
                string name = atomNames[(int)operands[cursor++].Value - 1];
                switch (name)
                {
                    case BeamBinaryNames.EnsureAtLeast:
                        cursor += 2;
                        break;
                    case BeamBinaryNames.EnsureExactly or BeamBinaryNames.SkipCommand:
                        cursor++;
                        break;
                    case BeamBinaryNames.EqualCommand:
                        cursor += 3;
                        break;
                    case BeamBinaryNames.BinarySegment or BeamBinaryNames.IntegerSegment:
                        KillUnrootedX(operands[cursor].Value);
                        SetRange(
                            operands[cursor + 4],
                            name == BeamBinaryNames.IntegerSegment
                            && operands[cursor + 1].Kind == BeamOperandParameters.Nil
                            ? ReadFixedWidth(operands[cursor + 2], operands[cursor + 3], signed: false)
                            : null
                        );
                        cursor += 5;
                        break;
                    case BeamBinaryNames.GetTail:
                        KillUnrootedX(operands[cursor].Value);
                        SetRange(operands[cursor + 2], null);
                        cursor += 3;
                        break;
                    default:
                        facts.Clear();

                        return;
                }
            }
        }
    }

    private static bool IsPackedMatchSelectorComparison(IReadOnlyList<BeamInstruction> body, int index)
    {
        if (index == 0 || index + 1 == body.Count
            || body[index - 1] is not { Definition.Opcode: BinaryMatch } first
            || body[index + 1] is not { Definition.Opcode: BinaryMatch } next)
        {
            return false;
        }

        // Packed matching can contain an exact test introduced from a switch after
        // OTP's type pass. Keep its existing typing when the match context continues.
        return SameProjectionRegister(first.Operands[1], next.Operands[1])
            && body[index].Operands.Skip(1).Any(operand => operand.Kind == BeamOperandParameters.Integer);
    }

    private static (long? Lower, long? Upper)? ReadFixedWidth(BeamOperand size, BeamOperand unit, bool signed)
    {
        if (size.Kind is not (BeamOperandParameters.Unsigned or BeamOperandParameters.Integer))
        {
            return null;
        }

        BigInteger bits = size.Value * unit.Value;
        if (bits < 1 || bits > BeamBinaryExtractRangeLimits.MaximumInferredSegmentBits)
        {
            return null;
        }

        // beam_ssa_type:bs_match_type/3, followed by beam_types:encode_range/2.
        int valueBits = (int)bits - (signed ? 1 : 0);
        BigInteger limit = BigInteger.One << valueBits;
        BigInteger lower = signed ? -limit : 0;
        BigInteger upper = limit - 1;

        return (lower >= BeamBinaryExtractRangeLimits.MinimumEncodedInteger ? (long)lower : null, upper <= BeamBinaryExtractRangeLimits.MaximumEncodedInteger ? (long)upper : null);
    }
}
