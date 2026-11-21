using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Allocation, EntryFramePlacement Placement)? PlanCheckedEntryProjectionAndTests(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (start < 1 || start + 4 >= end
            || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
            || body[start].Definition.Opcode != Label)
        {
            return null;
        }

        int check = SkipLines(start + 1);
        if (check + 3 >= end || body[check] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [{ Kind: BeamOperandParameters.Label }, { Kind: BeamOperandParameters.X } input,
                { Kind: BeamOperandParameters.Unsigned } tupleArity, _]
            })
        {
            return null;
        }

        int allocation = SkipLines(check + 1);
        if (allocation + 2 >= end || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.Sign: > 0 } width, var existingLive]
            }
            || existingLive.Value != arity.Value || input.Value >= existingLive.Value
            || existingLive.Value >= BeamOperandParameters.MaximumRegister - 1)
        {
            return null;
        }

        var saved = new Dictionary<int, int>();
        int projection = allocation + 1;
        for (; projection < end; projection++)
        {
            BeamInstruction instruction = body[projection];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } source, { Kind: BeamOperandParameters.Y } target]
                }
                && source.Value < existingLive.Value && target.Value < width.Value)
            {
                saved[(int)target.Value] = (int)source.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (BeamOperand slot in instruction.Operands.Skip(1))
                {
                    saved.Remove((int)slot.Value);
                }
            }
            else
            {
                break;
            }
        }

        if (projection + 2 >= end || body[projection] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var projectedSource, { Kind: BeamOperandParameters.Unsigned } field,
                { Kind: BeamOperandParameters.Y } destination]
            }
            || destination.Value >= width.Value || field.Value < 0 || field.Value >= tupleArity.Value
            || !ReadsCheckedTuple(projectedSource)
            || projectedSource.Kind == BeamOperandParameters.Y && SameProjectionRegister(projectedSource, destination))
        {
            return null;
        }

        var tested = new HashSet<int>();
        var delayed = new HashSet<int> { projection };
        int register = (int)existingLive.Value;
        saved[(int)destination.Value] = register;
        int consumer = SkipLines(projection + 1);
        if (consumer >= end || !IsEntryFramePredicate(body[consumer].Definition.Opcode)
            || body[consumer].Operands[0] is not { Kind: BeamOperandParameters.Label } rejected
            || !body[consumer].Operands.Skip(1).Any(operand => SameProjectionRegister(operand, destination)))
        {
            return null;
        }

        int frame = consumer;
        while (frame < end)
        {
            BeamInstruction instruction = body[frame];
            if (instruction.Definition.Opcode == Line)
            {
                frame++;
                continue;
            }

            if (!IsEntryFramePredicate(instruction.Definition.Opcode)
                || instruction.Operands[0] != rejected
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && !saved.ContainsKey((int)operand.Value)
                    || operand.Kind == BeamOperandParameters.X && operand.Value >= existingLive.Value))
            {
                break;
            }

            delayed.Add(frame);
            tested.Add(frame);
            frame++;
        }

        if (tested.Count == 0 || frame >= end || body[frame].Definition.Opcode is Label or Allocate or AllocateHeap)
        {
            return null;
        }

        // Keep the source line for the first framed operation with that operation.
        while (frame > consumer + 1 && body[frame - 1].Definition.Opcode == Line)
        {
            frame--;
        }

        int functionFailure = EntryFrameFailureLabel(body, start);
        if (functionFailure < 0 || ReadDisjointFrameOwnership(body, start, end) is not { } ownership
            || ownership.Owners[projection - start] != allocation
            || ownership.Owners[frame - start] != allocation)
        {
            return null;
        }

        if (rejected.Value != functionFailure)
        {
            int relay = frame + 1;
            while (relay < end && !(body[relay].Definition.Opcode == Label
                && body[relay].Operands[0].Value == rejected.Value))
            {
                relay++;
            }

            int deallocation = SkipLines(relay + 1);
            if (relay >= end || deallocation + 1 >= end || !EndsBeamBlock(body[relay - 1].Definition.Opcode)
                || body[deallocation] is not { Definition.Opcode: Deallocate, Operands: [var oldWidth] }
                || oldWidth != width || body[deallocation + 1] is not
                {
                    Definition.Opcode: Jump,
                    Operands: [{ Kind: BeamOperandParameters.Label } originalFailure]
                }
                || originalFailure.Value != functionFailure
                || ownership.Owners[deallocation - start] != allocation
                || Enumerable.Range(start, end - start).Any(index => !tested.Contains(index)
                    && body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && operand.Value == rejected.Value)))
            {
                return null;
            }

            // A failed refinement no longer reaches or needs the old deallocation.
            delayed.Add(deallocation);
        }

        return (allocation, new(
            projection,
            saved,
            delayed,
            new Dictionary<int, int> { [frame] = register + 1 },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]> { [frame] = [((int)destination.Value, register)] }
        ));

        int SkipLines(int index)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool ReadsCheckedTuple(BeamOperand source) => SameProjectionRegister(source, input)
            || source.Kind == BeamOperandParameters.Y
                && saved.TryGetValue((int)source.Value, out int argument) && argument == input.Value;
    }
}
