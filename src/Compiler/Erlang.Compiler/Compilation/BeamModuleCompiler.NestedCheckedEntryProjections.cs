using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Allocation, EntryFramePlacement Placement)? PlanNestedCheckedEntryProjections(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (start < 1 || start + 4 >= end || body[start].Definition.Opcode != Label
            || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var incoming] }
            || incoming.Value <= 0 || incoming.Value >= FirstReservedXRegister)
        {
            return null;
        }

        int arity = (int)incoming.Value;
        int failure = EntryFrameFailureLabel(body, start);
        int check = start + 1;
        if (failure < 0 || body[check] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label } outsideFailure,
                { Kind: BeamOperandParameters.X } input,
                { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 } tupleArity,
                { Kind: BeamOperandParameters.Atom }
                ]
            } || outsideFailure.Value != failure || input.Value >= arity)
        {
            return null;
        }

        int allocation = check + 1;
        if (body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.Sign: > 0 } width, var live]
            } || live.Value != arity || width.Value > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        var savedInputs = new Dictionary<int, int>();
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
                } && argument.Value < arity && slot.Value < width.Value)
            {
                savedInputs[(int)slot.Value] = (int)argument.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (BeamOperand initialized in instruction.Operands.Skip(1))
                {
                    savedInputs.Remove((int)initialized.Value);
                }
            }
            else
            {
                break;
            }
        }

        if (first >= end || body[first].Definition.Opcode != GetTupleElement)
        {
            return null;
        }

        var bindings = new Dictionary<int, int>(savedInputs);
        var tupleRegisters = new HashSet<int> { (int)input.Value };
        var tupleArities = new Dictionary<int, System.Numerics.BigInteger> { [(int)input.Value] = tupleArity.Value };
        var rewrites = new Dictionary<int, EntryFrameOperandRewrite>();
        var guards = new HashSet<int>();
        int nextRegister = arity;
        int relayLabel = -1;
        int frame = -1;
        for (int index = first; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                        var source, { Kind: BeamOperandParameters.Unsigned } field,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } destination
                    ]
                } && destination.Value < width.Value && nextRegister < FirstReservedXRegister
                && ReadRegister(source) is int tuple && tupleArities.TryGetValue(tuple, out var checkedArity)
                && field.Value >= 0 && field.Value < checkedArity)
            {
                // Read the source's old definition before updating the destination binding.
                rewrites.Add(index, new(source.Kind == BeamOperandParameters.Y ? [tuple, nextRegister] : [nextRegister]));
                bindings[(int)destination.Value] = nextRegister++;
                continue;
            }

            if (!IsNestedEntryPredicate(instruction.Definition.Opcode)
                || instruction.Operands[0] is not { Kind: BeamOperandParameters.Label } target
                || relayLabel >= 0 && target.Value != relayLabel)
            {
                break;
            }

            var registers = new List<int>();
            bool readsProjection = false;
            bool known = true;
            foreach (BeamOperand value in instruction.Operands.Skip(1))
            {
                if (value.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                {
                    continue;
                }

                if (ReadRegister(value) is not int register)
                {
                    known = false;
                    break;
                }

                if (value.Kind == BeamOperandParameters.Y)
                {
                    registers.Add(register);
                }

                readsProjection |= register >= arity;
            }

            if (!known || !readsProjection || !RefineTuple(instruction))
            {
                break;
            }

            relayLabel = (int)target.Value;
            guards.Add(index);
            rewrites.Add(index, new(registers.ToArray()));
            frame = index + 1;
        }

        if (frame < 0 || frame >= end || body[frame].Definition.Opcode is Label or Allocate or AllocateHeap
            || body.Skip(start).Take(end - start).Any(instruction =>
                instruction.Definition.Opcode == DebugLine || instruction.Definition.Name == "executable_line")
            || ReadDisjointFrameOwnership(body, start, end) is not { } ownership
            || ownership.Uses.Count != 1 || ownership.Owners[frame - start] != allocation)
        {
            return null;
        }

        var stores = new Dictionary<int, int>();
        var delayed = new HashSet<int>();
        for (int index = first; index < frame; index++)
        {
            if (ownership.Owners[index - start] != allocation)
            {
                return null;
            }

            delayed.Add(index);
            if (body[index].Definition.Opcode == GetTupleElement)
            {
                stores[(int)body[index].Operands[^1].Value] = rewrites[index].Registers[^1];
            }
        }

        var omitted = new HashSet<int>();
        if (relayLabel != failure)
        {
            int relay = frame;
            while (relay < end && !(body[relay].Definition.Opcode == Label
                && body[relay].Operands[0].Value == relayLabel))
            {
                relay++;
            }

            if (relay >= end || !EndsBeamBlock(body[relay - 1].Definition.Opcode)
                || ownership.Owners[relay - start] != allocation)
            {
                return null;
            }

            int release = relay + 1;
            while (release < end && body[release] is
                { Definition.Opcode: Move, Operands: [var from, var to] })
            {
                if (from.Kind != BeamOperandParameters.Y || to.Kind != BeamOperandParameters.X
                    || stores.ContainsKey((int)from.Value)
                    || !savedInputs.TryGetValue((int)from.Value, out int original) || original != to.Value)
                {
                    return null;
                }

                omitted.Add(release++);
            }

            if (release + 1 >= end || body[release] is not
                { Definition.Opcode: Deallocate, Operands: [var oldWidth] } || oldWidth != width
                || body[release + 1] is not
                { Definition.Opcode: Jump, Operands: [{ Kind: BeamOperandParameters.Label } parent] }
                || parent.Value != failure)
            {
                return null;
            }

            bool shared = Enumerable.Range(start, end - start).Any(index => !guards.Contains(index)
                && body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                    && operand.Value == relayLabel));
            if (shared)
            {
                // Framed edges still need the old cleanup. Only the lifted tests can bypass it.
                omitted.Clear();
                foreach (int guard in guards)
                {
                    rewrites[guard] = rewrites[guard] with { FailureLabel = failure };
                }
            }
            else
            {
                omitted.Add(release);
            }
        }

        int frameLive = stores.Values.Max() + 1;

        return (allocation, new(
            first,
            new Dictionary<int, int>(),
            delayed,
            new Dictionary<int, int> { [frame] = frameLive },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]>
            {
                [frame] = [.. stores.OrderBy(store => store.Key).Select(store => (store.Key, store.Value))]
            },
            rewrites,
            omitted
        ));

        int? ReadRegister(BeamOperand operand) => operand.Kind switch
        {
            BeamOperandParameters.X when operand.Value < arity => (int)operand.Value,
            BeamOperandParameters.Y when bindings.TryGetValue((int)operand.Value, out int register) => register,
            _ => null
        };

        bool RefineTuple(BeamInstruction instruction)
        {
            if (instruction.Definition.Opcode is not (IsTuple or TestArity or IsTaggedTuple))
            {
                return true;
            }

            if (ReadRegister(instruction.Operands[1]) is not int register)
            {
                return false;
            }

            if (instruction.Definition.Opcode == IsTuple)
            {
                tupleRegisters.Add(register);

                return true;
            }

            if (instruction.Definition.Opcode == TestArity && !tupleRegisters.Contains(register))
            {
                return false;
            }

            tupleRegisters.Add(register);
            tupleArities[register] = instruction.Operands[2].Value;

            return true;
        }
    }

    private static bool IsNestedEntryPredicate(byte opcode) => opcode is
        IsTaggedTuple or IsTuple or TestArity or IsMap or IsNil or IsNonemptyList or ExactEqualTest or ExactNotEqualTest;
}
