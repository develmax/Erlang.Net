using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DelayEntryFieldArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteEntryFieldArgument(code, decoded.Body);
    }

    private static byte[]? RewriteEntryFieldArgument(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        for (int call = 3; call < body.Count; call++)
        {
            if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value <= 0)
            {
                continue;
            }

            int argument = call - 1;
            while (argument > 0 && body[argument].Definition.Opcode == Line)
            {
                argument--;
            }

            if (body[argument] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } temporary, var destination]
                } || destination != x0 || temporary.Value < arity.Value)
            {
                continue;
            }

            int allocation = argument - 1;
            while (allocation > 0 && (body[allocation].Definition.Opcode == InitYRegisters || body[allocation] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source, { Kind: BeamOperandParameters.Y }]
                } && source != temporary))
            {
                allocation--;
            }

            if (allocation <= 0 || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[allocation].Operands[^1].Value != temporary.Value + 1
                || body[allocation - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, var field, var projected]
                } || projected != temporary || tuple.Value >= temporary.Value
                || body[allocation - 1].Offset < copied)
            {
                continue;
            }

            // The temporary is the highest root at allocation and is read
            // only by the final argument move. The original tuple stays rooted.
            int previousLive = (int)temporary.Value + 1;
            int operandEnd = body[allocation + 1].Offset;
            output.Write(code.Span[copied..body[allocation - 1].Offset]);
            output.Write(code.Span[body[allocation].Offset..(operandEnd - CompactRegisterCountWidth(previousLive))]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, temporary.Value);
            output.Write(code.Span[operandEnd..body[argument].Offset]);
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, XRegisterTag, tuple.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            BeamCompactWriter.Write(output, XRegisterTag, x0.Value);
            copied = body[argument + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
