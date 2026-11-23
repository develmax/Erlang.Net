using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderClosedCatchResultDispatch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnd)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteClosedCatchResultDispatch(code, body, () => GetOperandTypeIndex((TupleTypeBits, null, null, null)));
    }

    private static byte[]? RewriteClosedCatchResultDispatch(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<int> tupleType
    )
    {
        var selector = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int first = 1; first + 3 < body.Count; first++)
        {
            if (body[first - 1].Definition.Opcode != CatchEnd
                || body[first] is not
                {
                    Definition.Opcode: ExactNotEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } atomExit, var input, { Kind: BeamOperandParameters.Atom } atom]
                } || input != selector
                || body[first + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } tupleExit, var tuple, var arity, var tag]
                } || tuple != selector || tupleExit == atomExit)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(first, (int)atomExit.Value, out int atomReturn)
                || !flow.TryGetSingleForwardTarget(first + 1, (int)tupleExit.Value, out int fallback)
                || fallback >= atomReturn || atomReturn + 3 >= body.Count
                || body[atomReturn + 1] is not { Definition.Opcode: Deallocate, Operands: [var frame] }
                || body[atomReturn + 2].Definition.Opcode != Return
                || !HasTerminalTail(first + 2, fallback, frame)
                || !HasTerminalTail(fallback + 1, atomReturn, frame))
            {
                continue;
            }

            // Core separates the disjoint tuple and atom classes. The tuple proof
            // applies only to its tagged test. The remaining values reach a single fallback.
            output.Write(code.Span[copied..body[first].Offset]);
            output.WriteByte(IsTuple);
            Write(LabelTag, tupleExit);
            Write(XRegisterTag, selector);

            output.WriteByte(IsTaggedTuple);
            Write(LabelTag, atomExit);
            output.WriteByte(ExtendedTypedRegisterOperand);
            Write(XRegisterTag, selector);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tupleType());
            Write(BeamWire.UnsignedTag, arity);
            Write(AtomTag, tag);
            output.Write(code.Span[body[first + 2].Offset..body[fallback + 1].Offset]);

            output.WriteByte(ExactEqualTest);
            Write(LabelTag, atomExit);
            Write(XRegisterTag, selector);
            Write(AtomTag, atom);
            output.Write(code.Span[body[atomReturn + 1].Offset..body[atomReturn + 3].Offset]);
            output.Write(code.Span[body[atomReturn].Offset..body[atomReturn + 1].Offset]);
            output.Write(code.Span[body[fallback + 1].Offset..body[atomReturn].Offset]);
            copied = body[atomReturn + 3].Offset;
            first = atomReturn + 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Write(int operandTag, BeamOperand operand) => BeamCompactWriter.Write(output, operandTag, operand.Value);

        bool HasTerminalTail(int start, int end, BeamOperand frame) => start < end
            && body[end - 1] is { Definition.Opcode: TailExternalCallWithFrame, Operands: [_, _, var currentFrame] }
            && currentFrame == frame
            && body.Skip(start).Take(end - 1 - start).All(instruction => instruction.Definition.Opcode
                is TestHeap or GetTupleElement or PutList or PutTuple or Swap or Move or Line);
    }
}
