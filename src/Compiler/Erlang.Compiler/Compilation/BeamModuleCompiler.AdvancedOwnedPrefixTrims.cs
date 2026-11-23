using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AdvanceClearedOwnedPrefixTrim(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stack) = decoded;
        BeamOperand retired = new(BeamOperandParameters.Y, 0);
        BeamOperand result = new(BeamOperandParameters.X, 0);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? frame = null;
            for (int clear = start + 2; clear + 5 < end; clear++)
            {
                if (body[clear].Definition.Opcode != InitYRegisters
                    || body[clear].Operands.Count(operand => operand.Kind == BeamOperandParameters.Y) != 1
                    || !body[clear].Operands.Contains(retired)
                    || body[clear - 1] is not { Definition.Opcode: Move, Operands: [var saved, { Kind: BeamOperandParameters.X }] }
                    || saved != retired)
                {
                    continue;
                }

                int calls = 0;
                int store = clear + 1;
                for (; store + 2 < end; store++)
                {
                    BeamInstruction site = body[store];
                    if (site.Definition.Opcode is LocalCall or ExternalCall)
                    {
                        calls++;
                        continue;
                    }

                    if (site.Definition.Opcode == Line)
                    {
                        continue;
                    }

                    if (site is not { Definition.Opcode: Move, Operands: [var value, { Kind: BeamOperandParameters.X }] }
                        || SameProjectionRegister(value, retired))
                    {
                        break;
                    }
                }

                if (calls < 2 || store + 2 >= end || !IsMove(body[store], result, retired)
                    || body[store + 1] is not { Definition.Opcode: TrimFrame, Operands: [{ Value.IsOne: true }, var kept] }
                    || kept.Value <= 0
                    || (frame ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ))
                        is not { Owners: var owners, Uses: var uses }
                    || owners[clear - start] != owners[store + 1 - start]
                    || !uses.TryGetValue(owners[clear - start], out bool[]? slots) || slots.Length != kept.Value + 1)
                {
                    continue;
                }

                // The copied outgoing argument already owns the only value
                // in the cleared prefix. The intervening calls read the
                // kept suffix only. A subsequent store into the retired
                // prefix cannot survive the existing trim.
                int from = body[clear + 1].Offset;
                int until = body[store].Offset;
                int operand = FirstStackOperandAtOrAfter(stack, from);
                output.Write(code.Span[copied..body[clear].Offset]);
                output.Write(code.Span[body[store + 1].Offset..body[store + 2].Offset]);
                while (operand < stack.Count && stack[operand].Start < until)
                {
                    var (at, after, slot) = stack[operand++];
                    output.Write(code.Span[from..at]);
                    BeamCompactWriter.Write(output, YRegisterTag, slot - 1);
                    from = after;
                }

                output.Write(code.Span[from..until]);
                copied = body[store + 2].Offset;
                clear = store + 1;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
