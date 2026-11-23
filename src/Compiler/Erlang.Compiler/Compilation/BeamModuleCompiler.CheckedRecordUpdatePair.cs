using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardCheckedRecordUpdatePair(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded
            || RewriteCheckedRecordUpdatePair(code, decoded.Body) is not { } rewritten)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, Swap);

        return rewritten;
    }

    private static byte[]? RewriteCheckedRecordUpdatePair(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        int copied = 0;
        for (int arity = 6; arity + 7 < body.Count; arity++)
        {
            if (body[arity - 5].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[arity - 4] is not { Definition.Opcode: IsTuple, Operands: [_, var rawResult] } || rawResult != x0
                || body[arity - 3] is not { Definition.Opcode: IsTaggedTuple, Operands: [var invalid, var result, { Value: var outerWidth }, _] }
                || !SameProjectionRegister(result, x0) || outerWidth != 2
                || !ReadsField(
                    arity - 2,
                    x0,
                    1,
                    x1
                )
                || body[arity - 1] is not { Definition.Opcode: IsTuple, Operands: [var badTuple, var pair] }
                || badTuple != invalid || pair != x1
                || body[arity] is not { Definition.Opcode: TestArity, Operands: [var badArity, var checkedPair, { Value: var width }] }
                || badArity != invalid || checkedPair != x1 || width != 2
                || body[arity + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var replySource, { Value.IsZero: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } replySlot]
                } || replySource != x1
                || body[arity + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var updateSource, { Value.IsOne: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } updateSlot]
                } || updateSource != x1 || replySlot == updateSlot
                || body[arity + 3] is not { Definition.Opcode: TestHeap, Operands: [{ Kind: BeamOperandParameters.Unsigned }, { Value.IsZero: true }] }
                || body[arity + 4] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Atom } mode, var recordWidth, { Kind: BeamOperandParameters.Y } record,
                        var updated, { Value: var fields }, var field, var replacement
                    ]
                } || updated != x1 || fields != 2 || replacement != updateSlot
                || SameProjectionRegister(record, replySlot) || SameProjectionRegister(record, updateSlot)
                || body[arity + 5] is not { Definition.Opcode: Move, Operands: [var reply, var destination] }
                || reply != replySlot || destination != x0
                || body[arity + 6] is not { Definition.Opcode: InitYRegisters, Operands: var cleared }
                || !cleared.Skip(1).Contains(replySlot) || !cleared.Skip(1).Contains(updateSlot))
            {
                continue;
            }

            int initialized = arity - 6;
            while (initialized >= 0 && body[initialized].Definition.Opcode == Line)
            {
                initialized--;
            }

            if (initialized < 0 || body[initialized] is not { Definition.Opcode: InitYRegisters, Operands: var priorRoots }
                || !priorRoots.Skip(1).Contains(replySlot) || !priorRoots.Skip(1).Contains(updateSlot))
            {
                continue;
            }

            int call = arity + 7;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call == body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arguments }, _] } || arguments != 2)
            {
                continue;
            }

            // Guards keep the raw call result until all checks have succeeded.
            // At the collection point the checked pair is still X1, with the
            // original result filling X0. All original Y roots remain owned.
            output.Write(code.Span[copied..body[arity + 1].Offset]);
            int live = WritePostCallProjectionOperand(
                output,
                code,
                body[arity + 3].Offset,
                body[arity + 3].Offset,
                1,
                2
            );
            output.Write(code.Span[live..body[arity + 4].Offset]);
            WriteField(1, x0);
            output.WriteByte(UpdateRecord);
            BeamCompactWriter.Write(output, AtomTag, mode.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, recordWidth.Value);
            if (record.TypeIndex is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, record);
            if (record.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            WriteRegister(output, x0);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, fields);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, x0);
            WriteField(0, x1);
            output.WriteByte(Swap);
            WriteRegister(output, x0);
            WriteRegister(output, x1);
            copied = body[arity + 6].Offset;
            arity += 5;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadsField(
            int index,
            BeamOperand expectedSource,
            int field,
            BeamOperand expectedDestination
        ) => body[index] is { Definition.Opcode: GetTupleElement, Operands: [var source, var selected, var destination] }
            && source == expectedSource && selected.Value == field && destination == expectedDestination;

        void WriteField(int field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
            WriteRegister(output, destination);
        }
    }
}
