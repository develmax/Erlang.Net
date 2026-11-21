using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Allocation, EntryFramePlacement Placement)? PlanCheckedRecordSpillEntry(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (start < 1 || start + 6 >= end || body[start].Definition.Opcode != Label
            || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] })
        {
            return null;
        }

        int outer = start + 1;
        int allocation = outer + 1;
        if (body[outer] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label } outerFailure,
                { Kind: BeamOperandParameters.X } input,
                { Kind: BeamOperandParameters.Unsigned } tupleArity,
                { Kind: BeamOperandParameters.Atom }
                ]
            } || input.Value >= arity.Value || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.Sign: > 0 } width, var live]
            } || live.Value != arity.Value || width.Value > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        var copiedInputs = new HashSet<int>();
        int first = allocation + 1;
        for (; first < end; first++)
        {
            BeamInstruction instruction = body[first];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X } argument,
                    { Kind: BeamOperandParameters.Y } slot
                    ]
                } && argument.Value < live.Value && slot.Value < width.Value)
            {
                copiedInputs.Add((int)argument.Value);
            }
            else if (instruction.Definition.Opcode != InitYRegisters)
            {
                break;
            }
        }

        int spill = first + 1;
        int inner = spill + 1;
        int frame = inner + 1;
        if (frame >= end || body[first] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [
                    var source, { Kind: BeamOperandParameters.Unsigned } field,
                { Kind: BeamOperandParameters.X, TypeIndex: null } projected
                ]
            } || !SameProjectionRegister(source, input) || field.Value < 0 || field.Value >= tupleArity.Value
            || projected.Value >= live.Value
            || body[spill] is not
            {
                Definition.Opcode: Move,
                Operands: [var savedValue, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedSlot]
            } || !SameProjectionRegister(savedValue, projected) || savedSlot.Value >= width.Value
            || body[inner] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [{ Kind: BeamOperandParameters.Label } innerFailure, var innerInput, _, _]
            } || !SameProjectionRegister(innerInput, savedSlot) || outerFailure.Value == innerFailure.Value
            || body[frame].Definition.Opcode is Label or Allocate or AllocateHeap
            || body.Skip(start).Take(end - start).Any(instruction =>
                instruction.Definition.Opcode == DebugLine || instruction.Definition.Name == "executable_line"))
        {
            return null;
        }

        int outsideError = ReadRejection(outer, outerFailure, input);
        int insideError = ReadRejection(inner, innerFailure, savedSlot);
        if (outsideError < 0 || insideError < frame || ReadDisjointFrameOwnership(body, start, end) is not { } ownership
            || ownership.Uses.Count != 1 || ownership.Owners[outsideError - start] != UnframedOwner
            || ownership.Owners[insideError - start] != allocation || ownership.Owners[frame - start] != allocation)
        {
            return null;
        }

        int slotIndex = (int)savedSlot.Value;
        int register = (int)projected.Value;
        var delayed = new HashSet<int> { inner, insideError };
        Dictionary<int, EntryFrameOperandRewrite>? rewrites = null;
        if (copiedInputs.Contains(register))
        {
            // If the setup needs the old destination, use the checked input only
            // when the continuation never reads either of the replaced X values.
            var flow = new BeamProjectionFlow(body);
            if (copiedInputs.Contains((int)input.Value)
                || !flow.IsEntryTemporaryDead(frame, input) || !flow.IsEntryTemporaryDead(frame, projected))
            {
                return null;
            }

            register = (int)input.Value;
            delayed.Add(first);
            rewrites = new()
            {
                [first] = new([], TupleProjectionDestination: register)
            };
        }

        return (allocation, new(
            first,
            new Dictionary<int, int> { [slotIndex] = register },
            delayed,
            new Dictionary<int, int> { [frame] = (int)live.Value },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]> { [frame] = [(slotIndex, register)] },
            OperandRewrites: rewrites,
            Omitted: new HashSet<int> { spill }
        ));

        int ReadRejection(int check, BeamOperand failed, BeamOperand value)
        {
            int label = -1;
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Label && instruction.Operands[0].Value == failed.Value)
                {
                    label = index;
                }

                if (index != check && instruction.Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Label && operand.Value == failed.Value))
                {
                    return -1;
                }
            }

            if (label <= frame || !EndsBeamBlock(body[label - 1].Definition.Opcode))
            {
                return -1;
            }

            int error = label + 1;
            if (error < end && body[error].Definition.Opcode == Line)
            {
                error++;
            }

            // The same source location and checked record value reach the terminal error.
            return error < end && body[error] is { Definition.Opcode: BadRecord, Operands: [var original] }
                && SameProjectionRegister(original, value) ? error : -1;
        }
    }
}
