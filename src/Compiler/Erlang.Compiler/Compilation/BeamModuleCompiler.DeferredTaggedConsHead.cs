using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferSavedTaggedRecursiveConsHeads(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutTuple)
            || !code.Span.Contains(AllocateHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream(code.Length);
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
            if (start + 9 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] })
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int outer = start + 4; outer + 7 < end; outer++)
            {
                int group = outer - 2;
                if (body[outer] is not
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var tuple, { Value: var arity }, { Kind: BeamOperandParameters.Atom } tag,
                        { Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 } name, var saved]
                    }
                    || tuple != x0 || arity != 3 || saved != y1
                    || !IsMove(body[outer - 1], x0, y1) || !IsMove(body[outer + 1], x0, y0)
                    || body[group] is not { Definition.Opcode: PutTuple, Operands: [var constructed, ..] }
                    || constructed != x0 || body[outer + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 }, var argument]
                    }
                    || argument != x0)
                {
                    continue;
                }

                int call = outer + 3;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 4 >= end || body[call] is not
                    { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, var target] }
                    || target.Value != function.Value || body[call + 1] is not
                    {
                        Definition.Opcode: TestHeap, Operands: [{ Value: var heap }, { Value.IsOne: true }]
                    }
                    || heap != ConsHeapWords || body[call + 2] is not
                    {
                        Definition.Opcode: PutList, Operands: [var head, var tail, var result]
                    }
                    || head != y0 || tail != x0 || result != x0 || body[call + 3] is not
                    {
                        Definition.Opcode: Deallocate, Operands: [{ Value: var width }]
                    }
                    || width != 2 || body[call + 4].Definition.Opcode != Return
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners })
                {
                    continue;
                }

                int frame = owners[group - start];
                if (frame < start || body[frame].Offset < copied || body[frame] is not
                    { Definition.Opcode: AllocateHeap, Operands: [var size, { Value: var words }, var live] }
                    || size.Value != width || words < arity + 1
                    || Enumerable.Range(frame + 1, group - frame - 1).Any(index =>
                        body[index].Definition.Opcode is not (InitYRegisters or Move or PutTuple))
                    || Enumerable.Range(group, (call + 4) - group).Any(index => owners[index - start] != frame))
                {
                    continue;
                }

                var reservation = new BeamCursor(code[(body[frame].Offset + 1)..body[frame + 1].Offset]);
                _ = BeamCompactReader.Read(reservation);
                if (BeamCompactReader.Read(reservation).WireTag != BeamWire.UnsignedTag)
                {
                    continue;
                }

                // The nested constant and name are the two values that cross
                // recursion. Construct only the outer tuple after the call.
                output.Write(code.Span[copied..body[frame].Offset]);
                output.WriteByte(AllocateHeap);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, words - arity - 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
                output.Write(code.Span[body[frame + 1].Offset..(body[group].Offset + 1)]);
                WriteRegister(output, y0);
                var groupDestination = new BeamCursor(code[(body[group].Offset + 1)..body[group + 1].Offset]);
                _ = BeamCompactReader.Read(groupDestination);
                output.Write(code.Span[(body[group].Offset + 1 + groupDestination.Position)..body[group + 1].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, name);
                WriteRegister(output, y1);
                output.Write(code.Span[body[outer + 2].Offset..body[call + 1].Offset]);
                output.WriteByte(TestHeap);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heap + arity + 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.WriteByte(PutTuple);
                WriteRegister(output, x1);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity);
                BeamCompactWriter.Write(output, AtomTag, tag.Value);
                WriteRegister(output, y1);
                WriteRegister(output, y0);
                output.WriteByte(PutList);
                WriteRegister(output, x1);
                WriteRegister(output, x0);
                WriteRegister(output, x0);
                copied = body[call + 3].Offset;
                outer = call + 4;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? DeferTaggedRecursiveConsHead(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutTuple)
            || !code.Span.Contains(PutList) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        BeamOperand y1 = new(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream();
        int entry = -1;
        int copied = 0;
        for (int frame = 0; frame + 10 < body.Count; frame++)
        {
            if (body[frame].Definition.Opcode == FunctionInfo)
            {
                entry = body[frame + 1] is { Definition.Opcode: Label, Operands: [var label] } ? (int)label.Value : -1;
                continue;
            }

            if (entry < 0 || body[frame] is not
                {
                    Definition.Opcode: AllocateHeap,
                    Operands: [{ Value.IsOne: true }, { Value: var words }, var live]
                })
            {
                continue;
            }

            var reservation = new BeamCursor(code[(body[frame].Offset + 1)..body[frame + 1].Offset]);
            _ = BeamCompactReader.Read(reservation);
            if (BeamCompactReader.Read(reservation).WireTag != BeamWire.UnsignedTag)
            {
                continue;
            }

            int first = frame + 1;
            if (body[first] is { Definition.Opcode: InitYRegisters, Operands: [_, var initial] } && initial == y0)
            {
                first++;
            }

            int next = first;
            while (next < body.Count && body[next] is { Definition.Opcode: PutTuple, Operands: [var dst, ..] }
                && dst == x0)
            {
                next++;
            }

            int outer = next - 1;
            int savedGroup = outer - 1;
            if (savedGroup < first || next + 2 >= body.Count || body[outer] is not
                {
                    Operands: [_, { Value: var arity }, { Kind: BeamOperandParameters.Atom } tag,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } name, var group]
                }
                || arity != 3 || group != x0 || name == x0
                || !IsMove(body[next], x0, y0)
                || body[next + 1] is not
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X, Value.Sign: > 0 }, var tail] }
                || tail != x0 || words < arity + 1)
            {
                continue;
            }

            int call = next + 2;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 5 >= body.Count || body[call] is not
                {
                    Definition.Opcode: LocalCall,
                    Operands: [{ Value.IsOne: true }, var target]
                } || target.Value != entry
                || body[call + 1] is not
                { Definition.Opcode: TestHeap, Operands: [{ Value: var heap }, { Value.IsOne: true }] }
                || heap != ConsHeapWords
                || body[call + 2] is not { Definition.Opcode: PutList, Operands: [var head, var list, var result] }
                || head != y0 || list != x0 || result != x0
                || body[call + 3] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[call + 4].Definition.Opcode != Return)
            {
                continue;
            }

            // A Core safe cons head defers construction until its tail call
            // has produced a value. Keep the separately bound group and name
            // across that call, preserving all existing effect instructions.
            output.Write(code.Span[copied..body[frame].Offset]);
            output.WriteByte(AllocateHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, words - arity - 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            output.WriteByte(Move);
            WriteRegister(output, name);
            WriteRegister(output, y1);
            output.Write(code.Span[body[first].Offset..body[savedGroup].Offset]);
            output.WriteByte(PutTuple);
            WriteRegister(output, y0);
            int destination = body[savedGroup].Offset + 1;
            var groupOperands = new BeamCursor(code[destination..body[outer].Offset]);
            _ = BeamCompactReader.Read(groupOperands);
            output.Write(code.Span[(destination + groupOperands.Position)..body[outer].Offset]);
            output.Write(code.Span[body[next + 1].Offset..body[call + 1].Offset]);

            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heap + arity + 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.WriteByte(PutTuple);
            WriteRegister(output, x1);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteRegister(output, y1);
            WriteRegister(output, y0);
            output.WriteByte(PutList);
            WriteRegister(output, x1);
            WriteRegister(output, x0);
            WriteRegister(output, x0);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            copied = body[call + 4].Offset;
            frame = call + 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
