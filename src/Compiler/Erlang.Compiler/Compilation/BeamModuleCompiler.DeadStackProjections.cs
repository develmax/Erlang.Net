using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveDeadStackProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        bool requiresOrdinaryCall
    ) => RewriteDeadStackProjections(
        code,
        body,
        requiresOrdinaryCall,
        checkedEntryListHeadsOnly: false
    );

    private static byte[]? RemoveDeadCheckedEntryListHeads(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body) =>
        RewriteDeadStackProjections(
            code,
            body,
            requiresOrdinaryCall: true,
            checkedEntryListHeadsOnly: true
        );

    private static byte[]? RewriteDeadStackProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        bool requiresOrdinaryCall,
        bool checkedEntryListHeadsOnly
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            int limit = checkedEntryListHeadsOnly ? FindCheckedEntryListCall(body, entry, end) : end;
            if (limit < 0 || ReadStackProjectionUses(
                body,
                entry,
                end,
                requiresOrdinaryCall
            ) is not { } reads)
            {
                continue;
            }

            for (int index = entry; index < limit; index++)
            {
                BeamInstruction instruction = body[index];
                if (checkedEntryListHeadsOnly && instruction.Definition.Opcode != GetList)
                {
                    continue;
                }

                int result = StackProjectionOutputStart(instruction);
                if (result == instruction.Operands.Count)
                {
                    continue;
                }

                BeamOperand first = instruction.Operands[result];
                BeamOperand? second = instruction.Definition.Opcode == GetList ? instruction.Operands[2] : null;
                int? deadFirst = DeadStackProjectionSlot(first, reads);
                if (checkedEntryListHeadsOnly && (deadFirst is null || !HasCheckedListFailureRelay(
                    body,
                    entry,
                    index,
                    limit
                )))
                {
                    continue;
                }

                int? deadSecond = second is null || checkedEntryListHeadsOnly ? null : DeadStackProjectionSlot(second, reads);
                if (deadFirst is null && deadSecond is null)
                {
                    continue;
                }

                output.Write(code.Span[copied..instruction.Offset]);
                if (second is not null && (deadFirst is null || deadSecond is null))
                {
                    output.WriteByte(deadFirst is null ? GetHead : GetTail);
                    WriteRegister(output, instruction.Operands[0]);
                    WriteRegister(output, deadFirst is null ? first : second);
                }

                // A following collection or a join can still require the dead cell to be initialized.
                int[] cleared = deadFirst is int left && deadSecond is int right && left != right
                    ? [Math.Min(left, right), Math.Max(left, right)] : [deadFirst ?? deadSecond!.Value];
                WriteFrameInitialization(output, cleared);
                copied = body[index + 1].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int FindCheckedEntryListCall(IReadOnlyList<BeamInstruction> body, int entry, int end)
    {
        for (int call = entry; call < end; call++)
        {
            byte opcode = body[call].Definition.Opcode;
            if (opcode is LocalCall or ExternalCall)
            {
                for (int later = call + 1; later < end; later++)
                {
                    if (body[later].Definition.Opcode is GetList or GetHead or GetTail
                        or GetTupleElement or GetMapElements or BinaryMatch)
                    {
                        return -1;
                    }
                }

                return call;
            }

            if (opcode is not (Label or Line or Allocate or InitYRegisters or Move or Jump or BadMatch
                or IsNonemptyList or GetList or GetHead or GetTail))
            {
                return -1;
            }
        }

        return -1;
    }

    private static bool HasCheckedListFailureRelay(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int projection,
        int call
    )
    {
        int check = projection - 1;
        while (check >= entry && body[check].Definition.Opcode == Line)
        {
            check--;
        }

        if (check < entry || body[check] is not { Definition.Opcode: IsNonemptyList, Operands: [var target, var source] }
            || target.Kind != BeamOperandParameters.Label || source != body[projection].Operands[0])
        {
            return false;
        }

        for (int relay = projection + 1; relay < call; relay++)
        {
            if (body[relay] is not { Definition.Opcode: Label, Operands: [var label] } || label.Value != target.Value)
            {
                continue;
            }

            int failure = SkipLines(relay + 1);
            if (failure < call && body[failure] is
                {
                    Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X },
                    { Kind: BeamOperandParameters.X, Value.IsZero: true }]
                })
            {
                failure = SkipLines(failure + 1);

                return failure < call && body[failure] is
                {
                    Definition.Opcode: BadMatch, Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }]
                };
            }

            return failure < call && body[failure] is
            {
                Definition.Opcode: BadMatch, Operands: [{ Kind: BeamOperandParameters.X }]
            };
        }

        return false;

        int SkipLines(int index)
        {
            while (index < call && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }

    private static bool[]? ReadStackProjectionUses(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool requiresOrdinaryCall
    )
    {
        int frame = -1;
        bool projectsToStack = false;
        bool hasOrdinaryCall = false;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is AllocateHeap or TrimFrame or CatchEnter or TryEnter
                || instruction.Definition.Name is "debug_line" or "executable_line")
            {
                return null;
            }

            hasOrdinaryCall |= opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped;
            if (opcode == Allocate)
            {
                if (frame >= 0 || instruction.Operands[0].Value < 1
                    || instruction.Operands[0].Value > BeamOperandParameters.MaximumRegister)
                {
                    return null;
                }

                frame = (int)instruction.Operands[0].Value;
            }

            if (!projectsToStack)
            {
                for (int operand = StackProjectionOutputStart(instruction); operand < instruction.Operands.Count; operand++)
                {
                    projectsToStack |= instruction.Operands[operand].Kind == BeamOperandParameters.Y;
                }
            }
        }

        if (frame < 0 || !projectsToStack || hasOrdinaryCall != requiresOrdinaryCall)
        {
            return null;
        }

        var reads = new bool[frame];
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            int outputStart = StackProjectionOutputStart(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (operand.Value < 0 || operand.Value >= frame)
                {
                    return null;
                }

                // Without a trim there is one physical identity for every Y operand.
                // Conservatively count every mention outside the recognized projection destinations.
                reads[(int)operand.Value] |= position < outputStart;
            }
        }

        return reads;
    }

    private static int StackProjectionOutputStart(BeamInstruction instruction) => instruction switch
    {
        { Definition.Opcode: GetTupleElement, Operands: [var source, _, var result] }
            when IsSwapRegister(source) && IsSwapRegister(result) => 2,
        { Definition.Opcode: GetHead or GetTail, Operands: [var source, var result] }
            when IsSwapRegister(source) && IsSwapRegister(result) => 1,
        { Definition.Opcode: GetList, Operands: [var source, var head, var tail] }
            when IsSwapRegister(source) && IsSwapRegister(head) && IsSwapRegister(tail) => 1,
        _ => instruction.Operands.Count
    };

    private static int? DeadStackProjectionSlot(BeamOperand operand, bool[] reads) =>
        operand.Kind == BeamOperandParameters.Y && !reads[(int)operand.Value] ? (int)operand.Value : null;
}
