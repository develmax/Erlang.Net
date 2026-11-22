using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleSiblingListProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !(code.Span.Contains(GetList) || code.Span.Contains(GetTail))
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteSiblingListProjections(code, body);
    }

    private static byte[]? RewriteSiblingListProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int test = 1; test + 3 < body.Count; test++)
        {
            if (body[test - 1] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var firstCarrier, var firstIndex, var firstField]
                }
                || body[test] is not
                {
                    Definition.Opcode: IsNonemptyList, Operands: [var fail, var checkedField]
                } || !SameProjectionRegister(checkedField, firstField)
                || body[test + 1] is not
                {
                    Definition.Opcode: GetList or GetTail, Operands: [var list, ..]
                } split || !SameProjectionRegister(list, firstField)
                || body[test + 2] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var nextCarrier, var nextIndex, var nextField]
                } || !SameProjectionRegister(firstCarrier, nextCarrier) || firstIndex.Value >= nextIndex.Value
                || SameProjectionRegister(list, nextField)
                || split.Operands.Skip(1).Any(destination =>
                    SameProjectionRegister(destination, nextCarrier) || SameProjectionRegister(destination, nextField))
                || body[test + 3] is not { Definition.Opcode: IsNonemptyList, Operands: [var nextFail, var nextList] }
                || fail != nextFail || !SameProjectionRegister(nextField, nextList))
            {
                continue;
            }

            // Both projections stay on the first successful list-test edge.
            // Their writes are independent of the other projection's inputs and destinations.
            int first = body[test + 1].Offset;
            int second = body[test + 2].Offset;
            int after = body[test + 3].Offset;
            output.Write(code.Span[copied..first]);
            output.Write(code.Span[second..after]);
            output.Write(code.Span[first..second]);
            copied = after;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
