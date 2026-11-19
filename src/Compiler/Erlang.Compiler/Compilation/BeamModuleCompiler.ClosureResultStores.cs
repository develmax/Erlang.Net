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
                PutTuple when IsPostCallTupleBeforeOverwrite(body, index) => 0,
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
                || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(store, result))
            {
                continue;
            }

            int operands = construction.Offset + 1;
            var cursor = new BeamCursor(code[operands..]);
            if (destination != 0)
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

    private static bool IsPostCallTupleBeforeOverwrite(IReadOnlyList<BeamInstruction> body, int tuple)
    {
        // Immediately after an ordinary call, X0 is the only call-result root.
        // Keep branch-join transport copies and other construction schedules.
        return tuple >= 2
            && body[tuple - 2].Definition.Opcode is LocalCall or ExternalCall
            && body[tuple - 1] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
            && body[tuple].Operands[0] is { Kind: BeamOperandParameters.X, Value.IsZero: true } result
            && body[tuple].Operands.Skip(2).Any(operand => SameProjectionRegister(operand, result))
            && body[tuple + 2] is { Definition.Opcode: Move, Operands: [var nextSource, var nextDestination] }
            && SameProjectionRegister(nextDestination, result)
            && !SameProjectionRegister(nextSource, result);
    }
}
