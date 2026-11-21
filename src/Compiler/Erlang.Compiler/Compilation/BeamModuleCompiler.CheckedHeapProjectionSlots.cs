using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PromoteCheckedHeapProjectionSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded
            || RewriteCheckedHeapProjectionSlots(
                code,
                decoded.Body,
                decoded.StackOperands
            ) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten.Code, labelCount) is not { } updated)
        {
            return null;
        }

        // Publish only with removal of the Y slot that no longer receives its
        // projected definition before the collector.
        return RewriteDisjointFrames(
            rewritten.Code,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: rewritten.Entries
        );
    }

    private static (byte[] Code, HashSet<int> Entries)? RewriteCheckedHeapProjectionSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
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
            if (entry >= end || body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                || PlanCheckedHeapProjectionSlot(
                    body,
                    entry,
                    end,
                    ref flow
                ) is not { } plan)
            {
                continue;
            }

            int projection = body[plan.Projection].Offset;
            int heap = body[plan.Projection + 1].Offset;
            int afterHeap = body[plan.Projection + 2].Offset;
            int previousLive = (int)body[plan.Projection + 1].Operands[^1].Value;
            int definition = FirstStackOperandAtOrAfter(stackOperands, projection);
            int read = FirstStackOperandAtOrAfter(stackOperands, body[plan.Consumer].Offset);
            int readEnd = body[plan.Consumer + 1].Offset;
            while (read < stackOperands.Count && stackOperands[read].Start < readEnd
                && stackOperands[read].Slot != plan.Slot)
            {
                read++;
            }

            if (definition >= stackOperands.Count || stackOperands[definition].Slot != plan.Slot
                || stackOperands[definition].End != heap || read >= stackOperands.Count
                || stackOperands[read].Start >= readEnd)
            {
                continue;
            }

            entries.Add((int)label.Value);
            output.Write(code.Span[copied..projection]);
            WriteHeapRoots(
                output,
                code,
                heap,
                afterHeap,
                previousLive,
                plan.Live
            );
            output.Write(code.Span[projection..stackOperands[definition].Start]);
            BeamCompactWriter.Write(output, XRegisterTag, plan.Register);
            // The reader reports the register's inner bytes for a typed operand.
            output.Write(code.Span[afterHeap..stackOperands[read].Start]);
            BeamCompactWriter.Write(output, XRegisterTag, plan.Register);
            copied = stackOperands[read].End;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return (output.ToArray(), entries);
    }

    private static (int Projection, int Consumer, int Register, int Slot, int Live)? PlanCheckedHeapProjectionSlot(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        for (int projection = entry + 1; projection + 2 < end; projection++)
        {
            if (body[projection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X } tuple,
                    { Kind: BeamOperandParameters.Unsigned } field,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                }
                || body[projection + 1] is not { Definition.Opcode: TestHeap, Operands: [_, var live] }
                || tuple.Value > live.Value || live.Value >= FirstReservedXRegister
                || !HasCheckedHeapTuple(
                    body,
                    entry,
                    projection,
                    tuple,
                    field
                )
                || FindSingleHeapProjectionConsumer(
                    body,
                    entry,
                    end,
                    projection,
                    saved
                ) is not { } consumer
                || ReadDisjointFrameOwnership(body, entry, end) is not { } frame || frame.Uses.Count != 1)
            {
                continue;
            }

            int owner = frame.Uses.Keys.Single();
            if (Enumerable.Range(projection, consumer - projection + 1).Any(index => frame.Owners[index - entry] != owner)
                || Enumerable.Range(entry, end - entry).Any(index => index != owner && frame.Owners[index - entry] != owner
                    && (LinearFrameSizeOperand(body[index].Definition.Opcode) >= 0
                        || body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))))
            {
                continue;
            }

            int firstScratch = (int)live.Value;
            int lastScratch = firstScratch;
            int destination = StackAllocationDestination(body[consumer]);
            var unavailable = new HashSet<int>();
            for (int index = projection + 2; index <= consumer; index++)
            {
                var operands = body[index].Operands;
                for (int position = 0; position < operands.Count; position++)
                {
                    if (operands[position] is { Kind: BeamOperandParameters.X } register
                        && (index != consumer || position != destination))
                    {
                        unavailable.Add((int)register.Value);
                        lastScratch = Math.Max(lastScratch, (int)register.Value);
                    }
                }
            }

            lastScratch = Math.Min(lastScratch + 1, FirstReservedXRegister - 1);
            for (int register = firstScratch; register <= lastScratch; register++)
            {
                if (unavailable.Contains(register))
                {
                    continue;
                }

                var scratch = new BeamOperand(BeamOperandParameters.X, register);
                if (destination >= 0 && SameProjectionRegister(body[consumer].Operands[destination], scratch)
                    || (flow ??= new BeamProjectionFlow(body)).IsEntryTemporaryDead(consumer + 1, scratch))
                {
                    // Root the original tuple across the reservation. The
                    // scratch register is defined by the projection afterward.
                    int roots = Math.Max(firstScratch, (int)tuple.Value + 1);

                    return (projection, consumer, register, (int)saved.Value, roots);
                }
            }
        }

        return null;
    }

    private static int? FindSingleHeapProjectionConsumer(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        int projection,
        BeamOperand saved
    )
    {
        int consumer = -1;
        for (int index = entry; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is DebugLine or ExecutableLine)
            {
                return null;
            }

            if (index == projection || opcode == InitYRegisters)
            {
                continue;
            }

            int destination = StackAllocationDestination(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (!SameProjectionRegister(instruction.Operands[position], saved))
                {
                    continue;
                }

                if (consumer >= 0 || index <= projection + 1 || position == destination
                    || opcode is not (Move or PutList or PutTuple or MakeFun or UpdateRecord)
                    || destination < 0 || instruction.Operands[destination].Kind != BeamOperandParameters.X)
                {
                    return null;
                }

                consumer = index;
            }
        }

        if (consumer < 0 || body.Skip(projection + 2).Take(consumer - projection - 1).Any(instruction =>
            instruction.Definition.Opcode is not (Line or Move or GetTupleElement or PutList or PutTuple or MakeFun or UpdateRecord)))
        {
            return null;
        }

        return consumer;
    }

    private static bool HasCheckedHeapTuple(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int projection,
        BeamOperand tuple,
        BeamOperand field
    )
    {
        int check = projection - 1;
        while (check > entry && (body[check].Definition.Opcode == Line
            || body[check] is { Definition.Opcode: GetTupleElement, Operands: [_, _, var target] }
                && !SameProjectionRegister(target, tuple)))
        {
            check--;
        }

        BeamInstruction instruction = body[check];
        if (instruction.Definition.Opcode == IsTaggedTuple)
        {
            return SameProjectionRegister(instruction.Operands[1], tuple)
                && field.Value < instruction.Operands[2].Value;
        }

        return check > entry && instruction.Definition.Opcode == TestArity
            && SameProjectionRegister(instruction.Operands[1], tuple)
            && field.Value < instruction.Operands[2].Value
            && body[check - 1] is { Definition.Opcode: IsTuple, Operands: [var failed, var subject] }
            && failed == instruction.Operands[0] && SameProjectionRegister(subject, tuple);
    }
}
