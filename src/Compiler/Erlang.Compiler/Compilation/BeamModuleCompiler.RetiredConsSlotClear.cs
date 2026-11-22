using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearRetiredConsSlotBeforeCall(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var result = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            if (body[boundary].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            for (int clear = start + 5; clear + 1 < boundary; clear++)
            {
                if (body[clear - 1].Definition.Opcode != Label || body[clear] is not
                    {
                        Definition.Opcode: InitYRegisters,
                        Operands: [{ Value.IsOne: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                    }
                    || ReadRetiredConsSlot(
                        body,
                        start,
                        boundary,
                        clear,
                        result,
                        slot
                    ) is not { } placement)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.HasStraightCheckedCallPrefix(placement.Definition, placement.Call)
                    || !flow.HasOnlyConsClearFallthrough(placement.Definition, placement.Call)
                    || !flow.HasForwardNestedStatusSuffix(clear, boundary))
                {
                    continue;
                }

                // The other paths leave the initially nil slot untouched.
                // This path transfers the old value into its outgoing list.
                output.Write(code.Span[copied..body[placement.Line].Offset]);
                output.Write(code.Span[body[clear].Offset..body[clear + 1].Offset]);
                output.Write(code.Span[body[placement.Line].Offset..body[clear].Offset]);
                copied = body[clear + 1].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Definition, int Line, int Call)? ReadRetiredConsSlot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int clear,
        BeamOperand result,
        BeamOperand slot
    )
    {
        if (ReadDisjointFrameOwnership(
            body,
            start,
            end,
            trackSiblingTrimEpochs: true
        ) is not { } frame || frame.Uses.Count > 2)
        {
            return null;
        }

        int allocation = frame.Owners[clear - start];
        if (allocation < start || allocation + 2 >= clear || !frame.Uses.ContainsKey(allocation)
            || body[allocation].Definition.Opcode != Allocate
            || body[allocation + 1].Definition.Opcode != InitYRegisters
            || !body[allocation + 1].Operands.Contains(slot)
            || frame.Uses.Keys.Any(index => index != allocation
                && (index <= clear || body[index].Definition.Opcode != TrimFrame)))
        {
            return null;
        }

        int[] uses = Enumerable.Range(allocation + 2, clear - allocation - 2).Where(index =>
            frame.Owners[index - start] == allocation
            && body[index].Operands.Any(operand => SameProjectionRegister(operand, slot))).ToArray();
        if (uses is not [var definition, var consumed]
            || !IsMove(body[definition], result, slot)
            || body[consumed] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
            || head != slot || tail != result || list != result)
        {
            return null;
        }

        int line = consumed + 1;
        while (line < clear && body[line] is
            { Definition.Opcode: PutList, Operands: [_, var previous, var output] }
            && previous == result && output == result)
        {
            line++;
        }

        if (line + 1 >= clear || body[line] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
            || body[line + 1] is not
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
            || frame.Owners[line + 1 - start] != allocation)
        {
            return null;
        }

        return (definition, line, line + 1);
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasOnlyConsClearFallthrough(int start, int end) =>
            Enumerable.Range(start, end - start)
                .All(index => successors[index] is [var next] && next == index + 1);
    }
}
