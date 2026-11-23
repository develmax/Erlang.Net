using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseCheckedRetryArityCarrier(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var carrier = new BeamOperand(BeamOperandParameters.X, 0);
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        for (int first = 0; first + 6 < body.Count; first++)
        {
            if (body[first] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var tested, var width, _]
                } || tested != carrier
                || body[first + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } name]
                } || tuple != carrier || name == carrier || field.Value >= width.Value
                || body[first + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var sameTuple, var otherField, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                } || sameTuple != carrier || scratch == name || scratch == carrier
                    || otherField == field || otherField.Value >= width.Value
                || body[first + 3] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } otherwise, var checkedField,
                    { Kind: BeamOperandParameters.Integer } expected]
                } || checkedField != scratch || otherwise == rejected || body[first + 4].Definition.Opcode != Allocate)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(first + 3, (int)otherwise.Value, out int retry)
                || retry <= first + 5 || retry + 2 >= body.Count
                || body[retry + 1] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var nameFailure, var comparedName, { Kind: BeamOperandParameters.Atom }]
                } || nameFailure != rejected || comparedName != name
                || body[retry + 2] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var fieldFailure, var comparedField, { Kind: BeamOperandParameters.Integer } other]
                } || fieldFailure != rejected || comparedField != scratch || other == expected
                || !HasUnprotectedCheckedFailureRegion(body, first, retry + 2)
                || !flow.IsEntryTemporaryDead(first + 4, scratch)
                || !flow.IsEntryTemporaryDead(first + 5, carrier)
                || !flow.IsEntryTemporaryDead(retry + 1, carrier)
                || !flow.IsBranchTemporaryDead(retry + 2, scratch))
            {
                continue;
            }

            // On the successful edge, allocation can root the projected
            // integer in X0. Neither continuation reads the old tuple.
            ReplaceRegister(first + 2, 2);
            ReplaceRegister(first + 3, 1);
            ReplaceRegister(retry + 2, 1);
            first = retry + 2;
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void ReplaceRegister(int index, int operand)
        {
            int start = body[index].Offset + 1;
            var cursor = new BeamCursor(code[start..]);
            for (int before = 0; before < operand; before++)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            int from = start + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new(
                from,
                start + cursor.Position,
                XRegisterTag,
                0
            ));
        }
    }
}
