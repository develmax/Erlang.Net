using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseTerminalCheckedTupleField(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || !code.Span.Contains(TestArity) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteTerminalCheckedTupleField(code, body);
    }

    private static byte[]? RewriteTerminalCheckedTupleField(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        bool unprotected = false;
        for (int at = 0; at + 6 < body.Count; at++)
        {
            if (body[at].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, at + 1);
                continue;
            }

            if (!unprotected || body[at] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var checkedTuple]
                }
                || body[at + 1] is not { Definition.Opcode: TestArity, Operands: [var arityFail, var carrier, var arity] }
                || arityFail != failed || carrier != checkedTuple
                || carrier is not { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: false }
                || body[at + 2] is not { Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, var scratch] }
                || tuple != carrier || field.Value >= arity.Value || scratch == carrier
                || scratch is not { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: false }
                || body[at + 3] is not { Definition.Opcode: ExactEqualTest, Operands: [var reject, var compared, var expected] }
                || reject != failed || compared != scratch || expected is not { Kind: BeamOperandParameters.X }
                || SameProjectionRegister(expected, carrier) || SameProjectionRegister(expected, scratch)
                || body[at + 4] is not { Definition.Opcode: TestHeap, Operands: [_, var live] }
                || carrier.Value < live.Value || scratch.Value < live.Value
                || body[at + 5] is not { Definition.Opcode: UpdateRecord } update
                || update.Operands.Any(operand =>
                    SameProjectionRegister(operand, carrier) || SameProjectionRegister(operand, scratch))
                || body[at + 6].Definition.Opcode != Return)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int compare = at + 3;
            if (!flow.TryGetForwardLabel(compare, (int)failed.Value, out int fallback)
                || fallback + 2 >= body.Count
                || body[fallback + 1].Definition.Opcode != Move || body[fallback + 2].Definition.Opcode != Return
                || !flow.IsGuardTemporaryDeadOnFailure(compare, carrier, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(compare, scratch, preserveHeapRoots: true))
            {
                continue;
            }

            // Neither outcome can observe the old tuple or the scratch value.
            // The projection can use the retired carrier as its destination.
            int projection = body[at + 2].Offset + 1;
            var readProjection = new BeamCursor(code[projection..]);
            _ = BeamCompactReader.Read(readProjection);
            _ = BeamCompactReader.Read(readProjection);
            edits.Add(new CodeEdit(
                projection + readProjection.Position,
                body[compare].Offset,
                XRegisterTag,
                (int)carrier.Value
            ));

            int test = body[compare].Offset + 1;
            var readTest = new BeamCursor(code[test..]);
            _ = BeamCompactReader.Read(readTest);
            int argument = test + readTest.Position;
            _ = BeamCompactReader.Read(readTest);
            edits.Add(new CodeEdit(
                argument,
                test + readTest.Position,
                XRegisterTag,
                (int)carrier.Value
            ));
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }
}
