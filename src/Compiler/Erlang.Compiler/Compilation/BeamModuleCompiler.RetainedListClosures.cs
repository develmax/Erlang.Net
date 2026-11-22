using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleRetainedListClosures(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var entries = new HashSet<int>();
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadRetainedListClosureCorridor(
                code,
                body,
                start,
                end
            ) is not { } plan)
            {
                continue;
            }

            int heap = plan.Heap;
            int closureHeap = heap + 3;
            int make = heap + 4;
            BeamOperand x0 = new(BeamOperandParameters.X, 0);
            BeamOperand list = body[heap + 2].Operands[1];

            // The cons and the first uncaptured fun consume one shared heap
            // reservation. The original call result remains rooted until put_list.
            output.Write(code.Span[copied..body[heap].Offset]);
            output.Write(code.Span[body[closureHeap].Offset..plan.ClosureWords]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, ConsHeapWords);
            output.Write(code.Span[plan.ClosureWordsEnd..(body[make].Offset - 1)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[body[heap + 1].Offset..(body[heap + 2].Offset - 1)]);
            WriteRegister(output, list);

            int function = body[make].Offset + 1;
            var cursor = new BeamCursor(code[function..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[body[make].Offset..(function + cursor.Position)]);
            _ = BeamCompactReader.Read(cursor);
            WriteRegister(output, x0);
            output.Write(code.Span[(function + cursor.Position)..body[heap + 6].Offset]);
            output.Write(code.Span[body[heap + 7].Offset..body[plan.AfterLastRead].Offset]);
            WriteFrameInitialization(output, [(int)list.Value]);
            copied = body[plan.AfterLastRead].Offset;
            entries.Add((int)body[start].Operands[0].Value);
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();

        return DecodeLateAtomicProjectionSlots(rewritten, labelCount) is { } updated
            ? RewriteDisjointFrames(
                rewritten,
                updated.Body,
                updated.StackOperands,
                atomicOnly: false,
                entriesToCompact: entries
            )
            : null;
    }

    private static (int Heap, int ClosureWords, int ClosureWordsEnd, int AfterLastRead)? ReadRetainedListClosureCorridor(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        if (start + 14 >= end || body[start].Definition.Opcode != Label
            || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, { Value.IsZero: true }] }
            || width != 2 || body[start + 2] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [{ Value: var count }, { Kind: BeamOperandParameters.Y, Value.IsZero: true },
                { Kind: BeamOperandParameters.Y, Value.IsOne: true }]
            } || count != 2)
        {
            return null;
        }

        int producer = SkipRetainedListClosureLine(body, start + 3);
        int heap = producer + 1;
        if (!IsRetainedListClosureCall(body[producer], 0)
            || body[heap] is not { Definition.Opcode: TestHeap, Operands: [{ Value: var words }, { Value.IsOne: true }] }
            || words != ConsHeapWords || ReadHeapWords(code, body[heap]) is null
            || body[heap + 1] is not
            {
                Definition.Opcode: PutList,
                Operands: [
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                    or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                }, var tail, var built]
            } || tail != x0 || built != x0
            || body[heap + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [var constructed, { Kind: BeamOperandParameters.Y, TypeIndex: null } list]
            } || constructed != x0
            || ReadUncapturedClosureReservation(code, body[heap + 3]) is not { } reservation
            || body[heap + 4] is not
            {
                Definition.Opcode: MakeFun,
                Operands: [_, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved, { Value.IsZero: true }]
            } || saved == list
            || !IsRetainedListClosureMove(body[heap + 5], list, x1)
            || !IsRetainedListClosureMove(body[heap + 6], saved, x0))
        {
            return null;
        }

        int firstCall = SkipRetainedListClosureLine(body, heap + 7);
        if (firstCall + 5 >= end || !IsRetainedListClosureCall(body[firstCall], 2)
            || ReadUncapturedClosureReservation(code, body[firstCall + 1]) is null
            || body[firstCall + 2] is not
            {
                Definition.Opcode: MakeFun,
                Operands: [_, var outgoing, { Value.IsZero: true }]
            } || outgoing != x0
            || !IsRetainedListClosureMove(body[firstCall + 3], list, x1)
            || !IsRetainedListClosureCall(body[SkipRetainedListClosureLine(body, firstCall + 4)], 2)
            || !HasUnprotectedCallKillSchedule(body, start)
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame
            || frame.Uses.Count != 1 || !frame.Uses.ContainsKey(start + 1))
        {
            return null;
        }

        int afterRead = firstCall + 4;
        for (int index = afterRead; index < end; index++)
        {
            if (body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                return null;
            }
        }

        return (heap, reservation.Words, reservation.WordsEnd, afterRead);
    }

    private static (int Words, int WordsEnd)? ReadUncapturedClosureReservation(
        ReadOnlyMemory<byte> code,
        BeamInstruction instruction
    )
    {
        if (instruction is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] })
        {
            return null;
        }

        int start = instruction.Offset + 1;
        var cursor = new BeamCursor(code[start..]);
        if (!Next(ExtendedTag, AllocationList) || !Next(BeamWire.UnsignedTag, AllocationKinds)
            || !Next(BeamWire.UnsignedTag, AllocationWords))
        {
            return null;
        }

        int words = start + cursor.Position;
        if (!Next(BeamWire.UnsignedTag, 0))
        {
            return null;
        }

        int wordsEnd = start + cursor.Position;

        return Next(BeamWire.UnsignedTag, AllocationFloats) && Next(BeamWire.UnsignedTag, 0)
            && Next(BeamWire.UnsignedTag, AllocationFuns) && Next(BeamWire.UnsignedTag, 1)
            ? (words, wordsEnd)
            : null;

        bool Next(int tag, int value)
        {
            BeamCompactValue operand = BeamCompactReader.Read(cursor);

            return operand.WireTag == tag && operand.Value == value;
        }
    }

    private static int SkipRetainedListClosureLine(IReadOnlyList<BeamInstruction> body, int index) =>
        body[index].Definition.Opcode == Line ? index + 1 : index;

    private static bool IsRetainedListClosureMove(BeamInstruction instruction, BeamOperand source, BeamOperand destination) =>
        instruction is { Definition.Opcode: Move, Operands: [var read, var written] } && read == source && written == destination;

    private static bool IsRetainedListClosureCall(BeamInstruction instruction, int arity) =>
        instruction is { Definition.Opcode: LocalCall or ExternalCall, Operands: [var count, _] } && count.Value == arity;
}
