using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepTupleCarrierAcrossTagTest(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && code.Span.Contains(ExactEqualTest)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? RewriteTupleTagScratchSlots(code, decoded.Body)
            : null;

    private static byte[]? RewriteTupleTagScratchSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } carrier,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                } || carrier == scratch
                || body[index + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                        var sourceTuple,
                        var tagField,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tagDestination]
                } || sourceTuple != scratch)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (tagDestination == scratch || flow.IsBranchTemporaryDead(index + 1, scratch))
            {
                output.Write(code.Span[copied..body[index].Offset]);
                WriteField(carrier, tagField, tagDestination);
                copied = body[index + 2].Offset;
                index++;

                continue;
            }

            if (tagDestination != carrier || index + 4 >= body.Count || body[index + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } failure,
                        var testedTag,
                    { Kind: BeamOperandParameters.Atom } tag]
                } || testedTag != carrier
                || body[index + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var valueTuple, var valueField, var valueDestination]
                } || valueTuple != scratch || valueDestination != carrier)
            {
                continue;
            }

            // The fallback cannot observe the exchanged roles. The successful
            // projection restores the original destination and ends the carrier's use.
            if (!flow.IsGuardTemporaryDeadOnFailure(index + 2, carrier, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, scratch, preserveHeapRoots: true)
                || !flow.IsBranchTemporaryDead(index + 3, scratch))
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            WriteField(carrier, tagField, scratch);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteRegister(output, scratch);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteField(carrier, valueField, carrier);
            copied = body[index + 4].Offset;
            index += 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteField(BeamOperand tuple, BeamOperand field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, tuple);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, destination);
        }
    }
}
