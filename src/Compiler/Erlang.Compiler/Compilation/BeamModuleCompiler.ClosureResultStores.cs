using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? StoreConstructionResultsDirectly(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction construction = body[index];
            int destination = construction.Definition.Opcode switch
            {
                MakeFun => 1,
                PutTuple => 0,
                PutList when IsPostCallListBeforeOverwrite(body, index) => 2,
                _ => -1
            };
            if (destination < 0
                || construction.Operands[destination] is not { Kind: BeamOperandParameters.X, TypeIndex: null } result)
            {
                continue;
            }

            int store = index + 1;
            if (body[store] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                || source != result || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || construction.Operands.Skip(destination + 1).Any(operand => SameProjectionRegister(operand, slot))
                || construction.Definition.Opcode == PutTuple && !IsPostCallTupleBeforeOverwrite(body, index)
                    && !IsEntryTupleSavedAcrossCall(body, index, slot)
                || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(store, result))
            {
                continue;
            }

            int operands = construction.Offset + 1;
            var cursor = new BeamCursor(code[operands..]);
            for (int operand = 0; operand < destination; operand++)
            {
                _ = BeamCompactReader.Read(cursor);
            }
            output.Write(code.Span[copied..(operands + cursor.Position)]);
            WriteRegister(output, slot);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[(operands + cursor.Position)..body[store].Offset]);
            copied = body[store + 1].Offset;
            index = store;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsPostCallListBeforeOverwrite(IReadOnlyList<BeamInstruction> body, int cons)
    {
        if (cons < 3 || body[cons].Operands is not
            [
            { Kind: BeamOperandParameters.Y, TypeIndex: null },
            { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } accumulator,
                var result
            ]
            || accumulator != result)
        {
            return false;
        }

        int start = cons - 1;
        while (start > 1 && body[start] is
            {
                Definition.Opcode: PutList,
                Operands: [_, var provenTail, var destination]
            }
            && destination == result)
        {
            start--;
        }

        return start < cons - 1
            && body[start] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
            && body[start - 1].Definition.Opcode is LocalCall or ExternalCall;
    }

    private static bool IsPostCallTupleBeforeOverwrite(IReadOnlyList<BeamInstruction> body, int tuple)
    {
        // Immediately after an ordinary call, X0 is the only call-result root.
        // Keep branch-join transport copies and other construction schedules.
        return tuple >= 2
            && body[tuple - 2].Definition.Opcode is LocalCall or ExternalCall
            && body[tuple - 1] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
            && body[tuple].Operands[0] is { Kind: BeamOperandParameters.X, Value.IsZero: true } result
            && body[tuple].Operands.Skip(2).Any(operand => SameProjectionRegister(operand, result))
            && OverwritesConstructionTemporary(body[tuple + 2], result);
    }

    private static bool IsEntryTupleSavedAcrossCall(IReadOnlyList<BeamInstruction> body, int tuple, BeamOperand slot)
    {
        if (body[tuple].Operands[0] is not { Kind: BeamOperandParameters.X } result
            || !OverwritesConstructionTemporary(body[tuple + 2], result))
        {
            return false;
        }

        // The allocation reserves the heap. These entry definitions have
        // neither an alternate predecessor nor an intervening collection.
        for (int index = tuple - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == AllocateHeap)
            {
                return HasFirstReadAfterOrdinaryCall(body, tuple + 2, slot);
            }

            if (instruction.Definition.Opcode != InitYRegisters
                && instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
                })
            {
                break;
            }
        }

        return false;
    }

    private static bool HasFirstReadAfterOrdinaryCall(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand slot
    )
    {
        // OTP reserves Y for values needed after an ordinary call clobbers X.
        // A construction used by the outgoing send or call still belongs in X.
        bool crossedCall = false;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is LocalCall or ExternalCall)
            {
                crossedCall = true;
                continue;
            }

            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] })
            {
                if (SameProjectionRegister(source, slot))
                {
                    return crossedCall && destination.Kind == BeamOperandParameters.X;
                }

                if (!SameProjectionRegister(destination, slot))
                {
                    continue;
                }
            }

            if (instruction.Definition.Opcode != Line)
            {
                return false;
            }
        }

        return false;
    }

    private static bool OverwritesConstructionTemporary(BeamInstruction instruction, BeamOperand temporary) =>
        instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
        && SameProjectionRegister(destination, temporary)
        && !SameProjectionRegister(source, temporary);
}
