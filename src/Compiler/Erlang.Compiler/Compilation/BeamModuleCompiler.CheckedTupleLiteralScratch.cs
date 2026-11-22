using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseCheckedTupleLiteralScratch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        for (int first = 0; first + 5 < body.Count; first++)
        {
            if (body[first] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } carrier, var width, _]
                }
                || body[first + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                } || tuple != carrier || scratch == carrier || field.Value >= width.Value
                || body[first + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var firstFailure, var tested, { Kind: BeamOperandParameters.Atom }]
                } || firstFailure != rejected || tested != scratch
                || body[first + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var otherTuple, var nextField, { Kind: BeamOperandParameters.X, TypeIndex: null } other]
                } || otherTuple != carrier || other == carrier || other == scratch
                    || nextField == field || nextField.Value >= width.Value
                || body[first + 4] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var secondFailure, var compared, { Kind: BeamOperandParameters.Integer }]
                } || secondFailure != rejected || compared != other)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!HasUnprotectedCheckedFailureRegion(body, first, first + 4)
                || !flow.IsBranchTemporaryDead(first + 4, scratch)
                || !flow.IsBranchTemporaryDead(first + 4, other))
            {
                continue;
            }

            // The first literal test is the last read of its projected
            // field, so the next test can use the same physical scratch.
            Replace(first + 3, 2, (int)scratch.Value);
            Replace(first + 4, 1, (int)scratch.Value);
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void Replace(int instruction, int operand, int register)
        {
            int start = body[instruction].Offset + 1;
            var cursor = new BeamCursor(code[start..]);
            for (int skip = 0; skip < operand; skip++)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            int from = start + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new(
                from,
                start + cursor.Position,
                XRegisterTag,
                register
            ));
        }
    }
}
