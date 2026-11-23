using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferRetainedListField(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteRetainedListField(code, decoded.Body);
    }

    private static byte[]? RewriteRetainedListField(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        int copied = 0;
        for (int start = 0; start + 10 < body.Count; start++)
        {
            if (body[start] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, var field,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } projected]
                } || tuple.Value >= projected.Value
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsOne: true }, var live] }
                || live.Value != projected.Value + 1
                || body[start + 2] is not { Definition.Opcode: Move, Operands: [var source, var retained] }
                || source != projected || retained != y0
                || body[start + 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tailInput, var first]
                } || first != x0 || tailInput == x0 || tailInput.Value >= projected.Value
                || body[start + 4].Definition.Opcode != Line
                || body[start + 5] is not { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, _] }
                || body[start + 6] is not { Definition.Opcode: TestHeap, Operands: [var need, { Value.IsOne: true }] }
                || need.Value != ConsHeapWords
                || body[start + 7] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
                || head != y0 || tail != x0 || list != x0
                || body[start + 8] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[start + 9].Definition.Opcode != Return)
            {
                continue;
            }

            // Retain the checked tuple through the call and project its
            // field after the final heap test, where X1 is available.
            output.Write(code.Span[copied..body[start].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, projected.Value);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, tuple.Value);
            BeamCompactWriter.Write(output, YRegisterTag, y0.Value);
            output.Write(code.Span[body[start + 3].Offset..body[start + 7].Offset]);
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, YRegisterTag, y0.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            BeamCompactWriter.Write(output, XRegisterTag, x1.Value);
            output.WriteByte(PutList);
            BeamCompactWriter.Write(output, XRegisterTag, x1.Value);
            BeamCompactWriter.Write(output, XRegisterTag, x0.Value);
            BeamCompactWriter.Write(output, XRegisterTag, x0.Value);
            copied = body[start + 8].Offset;
            start += 7;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
