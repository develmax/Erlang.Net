using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleNestedRecordBranchCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int stackOperand = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            info = end - 1;
            if (!Enumerable.Range(entry, end - entry).Any(index => body[index].Definition.Opcode == UpdateRecord)
                || ReadDisjointFrameOwnership(body, entry, end) is not { } frames || frames.Uses.Count < 2)
            {
                continue;
            }

            var aliases = new Dictionary<int, (int Outer, int Inner, int First)>();
            foreach (int allocation in frames.Uses.Keys)
            {
                if (ReadNestedRecordAliases(
                    body,
                    allocation,
                    entry,
                    end,
                    frames.Owners
                ) is { } plan)
                {
                    aliases.Add(allocation, plan);
                }
            }

            for (int index = entry; index < end; index++)
            {
                if (!aliases.TryGetValue(frames.Owners[index - entry], out var plan))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                if (index == plan.First || index == plan.First + 1)
                {
                    int source = index == plan.First ? plan.First + 1 : plan.First;
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, XRegisterTag, body[source].Operands[0].Value);
                    BeamCompactWriter.Write(output, YRegisterTag, instruction.Operands[1].Value);
                    copied = next;
                    continue;
                }

                while (stackOperand < operands.Count && operands[stackOperand].Start < instruction.Offset)
                {
                    stackOperand++;
                }

                while (stackOperand < operands.Count && operands[stackOperand].Start < next)
                {
                    var (from, to, slot) = operands[stackOperand++];
                    if (slot == plan.Outer || slot == plan.Inner)
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, slot == plan.Outer ? plan.Inner : plan.Outer);
                        copied = to;
                    }
                }

                int constants = CountProjectedCallConstants(
                    body,
                    index,
                    end,
                    plan.Outer
                );
                if (constants > 1)
                {
                    // setup_args omits the identity copy of the projection in X0.
                    // Only the remaining independent copies enter order_moves.
                    output.Write(code.Span[copied..next]);
                    foreach (int move in IndependentCallMoveOrder(constants))
                    {
                        int position = index + 1 + move;
                        output.Write(code.Span[body[position].Offset..body[position + 1].Offset]);
                    }

                    copied = body[index + 1 + constants].Offset;
                    index += constants;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Outer, int Inner, int First)? ReadNestedRecordAliases(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int entry,
        int end,
        int[] owners
    )
    {
        if (allocation < entry + 3 || allocation + 3 >= end
            || body[allocation - 3] is { Definition.Opcode: not IsTaggedTuple }
            || body[allocation - 2].Definition.Opcode != GetTupleElement
            || body[allocation - 1] is { Definition.Opcode: not IsTaggedTuple })
        {
            return null;
        }

        var outerTest = body[allocation - 3].Operands;
        var project = body[allocation - 2].Operands;
        var innerTest = body[allocation - 1].Operands;
        if (outerTest[0] != innerTest[0] || outerTest[1] != project[0] || innerTest[1] != project[2]
            || project[0] == project[2])
        {
            return null;
        }

        int first = allocation + 1;
        if (body[first].Definition.Opcode == InitYRegisters)
        {
            first++;
        }

        int high = (int)body[allocation].Operands[0].Value - 1;
        var outer = new BeamOperand(BeamOperandParameters.Y, high - 1);
        var inner = new BeamOperand(BeamOperandParameters.Y, high);
        if (high < 1 || body[first] is not { Definition.Opcode: Move, Operands: [var root, var rootSlot] }
            || body[first + 1] is not { Definition.Opcode: Move, Operands: [var nested, var nestedSlot] }
            || root != project[0] || nested != project[2] || rootSlot != outer || nestedSlot != inner)
        {
            return null;
        }

        bool returned = false;
        for (int index = first + 2; index < end; index++)
        {
            if (owners[index - entry] != allocation)
            {
                continue;
            }

            var operation = body[index];
            if (operation.Definition.Opcode == InitYRegisters
                && operation.Operands.Any(operand => operand == outer || operand == inner))
            {
                return null;
            }

            returned |= index + 4 < end && operation.Definition.Opcode == UpdateRecord
                && SameProjectionRegister(operation.Operands[2], inner) && IsBareX0(operation.Operands[3])
                && body[index + 1] is { Definition.Opcode: UpdateRecord } replace
                && SameProjectionRegister(replace.Operands[2], outer)
                && replace.Operands.Skip(FirstRecordUpdateValueOperand)
                    .Where((_, position) => position % 2 == 0).Any(IsX0)
                && body[index + 2].Definition.Opcode == PutTuple
                && body[index + 3].Definition.Opcode == Deallocate
                && body[index + 4].Definition.Opcode == Return;
        }

        // Pre-codegen's frame copy for the earlier outer argument gets the
        // higher physical Y. The independent copies are then emitted low first.
        return returned ? (high - 1, high, first) : null;

        static bool IsX0(BeamOperand value) =>
            value is { Kind: BeamOperandParameters.X, Value.IsZero: true };
    }

    private static int CountProjectedCallConstants(
        IReadOnlyList<BeamInstruction> body,
        int index,
        int end,
        int outer
    )
    {
        if (body[index] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.Y } source, _, var destination]
            } || source.Value != outer || !IsBareX0(destination))
        {
            return 0;
        }

        int next = index + 1;
        for (; next < end && body[next] is
        {
            Definition.Opcode: Move,
            Operands:
                [
                    var literal,
                { Kind: BeamOperandParameters.X, TypeIndex: null } argument
                ]
        } && argument.Value == next - index && IsNonRegisterValue(literal); next++)
        {
        }

        int count = next - index - 1;
        if (body[next].Definition.Opcode == Line)
        {
            next++;
        }

        return next < end && body[next] is
        { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            && arity.Value == count + 1 ? count : 0;
    }
}
