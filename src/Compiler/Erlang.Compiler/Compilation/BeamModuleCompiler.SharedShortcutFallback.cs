using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? ShareClosedShortcutFallback(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNil)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        Dictionary<int, int>? labels = null;
        Dictionary<int, int[]>? incoming = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 12 < body.Count; start++)
        {
            if (body[start] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } alternative, var selected, { Kind: BeamOperandParameters.Atom }]
                } || selected != x0 || body[start + 1] is not
                { Definition.Opcode: Move, Operands: [var literal, var fastResult] }
                || fastResult != x0 || !IsNonRegisterValue(literal)
                || body[start + 2] is not { Definition.Opcode: Jump, Operands: [var outerJoin] }
                || !DefinesLabel(start + 3, alternative))
            {
                continue;
            }

            labels ??= body.Select((instruction, index) => (instruction, index))
                .Where(item => item.instruction.Definition.Opcode == Label)
                .ToDictionary(item => (int)item.instruction.Operands[0].Value, item => item.index);
            if (!labels.TryGetValue((int)outerJoin.Value, out int join) || join <= start + 10 || join + 2 >= body.Count
                || body[join - 6] is not { Definition.Opcode: IsNil, Operands: [var success, var lastResult] }
                || lastResult != x0 || body[join - 5] is not
                { Definition.Opcode: Move, Operands: [var fallback, var fallbackResult] }
                || fallback != literal || fallbackResult != x0
                || body[join - 4] is not { Definition.Opcode: Jump, Operands: [var innerJoin] }
                || !DefinesLabel(join - 3, success)
                || body[join - 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retained, var retainedResult]
                }
                || retainedResult != x0 || !DefinesLabel(join - 1, innerJoin)
                || body[join + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var joinedResult, { Kind: BeamOperandParameters.Y, TypeIndex: null } stored]
                }
                || joinedResult != x0 || retained == stored)
            {
                continue;
            }

            incoming ??= body.SelectMany((instruction, index) => instruction.Operands
                .Where(operand => operand.Kind == BeamOperandParameters.Label)
                .Select(operand => (Label: (int)operand.Value, Index: index)))
                .GroupBy(reference => reference.Label, reference => reference.Index)
                .ToDictionary(group => group.Key, group => group.ToArray());
            if (!HasOnlyIncoming(alternative, start) || !HasOnlyIncoming(outerJoin, start + 2)
                || !HasOnlyIncoming(innerJoin, join - 4)
                || !incoming.TryGetValue((int)success.Value, out var matched)
                || matched is not [var firstCheck, var lastCheck] || lastCheck != join - 6
                || firstCheck <= start + 3 || body[firstCheck] is not
                { Definition.Opcode: IsNil, Operands: [_, var firstResult] } || firstResult != x0)
            {
                continue;
            }

            int continuation = join + 2;
            flow ??= new BeamProjectionFlow(body);
            if (!flow.IsEntryTemporaryDead(continuation, x0)
                || ReadShortcutSuffixSlots(
                    body,
                    start,
                    continuation,
                    retained,
                    stored
                ) is not { } suffix || !flow.HasClosedShortcutSelection(start, continuation, suffix.End))
            {
                continue;
            }

            // Both rejected routes have the same closed result. Place that
            // copy after the two producer checks. The other edge already owns
            // the retained result, and the continuation reads its stack slot.
            ReadOnlySpan<byte> wire = code.Span;
            output.Write(wire[copied..body[start].Offset]);
            output.WriteByte(ExactNotEqualTest);
            output.Write(wire[(body[start].Offset + 1)..body[start + 1].Offset]);
            output.Write(wire[body[start + 4].Offset..body[join - 5].Offset]);
            output.Write(wire[body[start + 3].Offset..body[start + 4].Offset]);
            output.Write(wire[body[join - 5].Offset..(body[join - 4].Offset - 1)]);
            WriteRegister(output, retained);
            output.Write(wire[body[join - 3].Offset..body[join - 2].Offset]);
            WriteFrameInitialization(output, [(int)stored.Value]);
            copied = body[continuation].Offset;
            foreach (int index in suffix.Reads)
            {
                foreach (var operand in decoded.StackOperands)
                {
                    if (operand.Start < body[index].Offset || operand.Start >= body[index + 1].Offset
                        || operand.Slot != stored.Value)
                    {
                        continue;
                    }

                    output.Write(wire[copied..operand.Start]);
                    WriteRegister(output, retained);
                    copied = operand.End;
                }
            }
            start = suffix.End - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        bool DefinesLabel(int index, BeamOperand target) =>
            body[index] is { Definition.Opcode: Label, Operands: [var label] } && label.Value == target.Value;

        bool HasOnlyIncoming(BeamOperand label, int from) =>
            incoming!.TryGetValue((int)label.Value, out var uses) && uses is [var source] && source == from;
    }

    private static (int End, List<int> Reads)? ReadShortcutSuffixSlots(
        IReadOnlyList<BeamInstruction> body,
        int selection,
        int continuation,
        BeamOperand retained,
        BeamOperand stored
    )
    {
        int entry = selection;
        while (entry > 0 && body[entry - 1].Definition.Opcode != FunctionInfo)
        {
            entry--;
        }

        int end = continuation;
        while (end + 1 < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        if (ReadDisjointFrameOwnership(
            body,
            entry,
            end,
            trackSiblingTrimEpochs: true
        ) is not { } frame || frame.Uses.Count > 2)
        {
            return null;
        }

        int allocation = frame.Owners[selection - entry];
        if (allocation < entry || frame.Owners[continuation - 1 - entry] != allocation
            || !frame.Uses.ContainsKey(allocation)
            || frame.Uses.Keys.Any(index => index != allocation && (index <= continuation
                || body[index].Definition.Opcode != TrimFrame
                || body[index].Operands[0].Value <= retained.Value || body[index].Operands[0].Value <= stored.Value)))
        {
            return null;
        }

        List<int> reads = [];
        for (int index = continuation; index < end; index++)
        {
            if (frame.Owners[index - entry] != allocation)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            bool remap = false;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (SameProjectionRegister(operand, retained))
                {
                    return null;
                }

                if (!SameProjectionRegister(operand, stored))
                {
                    continue;
                }

                if (operand != stored || instruction.Definition.Opcode is not (Move or PutList)
                    || position == StackWriteOperand(instruction))
                {
                    return null;
                }

                remap = true;
            }

            if (remap)
            {
                reads.Add(index);
            }
        }

        return reads.Count == 0 ? null : (end, reads);
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasClosedShortcutSelection(int selection, int continuation, int end)
        {
            for (int index = selection; index < end; index++)
            {
                bool selected = index < continuation;
                if (index != selection && predecessors[index].Any(source =>
                    source < selection || source >= end || !selected && source < continuation - 1)
                    || successors[index].Any(target => selected && (target < selection || target > continuation)
                        || !selected && target < continuation))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
