using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SinkCheckedTupleFieldAcrossAllocation(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(AllocateHeap)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedTupleFieldAcrossAllocation(code, body);
    }

    private static byte[]? RewriteCheckedTupleFieldAcrossAllocation(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream(code.Length);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 4 < body.Count; index++)
        {
            if (body[index - 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure, var checkedValue, var arity, _]
                }
                || body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, { Kind: BeamOperandParameters.Unsigned, Value.Sign: >= 0 } field, var projected]
                }
                || tuple != checkedValue || field.Value >= arity.Value || !IsBareX(tuple) || !IsBareX(projected)
                || body[index + 1] is not { Definition.Opcode: AllocateHeap, Operands: [_, _, var oldLive] }
                || projected.Value + 1 != oldLive.Value || projected.Value >= FirstReservedXRegister
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var savedInput, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                }
                || body[index + 3] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var result, { Value: var length }, var first, var second]
                }
                || length != 2 || !IsBareX(result) || !IsBareX(first) || !IsBareX(second)
                || (first == projected) == (second == projected) || first == result || second == result
                || !IsBareX(savedInput) || savedInput == result
                || tuple.Value >= projected.Value || savedInput.Value >= projected.Value || result.Value >= projected.Value
                || first.Value > projected.Value || second.Value > projected.Value)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(index - 1, (int)failure.Value, out int rejected)
                || !HasUnprotectedCheckedFailureRegion(body, index - 1, rejected)
                || !flow.IsEntryTemporaryDead(index + 4, projected))
            {
                continue;
            }

            // The collector keeps the already checked tuple in its original
            // root. The old field temporary has no use after construction.
            output.Write(code.Span[copied..body[index].Offset]);
            WriteHeapRoots(
                output,
                code,
                body[index + 1].Offset,
                body[index + 2].Offset,
                (int)oldLive.Value,
                (int)projected.Value
            );
            output.Write(code.Span[body[index + 2].Offset..body[index + 3].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, tuple);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, result);
            output.WriteByte(PutTuple);
            WriteRegister(output, result);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, length);
            WriteRegister(output, first == projected ? result : first);
            WriteRegister(output, second == projected ? result : second);

            copied = body[index + 4].Offset;
            index += 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
