using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleCheckedParentTupleFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || !code.Span.Contains(GetTupleElement) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        BeamProjectionFlow? flow = null;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? frames = null;
            if (!HasUnprotectedCallKillSchedule(body, start))
            {
                continue;
            }

            for (int heap = start; heap + 8 < end; heap++)
            {
                if (body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                    || body[heap + 1] is not { Definition.Opcode: PutTuple, Operands: [var child, _, _, _, _] } childTuple
                    || child != x0 || childTuple.Operands.Skip(2).Any(operand =>
                        operand.Kind == BeamOperandParameters.X && !SameProjectionRegister(operand, x0))
                    || body[heap + 2] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } parent, var contentField, var content]
                    } || contentField.Value != 2 || !IsBareX(content) || content == x0
                    || body[heap + 3] is not
                    { Definition.Opcode: PutList, Operands: [var item, var tail, var appended] }
                    || item != x0 || tail != content || appended != x0
                    || body[heap + 4] is not
                    { Definition.Opcode: GetTupleElement, Operands: [var namedParent, { Value.IsZero: true }, var name] }
                    || namedParent != parent || name != content
                    || body[heap + 5] is not
                    { Definition.Opcode: GetTupleElement, Operands: [var attributeParent, { Value.IsOne: true }, var attrs] }
                    || attributeParent != parent || !IsBareX(attrs) || attrs == x0 || attrs == name
                    || body[heap + 6] is not
                    { Definition.Opcode: PutTuple, Operands: [var destination, _, var first, var second, var last] }
                    || destination != x0 || first != name || second != attrs || last != x0)
                {
                    continue;
                }

                int scratch = 1;
                while (scratch == name.Value || scratch == attrs.Value)
                {
                    scratch++;
                }

                if (!HasClosedParentTupleSuffix(
                    body,
                    heap + 7,
                    end,
                    scratch
                )
                    || (frames ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners }
                    || !HasRetainedCheckedParent(
                        body,
                        owners,
                        start,
                        heap,
                        parent,
                        contentField,
                        flow ??= new BeamProjectionFlow(body)
                    ))
                {
                    continue;
                }

                // Only X0 was live at the heap check. Project the checked
                // parent as one group before rebuilding its child.
                output.Write(code.Span[copied..body[heap + 1].Offset]);
                output.Write(code.Span[body[heap + 4].Offset..body[heap + 6].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, parent);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, contentField.Value);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                output.Write(code.Span[body[heap + 1].Offset..body[heap + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, x0);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                WriteRegister(output, x0);
                copied = body[heap + 6].Offset;
                heap += 6;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool HasClosedParentTupleSuffix(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int scratch
    )
    {
        int index = start;
        while (index + 1 < end && body[index].Definition.Opcode is (Move or GetTupleElement or PutList or PutTuple or UpdateRecord))
        {
            if (body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.X && operand.Value == scratch))
            {
                return false;
            }

            index++;
        }

        return index + 1 < end && body[index].Definition.Opcode == Deallocate
            && body[index + 1].Definition.Opcode == Return;
    }

    private static bool HasRetainedCheckedParent(
        IReadOnlyList<BeamInstruction> body,
        int[] owners,
        int entry,
        int heap,
        BeamOperand parent,
        BeamOperand field,
        BeamProjectionFlow flow
    )
    {
        int epoch = owners[heap - entry];
        if (epoch < entry || epoch >= heap)
        {
            return false;
        }

        int allocation = epoch;
        int originalSlot = (int)parent.Value;
        if (body[epoch].Definition.Opcode == TrimFrame)
        {
            originalSlot += (int)body[epoch].Operands[0].Value;
            allocation = owners[epoch - entry];
        }

        if (allocation < entry || body[allocation].Definition.Opcode != Allocate)
        {
            return false;
        }

        int save = allocation + 1;
        for (; save < heap; save++)
        {
            BeamInstruction instruction = body[save];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y } slot]
                } && IsBareX(source))
            {
                if (slot.Value != originalSlot)
                {
                    continue;
                }

                if (flow.HasCheckedTupleField(allocation, source, field))
                {
                    break;
                }

                return false;
            }

            if (instruction.Definition.Opcode != InitYRegisters && instruction is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [_, _, { Kind: BeamOperandParameters.Y }]
                })
            {
                return false;
            }
        }

        if (save >= heap)
        {
            return false;
        }

        for (int index = save + 1; index < heap; index++)
        {
            int owner = owners[index - entry];
            int slot = owner == allocation ? originalSlot : (int)parent.Value;
            if ((owner != allocation && owner != epoch) || body[index].Operands.Any(operand =>
                operand.Kind == BeamOperandParameters.Y && operand.Value == slot))
            {
                return false;
            }
        }

        return true;
    }
}
