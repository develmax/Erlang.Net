using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseClearedTokenFrameSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteClearedTokenFrameSlot(code, decoded.Body);
    }

    private static byte[]? RewriteClearedTokenFrameSlot(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int producer = 2; producer + 18 < body.Count; producer++)
        {
            if (body[producer].Definition.Opcode is not (ExternalCall or LocalCall)
                || body[producer + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var result, { Value: var width }, _]
                }
                || width != 2 || !IsX(result, 0)
                || body[producer + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.IsOne: true }, var list]
                }
                || !IsX(source, 0) || !IsX(list, 0)
                || !ListGuard(producer + 3, IsNonemptyList, rejected)
                || body[producer + 4] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [var first, var token, var rest]
                }
                || !IsX(first, 0) || !IsX(token, 1) || !IsX(rest, 0)
                || !ListGuard(producer + 5, IsNonemptyList, rejected)
                || body[producer + 6] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [var last, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved, var exhausted]
                }
                || !IsX(last, 0) || !IsX(exhausted, 0)
                || !ListGuard(producer + 7, IsNil, rejected)
                || body[producer + 8] is not { Definition.Opcode: Move, Operands: [var moveToken, var converterInput] }
                || !IsX(moveToken, 1) || !IsX(converterInput, 0))
            {
                continue;
            }

            int clear = body[producer - 1].Definition.Opcode == Line ? producer - 2 : producer - 1;
            int convert = body[producer + 9].Definition.Opcode == Line ? producer + 10 : producer + 9;
            int tuple = convert + 2;
            if (body[clear] is not { Definition.Opcode: InitYRegisters } clearRoots
                || body[convert] is not { Definition.Opcode: ExternalCall or LocalCall, Operands: [{ Value.IsOne: true }, _] }
                || body[tuple - 1].Definition.Opcode != TestHeap
                || body[tuple] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var tupleResult, _, var conversionResult, var captured]
                }
                || !IsX(tupleResult, 2) || !IsX(conversionResult, 0) || captured != saved
                || body[tuple + 1] is not { Definition.Opcode: Jump, Operands: [var continueAt] }
                || body[tuple + 2] is not { Definition.Opcode: Label, Operands: [var cold] } || rejected.Value != cold.Value
                || body[tuple + 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.LiteralValue }, var fallback]
                }
                || !IsX(fallback, 2)
                || body[tuple + 4] is not { Definition.Opcode: Label, Operands: [var joined] } || continueAt.Value != joined.Value
                || body[tuple + 5] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y } middle, var secondArgument]
                }
                || !IsX(secondArgument, 1)
                || body[tuple + 6] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y } left, var firstArgument]
                }
                || !IsX(firstArgument, 0)
                || body[tuple + 7] is not { Definition.Opcode: TailLocalCallWithFrame, Operands: [{ Value: var arity }, _, var frame] }
                || arity != 3 || saved.Value == middle.Value || saved.Value == left.Value
                || !clearRoots.Operands.Skip(1).Any(slot => slot.Value == saved.Value))
            {
                continue;
            }

            // All remaining consumers either build the outgoing tuple on
            // the successful path or return the fallback without reading it.
            var replacement = clearRoots.Operands.Skip(1)
                .Where(slot => slot.Value < frame.Value && slot.Value != middle.Value && slot.Value != left.Value)
                .MaxBy(slot => slot.Value);
            if (replacement is null || replacement.Value <= saved.Value || UsesProtectedFrame(producer))
            {
                continue;
            }

            output.Write(code.Span[copied..body[producer + 6].Offset]);
            output.WriteByte(GetList);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, YRegisterTag, replacement.Value);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            output.Write(code.Span[body[producer + 7].Offset..body[tuple].Offset]);
            output.WriteByte(PutTuple);
            BeamCompactWriter.Write(output, XRegisterTag, 2);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, YRegisterTag, replacement.Value);
            copied = body[tuple + 1].Offset;
            producer = tuple + 7;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool UsesProtectedFrame(int position)
        {
            int entry = position;
            while (entry > 0 && body[entry - 1].Definition.Opcode != FunctionInfo)
            {
                entry--;
            }

            for (int index = entry; index < body.Count; index++)
            {
                byte opcode = body[index].Definition.Opcode;
                if (opcode is FunctionInfo or EndCode)
                {
                    break;
                }

                if (opcode is TryEnter or CatchEnter)
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsX(BeamOperand operand, int register) =>
            operand is { Kind: BeamOperandParameters.X, TypeIndex: null } && operand.Value == register;

        bool ListGuard(int index, byte opcode, BeamOperand rejected) =>
            body[index].Definition.Opcode == opcode && body[index].Operands is [var failure, var list]
            && failure == rejected && IsX(list, 0);
    }
}
