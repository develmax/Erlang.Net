using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ProjectCheckedEntryTupleIntoFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 3 < body.Count; index++)
        {
            BeamInstruction projection = body[index];
            BeamInstruction allocation = body[index + 1];
            if (body[index - 1] is not
                { Definition.Opcode: IsTaggedTuple, Operands: [_, var checkedTuple, var arity, _] }
                || projection is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, var field,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } temporary]
                }
                || !SameProjectionRegister(checkedTuple, tuple) || field.Value >= arity.Value
                || tuple.Value >= temporary.Value
                || allocation.Definition.Opcode is not (Allocate or AllocateHeap)
                || allocation.Operands[^1].Value != temporary.Value + 1
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                }
                || source != temporary || slot.Value >= allocation.Operands[0].Value
                || !(flow ??= new BeamProjectionFlow(body)).IsEntryTemporaryDead(index + 3, temporary))
            {
                continue;
            }

            // The checked tuple remains an allocation root and owns the field
            // while collection can move either term. Write the new slot afterward.
            int oldLive = (int)allocation.Operands[^1].Value;
            int live = body[index + 2].Offset - CompactRegisterCountWidth(oldLive);
            output.Write(code.Span[copied..projection.Offset]);
            output.Write(code.Span[allocation.Offset..live]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, temporary.Value);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, tuple);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, slot);
            copied = body[index + 3].Offset;
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
