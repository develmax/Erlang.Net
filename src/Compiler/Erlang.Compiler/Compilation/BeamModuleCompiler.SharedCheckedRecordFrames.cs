using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedRecordFrameOrder(
        int SaveStart,
        int SaveEnd,
        int CopyStart,
        int Trim,
        (int Register, int Slot)[] Saves,
        int[] Slots,
        Dictionary<(int Instruction, int Slot), int> ConsSources
    );

    private static byte[]? OrderSharedCheckedRecordCallFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var carrier = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 7 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arguments] }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var badShape, var tuple, var arity, { Kind: BeamOperandParameters.Atom }]
                } || tuple != carrier || !HasUnprotectedCallKillSchedule(body, info + 1))
            {
                continue;
            }

            var fields = new Dictionary<int, int>();
            int selector = info + 3;
            int field = 0;
            while (selector < body.Count && body[selector] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Kind: BeamOperandParameters.Unsigned } member,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                } && source == carrier && member.Value > field && member.Value < arity.Value
                && (destination == carrier || destination.Value >= arguments.Value)
                && fields.TryAdd((int)destination.Value, (int)member.Value))
            {
                field = (int)member.Value;
                selector++;
                if (destination == carrier)
                {
                    break;
                }
            }

            if (!fields.ContainsKey(0) || selector + 1 >= body.Count || body[selector] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } selected,
                    { Kind: BeamOperandParameters.Atom }]
                } || selected == carrier || !fields.ContainsKey((int)selected.Value) || rejected == badShape
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(selector, (int)rejected.Value, out int retry)
                || body.Skip(selector + 1).Take(retry - selector - 1)
                    .Any(instruction => instruction.Definition.Opcode is FunctionInfo or EndCode)
                || !flow.HasStraightCheckedCallPrefix(info + 1, selector + 1)
                || !flow.HasStraightCheckedCallPrefix(retry, retry + 1))
            {
                continue;
            }

            int end = retry + 1;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (ReadCheckedRecordFrameOrder(
                body,
                flow,
                selector + 1,
                retry,
                (int)arguments.Value,
                fields
            ) is { } hot)
            {
                WriteCheckedRecordFrameOrder(hot);
            }

            if (ReadCheckedRecordFrameOrder(
                body,
                flow,
                retry + 1,
                end,
                (int)arguments.Value,
                fields
            ) is { } cold)
            {
                WriteCheckedRecordFrameOrder(cold);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteCheckedRecordFrameOrder(CheckedRecordFrameOrder plan)
        {
            output.Write(code.Span[copied..body[plan.SaveStart].Offset]);
            foreach (var (register, slot) in plan.Saves)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, register);
                BeamCompactWriter.Write(output, YRegisterTag, slot);
            }

            copied = body[plan.SaveEnd].Offset;
            int operand = FirstStackOperandAtOrAfter(stackOperands, copied);
            for (int index = plan.SaveEnd; index < plan.CopyStart; index++)
            {
                int next = body[index + 1].Offset;
                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    output.Write(code.Span[copied..from]);
                    if (plan.ConsSources.TryGetValue((index, slot), out int register))
                    {
                        BeamCompactWriter.Write(output, XRegisterTag, register);
                    }
                    else
                    {
                        BeamCompactWriter.Write(output, YRegisterTag, plan.Slots[slot]);
                    }

                    copied = to;
                }
            }

            output.Write(code.Span[copied..body[plan.CopyStart].Offset]);
            copied = body[plan.Trim].Offset;
        }
    }

    private static CheckedRecordFrameOrder? ReadCheckedRecordFrameOrder(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int allocation,
        int end,
        int arguments,
        Dictionary<int, int> fields
    )
    {
        if (allocation >= end || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap)
            || body[allocation].Operands[0] is not { Kind: BeamOperandParameters.Unsigned } count
            || count.Value < 2 || count.Value >= end - allocation
            || body[allocation].Operands[^1] is not { Kind: BeamOperandParameters.Unsigned } live)
        {
            return null;
        }

        int width = (int)count.Value;
        int saveStart = allocation + 1;
        int saveEnd = saveStart + width;
        var saves = new Dictionary<int, int>();
        var available = new HashSet<int>();
        for (int index = saveStart; index < saveEnd; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || saved.Value >= width || input.Value >= live.Value
                || input.Value >= arguments && !fields.ContainsKey((int)input.Value)
                || !saves.TryAdd((int)saved.Value, (int)input.Value)
                || !available.Add((int)input.Value))
            {
                return null;
            }
        }

        if (saves.Values.Count(register => !fields.ContainsKey(register)) != 1)
        {
            return null;
        }

        // get_match creates the tuple's identities with new_vars. That list
        // has descending identities in increasing field order. reserve_yregs
        // copies in identity order and turn_yregs reverses the physical slots.
        var slots = new int[width];
        var ordered = saves.OrderBy(binding => fields.ContainsKey(binding.Value) ? 0 : 1)
            .ThenBy(binding => fields.GetValueOrDefault(binding.Value))
            .Select((binding, index) =>
            {
                slots[binding.Key] = index;

                return (Register: binding.Value, Slot: index);
            }).ToArray();

        bool called = false;
        var cons = new Dictionary<(int Instruction, int Slot), int>();
        var owners = Enumerable.Range(0, width).ToArray();
        int copyStart = saveEnd;
        for (int index = saveEnd; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is
                {
                    Definition.Opcode: TrimFrame,
                    Operands: [var removed, var retained]
                })
            {
                if (!called || removed.Value < 1 || retained.Value < 1 || removed.Value + retained.Value != width
                    || !flow.HasStraightCheckedCallPrefix(allocation, index))
                {
                    return null;
                }

                for (int slot = (int)removed.Value; slot < width; slot++)
                {
                    if (slots[owners[slot]] != slot)
                    {
                        return null;
                    }
                }

                if (copyStart == index && cons.Count == 0 && !slots.Where((slot, old) => slot != old).Any())
                {
                    return null;
                }

                return new CheckedRecordFrameOrder(
                    saveStart,
                    saveEnd,
                    copyStart,
                    index,
                    ordered,
                    slots,
                    cons
                );
            }

            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } from,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } to]
                })
            {
                if (!called || from.Value >= width || to.Value >= width)
                {
                    return null;
                }

                owners[(int)to.Value] = owners[(int)from.Value];
                continue;
            }

            if (copyStart != index || instruction.Operands.Any(operand =>
                operand.Kind == BeamOperandParameters.Y && operand.Value >= width))
            {
                return null;
            }

            if (instruction.Definition.Opcode is LocalCall or ExternalCall)
            {
                called = true;
                available.Clear();
            }
            else if (instruction.Definition.Opcode is Move or PutList or PutTuple)
            {
                int write = StackWriteOperand(instruction);
                if (write < 0 || instruction.Operands[write] is not { Kind: BeamOperandParameters.X, TypeIndex: null } target)
                {
                    return null;
                }

                if (!called && instruction.Definition.Opcode == PutList)
                {
                    foreach (var operand in instruction.Operands.Take(write))
                    {
                        if (operand is { Kind: BeamOperandParameters.Y, TypeIndex: null }
                            && saves.TryGetValue((int)operand.Value, out int register) && available.Contains(register))
                        {
                            cons.TryAdd((index, (int)operand.Value), register);
                        }
                    }
                }

                available.Remove((int)target.Value);
            }
            else if (instruction.Definition.Opcode is not (Line or TestHeap))
            {
                return null;
            }
            else if (instruction.Definition.Opcode == TestHeap)
            {
                // Without an independent collector-live proof keep the Y read.
                available.Clear();
            }

            copyStart++;
        }

        return null;
    }
}
