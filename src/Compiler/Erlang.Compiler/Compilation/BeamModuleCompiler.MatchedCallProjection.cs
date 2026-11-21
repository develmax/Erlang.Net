using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? KeepMatchedCallProjectionInX(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var fieldSource = new BeamOperand(BeamOperandParameters.X, 1);
        var forwarded = new BeamOperand(BeamOperandParameters.X, 2);
        int copied = 0;
        int entry = 0;
        for (int index = 0; index + 5 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                entry = index + 1;
            }

            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 3] is not
                {
                    Definition.Opcode: Move, Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } slot, var reset
                ]
                } || reset != result)
            {
                continue;
            }

            int copyResult = index + 1;
            int loadConstant = index + 2;
            if (body[copyResult] is { Definition.Opcode: Move, Operands: [_, var target] } && target == fieldSource)
            {
                (copyResult, loadConstant) = (loadConstant, copyResult);
            }

            // These two independent moves can be ordered before this pass runs.
            if (body[copyResult] is not { Definition.Opcode: Move, Operands: [var source, var pass] }
                || source != result || pass != forwarded
                || body[loadConstant] is not { Definition.Opcode: Move, Operands: [var constant, var assign] }
                || assign != fieldSource || !IsNonRegisterValue(constant))
            {
                continue;
            }

            int call = index + 4;
            while (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call].Operands[0].Value != 3)
            {
                continue;
            }

            int read = ReadMatchedCallProjection(
                body,
                call + 1,
                result,
                fieldSource,
                slot
            );
            if (read < 0)
            {
                continue;
            }

            int save = index - 1;
            while (save >= entry && body[save].Definition.Opcode == Line)
            {
                save--;
            }

            int end = entry;
            bool stableFrame = true;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                stableFrame &= body[end].Definition.Opcode is not (TrimFrame or TryEnter or CatchEnter or DebugLine);
                end++;
            }

            if (!stableFrame || save < entry || body[save] is not
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X }, var saved] }
                || saved != slot || !(flow ??= new BeamProjectionFlow(body)).HasSingleStackCopyUse(
                    entry,
                    save,
                    end,
                    slot,
                    index + 3
                ))
            {
                continue;
            }

            int reload = body[index + 3].Offset;
            int next = body[index + 4].Offset;
            output.Write(code.Span[copied..body[index + 1].Offset]);
            output.Write(code.Span[body[loadConstant].Offset..body[loadConstant + 1].Offset]);
            output.Write(code.Span[body[copyResult].Offset..body[copyResult + 1].Offset]);
            output.Write(code.Span[reload..next]);
            WriteFrameInitialization(output, [(int)slot.Value]);
            int project = body[read].Offset;
            int heap = body[read + 1].Offset;
            int tuple = body[read + 2].Offset;
            copied = body[read + 3].Offset;
            int slotWidth = CompactRegisterCountWidth((int)slot.Value);
            output.Write(code.Span[next..project]);
            output.Write(code.Span[heap..(tuple - 1)]);
            // Both the call result and the nested tuple have been initialized.
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            output.Write(code.Span[project..(heap - slotWidth)]);
            WriteRegister(output, result);
            output.Write(code.Span[tuple..(copied - slotWidth)]);
            WriteRegister(output, result);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int ReadMatchedCallProjection(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand result,
        BeamOperand fieldSource,
        BeamOperand slot
    )
    {
        int index = start;
        while (index < body.Count && body[index].Definition.Opcode is IsTuple or IsTaggedTuple or TestArity
            && body[index].Operands.Any(operand => SameProjectionRegister(operand, result)))
        {
            index++;
        }

        if (index + 7 >= body.Count
            || body[index] is not { Definition.Opcode: GetTupleElement, Operands: [var outer, _, var nested] }
            || !SameProjectionRegister(outer, result) || nested != fieldSource
            || body[index + 1] is not { Definition.Opcode: IsTuple, Operands: [_, var tested] }
            || !SameProjectionRegister(tested, fieldSource)
            || body[index + 2] is not { Definition.Opcode: TestArity, Operands: [_, var tuple, _] }
            || !SameProjectionRegister(tuple, fieldSource)
            || body[index + 3] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ TypeIndex: null } src, _, var selected]
            } || src != fieldSource || selected != slot
            || body[index + 4] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
            || body[index + 5] is not { Definition.Opcode: PutTuple, Operands: [var to, _, var tag, var from] }
            || to != result || !IsNonRegisterValue(tag) || from != slot
            || body[index + 6].Definition.Opcode != Deallocate
            || body[index + 7].Definition.Opcode != Return)
        {
            return -1;
        }

        return index + 3;
    }

    private static bool IsNonRegisterValue(BeamOperand operand) =>
        operand.Kind is BeamOperandParameters.Integer or BeamOperandParameters.Atom or BeamOperandParameters.Nil
            or BeamOperandParameters.LiteralValue;
}
