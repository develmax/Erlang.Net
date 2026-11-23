using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CanonicalizeCheckedIntegerOrderTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsLess)
            || !code.Span.Contains(IsInteger)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 1; index + 1 < decoded.Body.Count; index++)
        {
            BeamInstruction test = decoded.Body[index];
            if (test is not { Definition.Opcode: IsLess, Operands: [var failure, var left, var right] }
                || decoded.Body[index - 1] is not { Definition.Opcode: IsInteger, Operands: [var rejected, var checkedRegister] }
                || failure != rejected)
            {
                continue;
            }

            bool literalFirst = left.Kind == BeamOperandParameters.Integer && SameProjectionRegister(right, checkedRegister);
            bool literalLast = right.Kind == BeamOperandParameters.Integer && SameProjectionRegister(left, checkedRegister);
            if (!literalFirst && !literalLast)
            {
                continue;
            }

            // beam_jump exchanges the operands using the adjacent bound for a proven integer.
            var value = literalFirst ? right : left;
            var bound = (literalFirst ? left.Value + 1 : right.Value - 1);
            int type = value.TypeIndex ?? GetOperandTypeIndex((IntegerTypeBits, null, null, null));
            output.Write(code.Span[copied..test.Offset]);
            output.WriteByte(IsGreaterOrEqual);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            if (literalLast)
            {
                BeamCompactWriter.Write(output, BeamWire.IntegerTag, bound);
            }

            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            if (literalFirst)
            {
                BeamCompactWriter.Write(output, BeamWire.IntegerTag, bound);
            }

            copied = decoded.Body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, IsGreaterOrEqual);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? AnnotateCheckedIntegerOrderInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsInteger)
            || !code.Span.Contains(IsGreaterOrEqual)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        var bounds = new Dictionary<BeamOperand, (long? Lower, long? Upper)>();
        int copied = 0;
        foreach (BeamInstruction instruction in body)
        {
            if (instruction is
                {
                    Definition.Opcode: IsInteger,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 },
                    { Kind: BeamOperandParameters.X } tested
                    ]
                })
            {
                bounds.TryAdd(tested with { TypeIndex = null }, (null, null));
                continue;
            }

            if (instruction is
                {
                    Definition.Opcode: IsGreaterOrEqual,
                    Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 }, var left, var right]
                })
            {
                // The original value is integer only on the guard's fallthrough.
                // Emit the incoming type, then meet it with the comparison bound.
                Annotate(instruction, 1, left);
                Annotate(instruction, 2, right);
                Refine(left, right, lower: true);
                Refine(right, left, lower: false);
                continue;
            }

            if (instruction.Definition.Opcode != Line)
            {
                bounds.Clear();
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Annotate(BeamInstruction instruction, int position, BeamOperand operand)
        {
            if (IsBareX(operand) && bounds.TryGetValue(operand, out var range))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    position,
                    IntegerTypeBits,
                    range.Lower,
                    range.Upper
                );
            }
        }

        void Refine(BeamOperand operand, BeamOperand literal, bool lower)
        {
            if (literal.Kind != BeamOperandParameters.Integer || literal.Value < long.MinValue
                || literal.Value > long.MaxValue || operand.Kind != BeamOperandParameters.X)
            {
                return;
            }

            BeamOperand register = operand with { TypeIndex = null };
            if (!bounds.TryGetValue(register, out var range))
            {
                return;
            }

            long bound = (long)literal.Value;
            if (lower)
            {
                range.Lower = range.Lower is long previous ? Math.Max(previous, bound) : bound;
            }
            else
            {
                range.Upper = range.Upper is long previous ? Math.Min(previous, bound) : bound;
            }

            if (range.Lower > range.Upper)
            {
                bounds.Remove(register);
            }
            else
            {
                bounds[register] = range;
            }
        }
    }
}
