using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record SharedLeafFramePlan(int FirstRestore, int Jump, List<SharedLeafOwnerPlan> Owners);

    private sealed record SharedLeafOwnerPlan(
        int Allocation,
        int Limit,
        int?[] Registers,
        Dictionary<int, int>? HeapRoots
    );

    private static SharedLeafFramePlan? PlanSharedLeafFrameReturn(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool afterCanonicalization
    )
    {
        if (!afterCanonicalization)
        {
            return null;
        }

        for (int index = start + 1; index + 3 < end; index++)
        {
            if (body[index].Definition.Opcode == Label && EndsBeamBlock(body[index - 1].Definition.Opcode)
                && ReadSharedLeafFrameReturn(
                    body,
                    start,
                    end,
                    index
                ) is { } plan)
            {
                return plan;
            }
        }

        return null;
    }

    private static SharedLeafFramePlan? ReadSharedLeafFrameReturn(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int relay
    )
    {
        int relayLabel = (int)body[relay].Operands[0].Value;
        var restored = new Dictionary<int, BeamOperand>();
        int release = relay + 1;
        for (; release < end && body[release] is
        {
            Definition.Opcode: Move,
            Operands: [
            { Kind: BeamOperandParameters.Y, TypeIndex: null } slot,
            { Kind: BeamOperandParameters.X, TypeIndex: null } argument
                ]
        }; release++)
        {
            if (!restored.TryAdd((int)argument.Value, slot))
            {
                return null;
            }
        }

        if (restored.Count == 0 || release + 1 >= end
            || body[release] is not
            { Definition.Opcode: Deallocate, Operands: [{ Value.Sign: > 0 } frameSize] }
            || restored.Values.Any(slot => slot.Value >= frameSize.Value)
            || body[release + 1] is not
            { Definition.Opcode: Jump, Operands: [{ Kind: BeamOperandParameters.Label } destination] }
            || destination.Value == relayLabel)
        {
            return null;
        }

        int fallback = start;
        while (fallback < end && !(body[fallback].Definition.Opcode == Label
            && body[fallback].Operands[0].Value == destination.Value))
        {
            fallback++;
        }

        if (fallback >= end || ReadLeafReturnInput(body, fallback + 1, end) is not { } returnInput
            || !restored.TryGetValue((int)returnInput.Value, out var returnSlot)
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                ignoredBranchTarget: relayLabel
            ) is not { } ownership
            || ownership.Owners[relay - start] != UnseenFrameOwner
            || ownership.Owners[fallback - start] != UnframedOwner)
        {
            return null;
        }

        var allocations = new SortedSet<int>();
        for (int index = start; index < end; index++)
        {
            if (!body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label && operand.Value == relayLabel))
            {
                continue;
            }

            int allocation = ownership.Owners[index - start];
            if (allocation < start || allocation >= relay)
            {
                return null;
            }

            allocations.Add(allocation);
        }

        if (allocations.Count < 2)
        {
            return null;
        }

        var plans = new List<SharedLeafOwnerPlan>();
        int previousLimit = start;
        foreach (int allocation in allocations)
        {
            int limit = ReadOwnedLeafFrameTail(
                body,
                start,
                end,
                allocation,
                ownership.Owners,
                sharedExit: relayLabel
            );
            BeamInstruction frame = body[allocation];
            if (allocation < previousLimit || limit < 0 || limit > relay
                || frame.Operands[0].Value != frameSize.Value || returnInput.Value >= frame.Operands[^1].Value
                || !PreservesLeafReturnInput(
                    body,
                    allocation,
                    limit,
                    returnSlot,
                    returnInput
                )
                || PlanLeafFrameRegisters(
                    body,
                    allocation,
                    limit,
                    (int)frame.Operands[^1].Value,
                    afterCanonicalization: true,
                    isArithmetic: null,
                    out var roots,
                    allocationAtStart: true,
                    externalBranchInputs: BigInteger.One << (int)returnInput.Value
                ) is not { } registers)
            {
                return null;
            }

            plans.Add(new(
                allocation,
                limit,
                registers,
                roots
            ));
            previousLimit = limit;
        }

        return new(relay + 1, release + 1, plans);
    }

    private static BeamOperand? ReadLeafReturnInput(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        while (start < end && body[start].Definition.Opcode == Line)
        {
            start++;
        }

        if (start < end && body[start].Definition.Opcode == Return)
        {
            return new(BeamOperandParameters.X, 0);
        }

        return start + 1 < end && body[start] is
        {
            Definition.Opcode: Move,
            Operands: [
            { Kind: BeamOperandParameters.X } input,
            { Kind: BeamOperandParameters.X, Value.IsZero: true }
                ]
        } && body[start + 1].Definition.Opcode == Return ? input : null;
    }

    private static bool PreservesLeafReturnInput(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int limit,
        BeamOperand saved,
        BeamOperand input
    )
    {
        int first = allocation + 1;
        bool hasSavedInput = false;
        for (; first < limit; first++)
        {
            BeamInstruction instruction = body[first];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X } source,
                    { Kind: BeamOperandParameters.Y } target
                    ]
                })
            {
                if (SameProjectionRegister(target, saved))
                {
                    hasSavedInput = SameProjectionRegister(source, input);
                }
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                hasSavedInput &= !instruction.Operands.Skip(1).Any(slot => SameProjectionRegister(slot, saved));
            }
            else
            {
                break;
            }
        }

        if (!hasSavedInput)
        {
            return false;
        }

        for (int index = first; index < limit; index++)
        {
            BeamInstruction instruction = body[index];
            int destination = StackWriteOperand(instruction);
            if (destination >= 0 && ChangesInput(instruction.Operands[destination])
                || instruction.Definition.Opcode == GetList && ChangesInput(instruction.Operands[2])
                || instruction.Definition.Opcode == Swap && instruction.Operands.Any(ChangesInput)
                || instruction.Definition.Opcode == InitYRegisters && instruction.Operands.Skip(1).Any(ChangesInput))
            {
                return false;
            }
        }

        return true;

        bool ChangesInput(BeamOperand operand) => SameProjectionRegister(operand, saved) || SameProjectionRegister(operand, input);
    }
}
