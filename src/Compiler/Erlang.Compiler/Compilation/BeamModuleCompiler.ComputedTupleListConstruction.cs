using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CombineComputedTupleListConstruction(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled)
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(
                    BeamWire.FormatVersion,
                    highestOpcode,
                    labelCount,
                    exports.Count + locals.Count,
                    code
                ),
                atoms.Count + 1,
                literalBytes.Count
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        const int tupleArity = 2;
        const int tupleWords = tupleArity + 1;
        const int listWords = 3 * ConsHeapWords;
        const int suffixWords = 2 * tupleWords + listWords;
        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var importsByIndex = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        var y2 = new BeamOperand(BeamOperandParameters.Y, 2);
        var nil = new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex);
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        int frame = -1;
        for (int index = 0; index + 16 < body.Count; index++)
        {
            var instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case FunctionInfo:
                    entry = index + 1;
                    frame = -1;
                    break;
                case Allocate when frame < 0 && instruction.Operands[0].Value == 3:
                    frame = index;
                    break;
            }

            if (frame < entry || !Moves(index, x0, y1)
                || !ReservesHeap(index + 1, tupleWords, 0)
                || !ReadsTuple(
                    index + 2,
                    x0,
                    y0,
                    out var firstTag
                )
                || !Moves(index + 3, x0, y2))
            {
                continue;
            }

            int arithmetic = SkipLines(index + 4);
            if (arithmetic + 10 >= body.Count || !UsesRetainedValue(body[arithmetic])
                || !ReservesHeap(arithmetic + 1, suffixWords, 1)
                || !ReadsTuple(
                    arithmetic + 2,
                    x0,
                    x0,
                    out var secondTag
                )
                || !Moves(arithmetic + 3, x0, x1)
                || !ReadsTuple(
                    arithmetic + 4,
                    x0,
                    y1,
                    out var lastTag
                )
                || !Cons(
                    arithmetic + 5,
                    x0,
                    nil,
                    x0
                )
                || !Cons(
                    arithmetic + 6,
                    x1,
                    x0,
                    x0
                )
                || !Cons(
                    arithmetic + 7,
                    y2,
                    x0,
                    x1
                )
                || !Moves(arithmetic + 8, y0, y2)
                || !Trims(arithmetic + 9, 2))
            {
                continue;
            }

            int suffix = arithmetic + 10;
            if (!IsStraightFramePrefix(frame, index)
                || body[suffix] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.LiteralValue
                        or BeamOperandParameters.Integer or BeamOperandParameters.Nil
                    }, var target]
                }
                || target != x0)
            {
                continue;
            }

            int call = SkipLines(suffix + 1);
            if (call + 3 >= body.Count || body[call].Definition.Opcode is not (ExternalCall or LocalCall)
                || body[call].Operands[0].Value != 2
                || !Moves(call + 1, y0, x0)
                || body[call + 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[call + 3].Definition.Opcode != Return)
            {
                continue;
            }

            // The first tuple has no dependencies on the scalar operation and is
            // only consumed in the final list. Plan all scalar operands before
            // constructing list elements from right to left, as Core orders them.
            output.Write(code.Span[copied..body[frame].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[frame].Operands[1].Value);
            copied = body[frame + 1].Offset;
            for (int prefix = frame + 1; prefix < index; prefix++)
            {
                BeamInstruction item = body[prefix];
                if (item.Definition.Opcode == InitYRegisters && item.Operands.Contains(y2))
                {
                    output.Write(code.Span[copied..item.Offset]);
                    int[] keep = item.Operands.Skip(1).Where(slot => slot != y2).Select(slot => (int)slot.Value).ToArray();
                    if (keep.Length != 0)
                    {
                        WriteFrameInitialization(output, keep);
                    }

                    copied = body[prefix + 1].Offset;
                }
            }

            output.Write(code.Span[copied..instruction.Offset]);
            output.Write(code.Span[body[index + 4].Offset..body[arithmetic].Offset]);
            int afterLive = WriteLeafArithmeticRoots(
                code,
                output,
                body[arithmetic].Offset,
                body[arithmetic].Offset,
                1
            );
            int afterArithmetic = body[arithmetic + 1].Offset;
            output.Write(code.Span[afterLive..(afterArithmetic - 1)]);
            WriteRegister(output, x1);

            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleWords + suffixWords);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            WriteTuple(x0, lastTag, x0);
            WriteCons(x0, nil, x0);
            WriteTuple(x1, secondTag, x1);
            WriteCons(x1, x0, x0);
            WriteTuple(x1, firstTag, y0);
            WriteCons(x1, x0, x1);

            output.WriteByte(Move);
            WriteRegister(output, y0);
            WriteRegister(output, y1);
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = body[suffix].Offset;
            index = call + 3;
            frame = -1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool IsStraightFramePrefix(int first, int last)
        {
            for (int offset = first + 1; offset < last; offset++)
            {
                BeamInstruction prefix = body[offset];
                if (prefix.Definition.Opcode is not (Line or Move or InitYRegisters
                        or GcBifTwo or LocalCall or ExternalCall)
                    || prefix.Definition.Opcode != InitYRegisters
                        && prefix.Operands.Any(operand => SameProjectionRegister(operand, y2))
                    || prefix.Definition.Opcode == GcBifTwo && !prefix.Operands[0].Value.IsZero)
                {
                    return false;
                }
            }

            return true;
        }

        bool UsesRetainedValue(BeamInstruction candidate) =>
            IsLeafArithmeticGcBif(candidate, atomNames, importsByIndex)
            && candidate.Operands[0].Value.IsZero && candidate.Operands[1].Value.IsZero
            && SameProjectionRegister(candidate.Operands[3], y0)
            && candidate.Operands[4].Kind == BeamOperandParameters.Integer
            && candidate.Operands[5] == x0;

        bool Moves(int offset, BeamOperand from, BeamOperand to) =>
            body[offset] is { Definition.Opcode: Move, Operands: [var source, var target] }
            && source == from && target == to;

        bool ReservesHeap(int offset, int terms, int live) =>
            body[offset] is { Definition.Opcode: TestHeap, Operands: [var words, var roots] }
            && words.Value == terms && roots.Value == live;

        bool ReadsTuple(
            int offset,
            BeamOperand target,
            BeamOperand value,
            out BeamOperand tag
        )
        {
            if (body[offset] is
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var tuple, { Value: var arity }, { Kind: BeamOperandParameters.Atom } atom, var field]
                }
                && tuple == target && arity == tupleArity && field == value)
            {
                tag = atom;

                return true;
            }

            tag = null!;

            return false;
        }

        bool Cons(
            int offset,
            BeamOperand head,
            BeamOperand tail,
            BeamOperand destination
        ) =>
            body[offset] is { Definition.Opcode: PutList, Operands: [var h, var t, var d] }
            && head == h && tail == t && destination == d;

        bool Trims(int offset, int removed) =>
            body[offset] is { Definition.Opcode: TrimFrame, Operands: [var slots, { Value.IsOne: true }] }
            && slots.Value == removed;

        void WriteTuple(BeamOperand target, BeamOperand tag, BeamOperand value)
        {
            output.WriteByte(PutTuple);
            WriteRegister(output, target);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleArity);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteRegister(output, value);
        }

        void WriteCons(BeamOperand head, BeamOperand tail, BeamOperand destination)
        {
            output.WriteByte(PutList);
            WriteRegister(output, head);
            if (tail.Kind == BeamOperandParameters.Nil)
            {
                BeamCompactWriter.Write(output, AtomTag, NilAtomIndex);
            }
            else
            {
                WriteRegister(output, tail);
            }

            WriteRegister(output, destination);
        }
    }
}
