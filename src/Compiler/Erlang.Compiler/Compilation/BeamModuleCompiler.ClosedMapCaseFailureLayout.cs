using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CopiedMapFailure(
        int Start,
        int Resume,
        int RemoveFrom,
        BeamOperand Map,
        int FirstReference
    );

    private byte[]? OutlineClosedMapCaseFailures(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CaseEnd)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        ILookup<int, (int Index, BeamInstruction Instruction)>? transfers = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            var boundary = body[end].Definition.Opcode;
            if (boundary is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int tail = end;
            if (boundary == FunctionInfo)
            {
                while (tail > start && body[tail - 1].Definition.Opcode == Line)
                {
                    tail--;
                }

                if (tail == start || body[--tail].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            if (tail <= start || body[start].Definition.Opcode != Label
                || !EndsBeamBlock(body[tail - 1].Definition.Opcode)
                || body.Skip(start).Take(tail - start).Any(instruction => instruction.Definition.Opcode
                    is TryEnter or CatchEnter or LoopReceive or Wait or DebugLine or ExecutableLine))
            {
                continue;
            }

            var failures = new List<CopiedMapFailure>(2);
            for (int index = start + 1; index + 4 < tail; index++)
            {
                if (ReadFailure(index, start) is { } failure)
                {
                    failures.Add(failure);
                }
            }

            if (failures is not [var outer, var inner] || outer.Map != inner.Map || inner.FirstReference < outer.Resume
                || !PreservesFollowingCallLocations(outer.Resume, inner.Start)
                || !PreservesFollowingCallLocations(inner.Resume, tail))
            {
                continue;
            }

            foreach (CopiedMapFailure failure in failures)
            {
                output.Write(code.Span[copied..body[failure.RemoveFrom].Offset]);
                copied = body[failure.Resume].Offset;
            }

            output.Write(code.Span[copied..body[tail].Offset]);
            copied = body[tail].Offset;
            // The earlier case encloses the later normal continuation. Its
            // failure follows the nested case's failure in final block order.
            output.Write(code.Span[body[inner.Start].Offset..body[inner.Resume].Offset]);
            output.Write(code.Span[body[outer.Start].Offset..body[outer.Resume].Offset]);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return RenumberOutlinedMapCaseLabels(output.ToArray(), labelCount);

        CopiedMapFailure? ReadFailure(int index, int start)
        {
            if (!EndsBeamBlock(body[index - 1].Definition.Opcode)
                || body[index - 1].Definition.Opcode is not (Jump or Return)
                || body[index] is not { Definition.Opcode: Label, Operands: [var label] }
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 } map,
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true } result
                    ]
                }
                || body[index + 2] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                || body[index + 3] is not { Definition.Opcode: CaseEnd, Operands: [var payload] } || payload != result
                || body[index + 4] is not { Definition.Opcode: Label, Operands: [var resume] })
            {
                return null;
            }

            transfers ??= body.SelectMany((instruction, position) => instruction.Operands
                    .Where(operand => operand.Kind == BeamOperandParameters.Label)
                    .Select(operand => (Label: (int)operand.Value, Index: position, Instruction: instruction)))
                .ToLookup(pair => pair.Label, pair => (pair.Index, pair.Instruction));
            var incoming = transfers[(int)label.Value].ToArray();
            if (!incoming.Any(edge => edge.Instruction.Definition.Opcode == GetMapElements)
                || incoming.Any(edge => edge.Index <= start || edge.Index >= index
                    || edge.Instruction.Definition.Opcode is not (IsMap or GetMapElements)
                    || !SameProjectionRegister(edge.Instruction.Operands[1], map)))
            {
                return null;
            }

            int removeFrom = body[index - 1] is { Definition.Opcode: Jump, Operands: [var destination] }
                && destination.Value == resume.Value ? index - 1 : index;

            return new(
                index,
                index + 4,
                removeFrom,
                map,
                incoming.Min(edge => edge.Index)
            );
        }

        bool PreservesFollowingCallLocations(int start, int end)
        {
            for (int index = start; index < end; index++)
            {
                switch (body[index].Definition.Opcode)
                {
                    case Line:
                        return true;
                    case LocalCall or ExternalCall or TailLocalCall or TailLocalCallWithFrame
                        or TailExternalCall or TailExternalCallWithFrame or DynamicApply or TailDynamicApply or CallFun:
                        return false;
                }
            }

            return true;
        }
    }

    private byte[]? RenumberOutlinedMapCaseLabels(
        ReadOnlyMemory<byte> code,
        int labelCount,
        int? physicalLabelCount = null
    )
    {
        var references = new List<(int Start, int End, int Label)>();
        var header = new BeamCodeHeader(
            BeamWire.FormatVersion,
            highestOpcode,
            labelCount,
            exports.Count + locals.Count,
            code
        );
        var body = BeamInstructionReader.Read(
            header,
            atoms.Count + 1,
            literalBytes.Count,
            recordLabelOperand: (start, end, label) => references.Add((start, end, label))
        );
        var labels = new int[labelCount];
        var edits = new List<CodeEdit>();
        int next = 1;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var label] })
            {
                continue;
            }

            int prior = (int)label.Value;
            labels[prior] = next++;
            if (labels[prior] != prior)
            {
                edits.Add(new CodeEdit(
                    body[index].Offset + 1,
                    body[index + 1].Offset,
                    BeamWire.UnsignedTag,
                    labels[prior]
                ));
            }
        }

        if (next != (physicalLabelCount ?? labelCount) || references.Any(reference => reference.Label != 0 && labels[reference.Label] == 0))
        {
            return null;
        }

        ComposeEmittedPhysicalLabels(labels);

        return RewriteLabelOperands(
            code,
            references,
            labels,
            edits
        );
    }
}
