using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ShareRepeatedTupleTagFailure(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        BeamProjectionFlow flow,
        int predicate
    )
    {
        if (body[predicate] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var retry, var compared, { Kind: BeamOperandParameters.Atom } tag]
            }
            || body[predicate - 1].Operands is not
            [
            { Kind: BeamOperandParameters.X, TypeIndex: null } carrier, var field,
            { Kind: BeamOperandParameters.X, TypeIndex: null } projected
            ]
            || carrier == projected || compared != projected
            || !flow.TryGetForwardLabel(predicate, (int)retry.Value, out int fallback)
            || fallback <= predicate + 2 || fallback + 3 >= body.Count
            || !EndsBeamBlock(body[fallback - 1].Definition.Opcode)
            || body[fallback + 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var retryCarrier, var retryField, var retryProjection]
            }
            || retryCarrier != carrier || retryField != field || retryProjection != projected
            || body[fallback + 2] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var reject, var retryCompared, var retryTag]
            }
            || reject == retry || retryCompared != projected || retryTag != tag
            || body[fallback + 3] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var followingCarrier, var followingField, var followingProjection]
            }
            || followingCarrier != carrier || followingField == field || followingProjection != projected)
        {
            return null;
        }

        var first = body[predicate];
        var incoming = labelOperands.Where(operand => operand.Label == retry.Value).ToArray();
        if (incoming.Length < 2 || incoming[0].Start != first.Offset + 1
            || incoming[^1].Start >= body[fallback].Offset)
        {
            return null;
        }

        int edge = 1;
        for (int index = predicate + 1; edge < incoming.Length; index++)
        {
            var instruction = body[index];
            if (!PreservesTupleCarrier(instruction, carrier))
            {
                return null;
            }

            if (incoming[edge].Start < body[index + 1].Offset)
            {
                if (!IsEntryFramePredicate(instruction.Definition.Opcode)
                    || incoming[edge].Start != instruction.Offset + 1)
                {
                    return null;
                }

                edge++;
            }
        }

        // The first failed predicate already has the rejected tag in the same X register.
        // Every other predecessor still knows the tag. The next projection overwrites its scratch.
        return ApplyDuplicatedFrameReadEdits(
            code,
            [
            new(
                incoming[0].Start,
                incoming[0].End,
                LabelTag,
                (int)reject.Value
            ),
            new(
                body[fallback + 1].Offset,
                body[fallback + 3].Offset,
                0,
                0,
                IsRemoval: true
            )
        ]
        );
    }

    private static bool PreservesTupleCarrier(BeamInstruction instruction, BeamOperand carrier)
    {
        var operands = instruction.Operands;

        bool Preserved(BeamOperand operand) => !SameProjectionRegister(operand, carrier);

        return instruction.Definition.Opcode switch
        {
            GetTupleElement or GetHead or GetTail or Move or PutList => Preserved(operands[^1]),
            GetList or Swap => Preserved(operands[^1]) && Preserved(operands[^2]),
            PutTuple => Preserved(operands[0]),
            TestHeap => carrier.Value < operands[^1].Value,
            Line => true,
            byte opcode => IsEntryFramePredicate(opcode)
        };
    }
}
