using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string ReuseRecordUpdateHint = "reuse";

    private byte[]? ScheduleCheckedRecordTupleReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(UpdateRecord)
            || !code.Span.Contains(SelectTupleArity) || !atoms.TryGetValue(ReuseRecordUpdateHint, out int reuse)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var replacement = new MemoryStream();
        int copied = 0;
        for (int dispatch = 8; dispatch + 1 < body.Count; dispatch++)
        {
            if (body[dispatch] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var selector, _, var size, var pairSize, var pairLabel, var tripleSize, var tripleLabel]
                }
                || selector.Kind != BeamOperandParameters.X || !selector.Value.IsZero || size.Value != 4
                || pairSize.Value != 2 || tripleSize.Value != 3
                || pairLabel.Kind != BeamOperandParameters.Label || tripleLabel.Kind != BeamOperandParameters.Label
                || body[dispatch - 1] is not { Definition.Opcode: IsTuple, Operands: [_, var tested] }
                || !IsX(tested, 0))
            {
                continue;
            }

            int call = dispatch - 2;
            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            // The record check, sole stack allocation and unconditional save establish the carrier.
            int save = call - 1;
            while (save >= 2 && body[save].Definition.Opcode is Line or GetTupleElement)
            {
                if (body[save].Definition.Opcode == GetTupleElement
                    && body[save].Operands[^1].Kind != BeamOperandParameters.X)
                {
                    break;
                }

                save--;
            }

            if (save < 3 || body[save] is not { Definition.Opcode: Move, Operands: [var entry, var carrier] }
                || !IsX(entry, 0) || carrier is not { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null }
                || body[save - 1] is not { Definition.Opcode: Allocate, Operands: [var frame, var live] }
                || frame.Value != 1 || live.Value < 1
                || body[save - 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var checkedRecord, var width, { Kind: BeamOperandParameters.Atom }]
                }
                || !IsX(checkedRecord, 0) || width.Value < 4 || width.Value > BeamOperandParameters.MaximumManagedArguments
                || body[save - 3].Definition.Opcode != Label)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(dispatch, (int)tripleLabel.Value, out int triple)
                || !flow.TryGetSingleForwardTarget(dispatch, (int)pairLabel.Value, out int pair)
                || triple != dispatch + 1 || pair != triple + 8
                || !flow.IsInitialized(triple + 1, carrier) || !flow.IsInitialized(pair + 1, carrier))
            {
                continue;
            }

            // Only the first projection feeds the update. The original result tuple in X0
            // is distinct from the checked record and stays rooted until its other field is read.
            if (pair + 9 >= body.Count
                || !Projection(triple + 1, 2, out int continuationField)
                || !Projection(triple + 2, 1, out int payloadField)
                || continuationField == payloadField || continuationField >= (int)pairSize.Value
                || payloadField >= (int)pairSize.Value
                || !Heap(triple + 3, (int)width.Value + 5, 3)
                || !Update(
                    triple + 4,
                    carrier,
                    (int)width.Value,
                    out int field
                )
                || !SuccessTuple(triple + 5) || !FrameReturn(triple + 6)
                || !Projection(pair + 1, 1, out int comparedField) || comparedField != continuationField
                || body[pair + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } reject, var compared, { Kind: BeamOperandParameters.Atom }]
                }
                || !IsX(compared, 1) || !Heap(pair + 3, 6, 1)
                || !Projection(pair + 4, 0, out int errorField) || errorField != payloadField
                || !WrapsX0(pair + 5) || !WrapsX0(pair + 6) || !FrameReturn(pair + 7)
                || !flow.TryGetSingleForwardTarget(pair + 2, (int)reject.Value, out int fallback)
                || fallback != pair + 9 || fallback + 12 >= body.Count)
            {
                continue;
            }

            // Both sides of the comparison need the second field. Only its exclusive
            // failure edge needs the continuation, so keep the two projections in X1 and X0.
            if (!Projection(fallback + 1, 2, out int repeatedContinuation) || repeatedContinuation != continuationField
                || !Projection(fallback + 2, 1, out int repeatedPayload) || repeatedPayload != payloadField
                || body[fallback + 3] is not
                {
                    Definition.Opcode: IsNil,
                    Operands: [{ Kind: BeamOperandParameters.Label } nonemptyLabel, var list]
                }
                || !IsX(list, 1) || !Heap(fallback + 4, (int)width.Value + 1, 3)
                || !Update(
                    fallback + 5,
                    carrier,
                    (int)width.Value,
                    out int nilField
                ) || nilField != field
                || body[fallback + 6] is not { Definition.Opcode: TailLocalCallWithFrame, Operands: [var arity, _, var tailFrame] }
                || arity.Value != 1 || tailFrame.Value != 1
                || !flow.TryGetSingleForwardTarget(fallback + 3, (int)nonemptyLabel.Value, out int nonempty)
                || nonempty != fallback + 7
                || !Heap(nonempty + 1, (int)width.Value + 5, 3)
                || !Update(
                    nonempty + 2,
                    carrier,
                    (int)width.Value,
                    out int nonemptyField
                ) || nonemptyField != field
                || !SuccessTuple(nonempty + 3) || !FrameReturn(nonempty + 4))
            {
                continue;
            }

            replacement.Write(code.Span[copied..body[triple + 1].Offset]);
            RewriteLast(triple + 3, BeamWire.UnsignedTag, 1);
            RewriteLast(triple + 1, XRegisterTag, 1);
            RewriteUpdate(triple + 4, 1);
            RewriteLast(triple + 2, XRegisterTag, 0);
            RewriteTuple(triple + 5);
            Copy(triple + 6, pair + 2);
            Copy(pair + 4, pair + 5);
            Copy(pair + 2, pair + 4);
            Copy(pair + 5, fallback + 1);
            RewriteLast(fallback + 3, XRegisterTag, 0);
            RewriteLast(fallback + 4, BeamWire.UnsignedTag, 2);
            RewriteUpdate(fallback + 5, 0);
            Copy(fallback + 6, nonempty + 1);
            RewriteLast(nonempty + 1, BeamWire.UnsignedTag, 2);
            RewriteUpdate(nonempty + 2, 1);
            RewriteTuple(nonempty + 3);
            copied = body[nonempty + 4].Offset;
            dispatch = nonempty;
        }

        if (copied == 0)
        {
            return null;
        }

        replacement.Write(code.Span[copied..]);

        return replacement.ToArray();

        bool Projection(int index, int destination, out int field)
        {
            if (body[index] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Kind: BeamOperandParameters.Unsigned } member, var target]
                }
                && member.Value <= int.MaxValue && IsX(source, 0) && IsX(target, destination))
            {
                field = (int)member.Value;

                return true;
            }

            field = -1;

            return false;
        }

        bool Update(
            int index,
            BeamOperand carrier,
            int width,
            out int field
        )
        {
            if (body[index] is
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [{ Kind: BeamOperandParameters.Atom } hint, var size, var source, var dest,
                    { Kind: BeamOperandParameters.Count } count, { Kind: BeamOperandParameters.Unsigned } member, var value]
                }
                && hint.Value == reuse && size.Value == width && SameProjectionRegister(carrier, source)
                && IsX(dest, 0) && count.Value == 2 && member.Value >= 1 && member.Value <= width && IsX(value, 2))
            {
                field = (int)member.Value;

                return true;
            }

            field = -1;

            return false;
        }

        bool SuccessTuple(int index) => body[index] is
        {
            Definition.Opcode: PutTuple,
            Operands: [var destination, var count, { Kind: BeamOperandParameters.Atom }, var record, var payload]
        }
            && IsX(destination, 0) && count.Value == 3 && IsX(record, 0) && IsX(payload, 1);

        bool WrapsX0(int index) => body[index] is
        {
            Definition.Opcode: PutTuple,
            Operands: [var destination, var count, { Kind: BeamOperandParameters.Atom or BeamOperandParameters.LiteralValue }, var value]
        }
            && IsX(destination, 0) && count.Value == 2 && IsX(value, 0);

        bool Heap(int index, int words, int live) =>
            body[index] is { Definition.Opcode: TestHeap, Operands: [var cells, var roots] }
            && cells.Kind == BeamOperandParameters.Unsigned && cells.Value == words && roots.Value == live;

        bool FrameReturn(int index) =>
            body[index] is { Definition.Opcode: Deallocate, Operands: [var width] }
            && width.Value == 1 && body[index + 1].Definition.Opcode == Return;

        void Copy(int first, int after) => replacement.Write(code.Span[body[first].Offset..body[after].Offset]);

        void RewriteLast(
            int index,
            int tag,
            int value
        )
        {
            int first = body[index].Offset;
            var cursor = new BeamCursor(code[(first + 1)..body[index + 1].Offset]);
            int trailing = 0;
            while (cursor.Remaining != 0)
            {
                trailing = cursor.Position;
                _ = BeamCompactReader.Read(cursor);
            }

            replacement.Write(code.Span[first..(first + 1 + trailing)]);
            BeamCompactWriter.Write(replacement, tag, value);
        }

        void RewriteTuple(int index)
        {
            replacement.Write(code.Span[body[index].Offset..(body[index + 1].Offset - 2 * CompactRegisterCountWidth(1))]);
            BeamCompactWriter.Write(replacement, XRegisterTag, 1);
            BeamCompactWriter.Write(replacement, XRegisterTag, 0);
        }

        void RewriteUpdate(int index, int destination)
        {
            int first = body[index].Offset;
            int after = body[index + 1].Offset;
            var cursor = new BeamCursor(code[(first + 1)..after]);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            if (BeamCompactReader.Read(cursor).WireTag == BeamOperandParameters.ExtendedTag)
            {
                _ = BeamCompactReader.Read(cursor);
                _ = BeamCompactReader.Read(cursor);
            }

            replacement.Write(code.Span[first..(first + 1 + cursor.Position)]);
            BeamCompactWriter.Write(replacement, XRegisterTag, destination);
            _ = BeamCompactReader.Read(cursor);
            replacement.Write(code.Span[(first + 1 + cursor.Position)..(after - CompactRegisterCountWidth(2))]);
            BeamCompactWriter.Write(replacement, XRegisterTag, 1);
        }

        static bool IsX(BeamOperand operand, int register) =>
            operand is { Kind: BeamOperandParameters.X, TypeIndex: null } && operand.Value == register;
    }
}
