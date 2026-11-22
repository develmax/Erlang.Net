using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedTaggedTupleGuardRetries(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } firstFailure, var selector, var width, var tag]
                })
            {
                continue;
            }

            int guardIndex = index + 1;
            while (guardIndex < body.Count)
            {
                BeamInstruction prefix = body[guardIndex];
                if (prefix.Definition.Opcode != Line && (prefix is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, var field, var destination]
                    } || !SameProjectionRegister(selector, source) || SameProjectionRegister(selector, destination)
                    || field.Value.Sign < 0 || field.Value >= width.Value))
                {
                    break;
                }

                guardIndex++;
            }

            if (guardIndex >= body.Count || body[guardIndex] is not
                { Definition.Opcode: ExactEqualTest or ExactNotEqualTest, Operands: [var retryLabel, _, _] }
                || retryLabel != firstFailure
                || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                    index,
                    guardIndex,
                    (int)retryLabel.Value,
                    out int retry
                )
                || retry + 2 >= body.Count || body[retry + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } finalFailure, var other, var otherWidth, var otherTag]
                }
                || !SameProjectionRegister(selector, other) || width != otherWidth || tag != otherTag
                || finalFailure == firstFailure
                || Enumerable.Range(guardIndex + 1, retry - guardIndex)
                    .Any(position => body[position].Definition.Opcode is FunctionInfo or EndCode))
            {
                continue;
            }

            // The rejected tag would fail both tests. The guard retry instead
            // enters with the previously checked tuple in the same register.
            int operand = body[index].Offset + 1;
            var cursor = new BeamCursor(code[operand..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..operand]);
            BeamCompactWriter.Write(output, LabelTag, finalFailure.Value);
            output.Write(code.Span[(operand + cursor.Position)..body[retry + 1].Offset]);
            copied = body[retry + 2].Offset;
            index = retry + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
