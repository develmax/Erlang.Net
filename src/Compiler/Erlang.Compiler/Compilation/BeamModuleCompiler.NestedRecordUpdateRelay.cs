using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardNestedRecordUpdateRelay(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool joinedOnly = false
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord))
        {
            return null;
        }

        int tupleType = typeBits.IndexOf((TupleTypeBits, null, null, null));
        if (tupleType < 0 || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteNestedRecordUpdateRelays(
            code,
            decoded.Body,
            decoded.StackOperands,
            tupleType,
            joinedOnly
        );
    }

    private static byte[]? RewriteNestedRecordUpdateRelays(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        int tupleType,
        bool joinedOnly
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 4 < body.Count; index++)
        {
            int save = index + (joinedOnly ? 4 : 3);
            if (joinedOnly && (save + 4 >= body.Count || body[index + 3].Definition.Opcode != Label
                || body[save + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[save + 2] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] }
                || destination != x0 || body[save + 3].Definition.Opcode != Deallocate
                || body[save + 4].Definition.Opcode != Return))
            {
                continue;
            }

            if (body[index] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Atom }, { Value: var innerWidth },
                    { Kind: BeamOperandParameters.Y } inner, var innerResult, _, ..
                    ]
                }
                || inner.TypeIndex != tupleType || innerResult != x0
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var produced, { Kind: BeamOperandParameters.Y, TypeIndex: null } scratch]
                }
                || produced != x0 || SameProjectionRegister(inner, scratch)
                || body[index + 2] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Atom }, { Value: var outerWidth },
                    { Kind: BeamOperandParameters.Y, TypeIndex: not null } outer, var outerResult,
                    { Value: var fields }, _, var value
                    ]
                }
                || outerResult != x0 || fields != 2 || value != scratch
                || SameProjectionRegister(outer, inner) || SameProjectionRegister(outer, scratch)
                || body[save] is not { Definition.Opcode: Move, Operands: [var updated, var retained] }
                || updated != x0 || retained != scratch
                || body[index - 1] is not
                { Definition.Opcode: TestHeap, Operands: [{ Kind: BeamOperandParameters.Unsigned, Value: var words }, _] }
                || words < innerWidth + outerWidth + 2)
            {
                continue;
            }

            int outerOffset = body[index + 2].Offset;
            int outerEnd = body[index + 3].Offset;
            int operand = FirstStackOperandAtOrAfter(stackOperands, outerOffset) + 1;
            if (operand >= stackOperands.Count)
            {
                continue;
            }

            var (from, to, slot) = stackOperands[operand];
            if (from >= outerEnd || to != outerEnd || slot != scratch.Value)
            {
                continue;
            }

            // Both updates already have heap reserved. The temporary has one
            // read before the outer result overwrites it in the same frame.
            output.Write(code.Span[copied..body[index + 1].Offset]);
            output.Write(code.Span[outerOffset..from]);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleType);
            copied = to;
            index += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
