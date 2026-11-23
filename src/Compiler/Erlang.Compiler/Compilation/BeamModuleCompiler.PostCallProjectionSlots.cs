using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? PromotePostCallProjectionSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int operand = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadPostCallSlice(body, start, end) is not { } plan)
            {
                continue;
            }

            var available = new Dictionary<int, bool>();
            if (PlanLeafFrameRegisters(
                body,
                plan.Start,
                plan.Tail + 1,
                1,
                afterCanonicalization: true,
                isArithmetic: null,
                out var heapRoots,
                isolatedTailBranch: true,
                postCallSlots: plan.Slots,
                postCallFrameSize: plan.FrameSize,
                canUsePostCallRegister: AvailableOnFailure
            ) is not { } registers)
            {
                continue;
            }

            bool AvailableOnFailure(int register)
            {
                if (!available.TryGetValue(register, out bool safe))
                {
                    // Reject a candidate register before assigning it, so another
                    // register remains available when a failed guard needs the result.
                    var operand = new BeamOperand(BeamOperandParameters.X, register);
                    safe = plan.Guards.All(guard => (flow ??= new BeamProjectionFlow(body))
                        .IsGuardTemporaryDeadOnFailure(
                            guard,
                            operand,
                            preserveHeapRoots: true
                        ));
                    available.Add(register, safe);
                }

                return safe;
            }

            int[] remap = new int[plan.FrameSize];
            int frameSize = 0;
            for (int slot = 0; slot < remap.Length; slot++)
            {
                remap[slot] = registers[slot].HasValue ? -1 : frameSize++;
            }

            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    int[] cleared = instruction.Operands.Skip(1)
                        .Select(slot => remap[(int)slot.Value]).Where(slot => slot >= 0).ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (cleared.Length != 0)
                    {
                        WriteFrameInitialization(output, cleared);
                    }

                    copied = next;
                    continue;
                }

                int widthOperand = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (widthOperand >= 0)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        instruction.Offset,
                        copied,
                        widthOperand,
                        frameSize
                    );
                }
                else if (heapRoots?.TryGetValue(index, out int live) == true)
                {
                    // The first operand can encode an allocation list.
                    int rootPosition = next - CompactRegisterCountWidth((int)instruction.Operands[^1].Value);
                    output.Write(code.Span[copied..rootPosition]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
                    copied = next;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    output.Write(code.Span[copied..from]);
                    if (registers[slot] is int register)
                    {
                        BeamCompactWriter.Write(output, XRegisterTag, register);
                    }
                    else
                    {
                        BeamCompactWriter.Write(output, YRegisterTag, remap[slot]);
                    }

                    copied = to;
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

    private static int WritePostCallProjectionOperand(
        Stream output,
        ReadOnlyMemory<byte> code,
        int instruction,
        int copied,
        int operand,
        int value
    )
    {
        int start = instruction + 1;
        var cursor = new BeamCursor(code[start..]);
        for (int position = 0; position < operand; position++)
        {
            _ = BeamCompactReader.Read(cursor);
        }

        output.Write(code.Span[copied..(start + cursor.Position)]);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);
        _ = BeamCompactReader.Read(cursor);

        return start + cursor.Position;
    }

    private static PostCallProjectionSlice? ReadPostCallSlice(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        int allocation = start;
        while (allocation < end && body[allocation].Definition.Opcode is Label or Line)
        {
            allocation++;
        }

        if (allocation >= end || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap))
        {
            return null;
        }

        int frameSize = (int)body[allocation].Operands[0].Value;
        int call = -1;
        for (int index = allocation + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            int width = LinearFrameSizeOperand(opcode);
            if ((opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter or TailDynamicApply
                or CallFun or CallFunTyped or DynamicApply or DebugLine)
                || instruction.Definition.Name == "executable_line"
                || (width >= 0 && instruction.Operands[width].Value != frameSize)
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y && operand.Value >= frameSize))
            {
                return null;
            }

            if (opcode is LocalCall or ExternalCall)
            {
                if (call >= 0)
                {
                    return null;
                }

                call = index;
            }
        }

        if (call < 0 || frameSize <= 1)
        {
            return null;
        }

        int tail = call + 1;
        int failure = -1;
        bool heapSeen = false;
        var guards = new List<int>();
        var projected = new HashSet<int>();
        var writtenSlots = new HashSet<int>();
        for (; tail < end; tail++)
        {
            BeamInstruction instruction = body[tail];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is TailLocalCallWithFrame or TailExternalCallWithFrame)
            {
                break;
            }

            if (opcode == Deallocate && tail + 1 < end && body[tail + 1].Definition.Opcode == Return)
            {
                tail++;
                break;
            }

            if (opcode is IsTuple or IsTaggedTuple or TestArity or IsNonemptyList or IsNil)
            {
                if (heapSeen || instruction.Operands[0] is not { Kind: BeamOperandParameters.Label } target
                    || (failure >= 0 && failure != (int)target.Value))
                {
                    return null;
                }

                failure = (int)target.Value;
                guards.Add(tail);
                continue;
            }

            if (opcode is not (Move or Line or TestHeap or PutTuple or PutList or GetTupleElement
                or GetHead or GetTail or GetList) || (heapSeen && opcode == TestHeap))
            {
                return null;
            }

            heapSeen |= opcode == TestHeap;
            int destination = StackWriteOperand(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if ((position == destination || opcode == GetList && position == 2)
                    && instruction.Operands[position] is { Kind: BeamOperandParameters.Y } operand)
                {
                    int slot = (int)operand.Value;
                    writtenSlots.Add(slot);
                    if (opcode is GetTupleElement or GetHead or GetTail or GetList)
                    {
                        projected.Add(slot);
                    }
                }
            }
        }

        if (tail >= end || guards.Count == 0 || projected.Count == 0 || !writtenSlots.SetEquals(projected)
            || !body.Skip(tail + 1).Take(end - tail - 1)
                .Any(instruction => instruction.Definition.Opcode == Label && instruction.Operands[0].Value == failure))
        {
            return null;
        }

        // A replaced slot is scratch only in the single success slice. The fallback,
        // the entry prefix and any alternate exit can still use the untouched slots.
        for (int index = start; index < end; index++)
        {
            if (index > call && index < tail || body[index].Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            if (body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                && projected.Contains((int)operand.Value)))
            {
                return null;
            }
        }

        return new PostCallProjectionSlice(
            call + 1,
            tail,
            frameSize,
            projected,
            guards
        );
    }

    private sealed record PostCallProjectionSlice(
        int Start,
        int Tail,
        int FrameSize,
        HashSet<int> Slots,
        List<int> Guards
    );
}
