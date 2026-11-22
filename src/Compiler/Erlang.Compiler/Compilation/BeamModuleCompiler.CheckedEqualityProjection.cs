using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FinishCheckedEqualityProjection(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ReuseCheckedEqualityProjection(code, labelCount) is not { } checkedEquality)
        {
            return null;
        }

        return ForwardCheckedPayloadCallArgument(checkedEquality, labelCount) ?? checkedEquality;
    }

    private byte[]? ReuseCheckedEqualityProjection(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactEqualTest)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedEqualityProjection(code, body);
    }

    private static byte[]? RewriteCheckedEqualityProjection(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var carrier = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 4 < body.Count; index++)
        {
            if (body[index - 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var checkedTuple, var arity, _]
                } || checkedTuple != carrier
                || body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands:
                    [
                        var tuple,
                        var field,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch
                    ]
                } || tuple != carrier || scratch == carrier || field.Value >= arity.Value || body[index].Offset < copied
                || body[index + 1] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var compared, var expected]
                } || compared != scratch || expected.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(index + 1, (int)rejected.Value, out int failed)
                || failed <= index + 2 || failed + 2 >= body.Count
                || body[failed + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var repeatedTuple, var repeatedField, var destination]
                } || repeatedTuple != carrier || repeatedField != field || destination != carrier
                || !flow.IsEntryTemporaryDead(index + 2, carrier)
                || !flow.IsEntryTemporaryDead(index + 2, scratch)
                || !flow.IsEntryTemporaryDead(failed + 2, scratch))
            {
                continue;
            }

            // The failed equality edge owns the repeated field read. The
            // success edge discards the old tuple before observing X0.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, carrier);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, carrier);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, carrier);
            BeamCompactWriter.Write(
                output,
                expected.Kind == BeamOperandParameters.Atom ? AtomTag : BeamWire.IntegerTag,
                expected.Value
            );
            output.Write(code.Span[body[index + 2].Offset..body[failed + 1].Offset]);
            copied = body[failed + 2].Offset;
            index = failed + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
