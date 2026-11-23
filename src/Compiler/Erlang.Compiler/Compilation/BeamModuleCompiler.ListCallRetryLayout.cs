using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? PlaceListRetryAfterCheckedCall(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNonemptyList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || RewriteListRetryAfterCheckedCall(code, body) is not { } rewritten)
        {
            return null;
        }

        return OrderFinalPhysicalLabelsCore(
            rewritten,
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        );
    }

    private static byte[]? RewriteListRetryAfterCheckedCall(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        var first = new BeamOperand(BeamOperandParameters.X, 0);
        for (int i = 3; i + 23 < body.Count; i++)
        {
            if (body[i - 2] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value: var arity }] }
                || arity != 3
                || body[i - 1] is not { Definition.Opcode: Label, Operands: [var entry] }
                || body[i] is not { Definition.Opcode: IsNonemptyList, Operands: [var empty, var list] }
                || list != first
                || body[i + 1] is not { Definition.Opcode: GetList, Operands: [var selected, var head, var tail] }
                || selected != list || head != new BeamOperand(BeamOperandParameters.X, 3)
                || tail != new BeamOperand(BeamOperandParameters.X, 4)
                || body[i + 2] is not { Definition.Opcode: IsTuple, Operands: [var fail, var tuple] }
                || tuple != head
                || body[i + 3] is not { Definition.Opcode: TestArity, Operands: [var failArity, var sameTuple, _] }
                || failArity != fail || sameTuple != head
                || fail.Value != EntryFrameFailureLabel(body, i - 1)
                || body[i + 4] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value != arity || live.Value != 5)
            {
                continue;
            }

            if (body[i + 8] is not { Definition.Opcode: GetTupleElement, Operands: [var carrier, _, var destination] }
                || carrier != head || destination != first
                || body[i + 9].Definition.Opcode != Line
                || body[i + 10] is not { Definition.Opcode: LocalCall, Operands: [var callArity, _] }
                || callArity != width
                || body[i + 11] is not { Definition.Opcode: IsTuple, Operands: [var retry, var result] }
                || !SameProjectionRegister(result, first)
                || body[i + 12] is not { Definition.Opcode: Deallocate, Operands: [var returnWidth] }
                || returnWidth != width || body[i + 13].Definition.Opcode != Return
                || body[i + 14] is not { Definition.Opcode: Label, Operands: [var emptyLabel] }
                || emptyLabel.Value != empty.Value
                || body[i + 15] is not { Definition.Opcode: IsNil, Operands: [var emptyFailure, var emptyInput] }
                || emptyFailure != fail || emptyInput != list
                || body[i + 16] is not { Definition.Opcode: Move, Operands: [var value, var nilDestination] }
                || !IsNonRegisterValue(value) || nilDestination != first
                || body[i + 17].Definition.Opcode != Return
                || body[i + 18] is not { Definition.Opcode: Label, Operands: [var retryLabel] }
                || retryLabel.Value != retry.Value
                || body[i + 22] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [var retryArity, var self, var retryWidth]
                }
                || retryArity != width || self.Value != entry.Value || retryWidth != width)
            {
                continue;
            }

            var inputBySlot = new Dictionary<BeamOperand, BeamOperand>();
            var retained = new HashSet<BeamOperand>();
            var retryInputs = new HashSet<BeamOperand>();
            for (int offset = 0; offset < 3; offset++)
            {
                if (body[i + 5 + offset] is { Definition.Opcode: Move, Operands: [var input, var slot] }
                    && (input == tail || input == new BeamOperand(BeamOperandParameters.X, 1)
                        || input == new BeamOperand(BeamOperandParameters.X, 2))
                    && slot is { Kind: BeamOperandParameters.Y, TypeIndex: null } && slot.Value >= 0 && slot.Value < 3
                    && inputBySlot.TryAdd(slot, input))
                {
                    retained.Add(input);
                }
            }

            for (int offset = 0; offset < 3; offset++)
            {
                if (body[i + 19 + offset] is { Definition.Opcode: Move, Operands: [var slot, var argument] }
                    && inputBySlot.TryGetValue(slot, out var input)
                    && argument == (input == tail ? first : input))
                {
                    retryInputs.Add(input);
                }
            }

            flow ??= new BeamProjectionFlow(body);
            if (retained.Count != 3 || !retryInputs.SetEquals(retained)
                || !flow.TryGetSingleForwardTarget(i, (int)empty.Value, out int emptyBlock) || emptyBlock != i + 14
                || !flow.TryGetSingleForwardTarget(i + 11, (int)retry.Value, out int retryBlock) || retryBlock != i + 18)
            {
                continue;
            }

            // The full frame consists of incoming copies. OTP reserves them before
            // the call and numbers them in reverse identity order. Its move scheduler
            // emits independent call copies in descending destination order.
            output.Write(code.Span[copied..body[i + 5].Offset]);
            WriteMove(
                XRegisterTag,
                4,
                YRegisterTag,
                0
            );
            WriteMove(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            WriteMove(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            output.Write(code.Span[body[i + 8].Offset..body[i + 14].Offset]);
            // Reverse postorder places the call's own retry before the earlier nil arm.
            output.Write(code.Span[body[i + 18].Offset..body[i + 19].Offset]);
            WriteMove(
                YRegisterTag,
                1,
                XRegisterTag,
                2
            );
            WriteMove(
                YRegisterTag,
                2,
                XRegisterTag,
                1
            );
            WriteMove(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            output.Write(code.Span[body[i + 22].Offset..body[i + 23].Offset]);
            output.Write(code.Span[body[i + 14].Offset..body[i + 18].Offset]);
            copied = body[i + 23].Offset;
            i += 22;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteMove(
            int sourceTag,
            int source,
            int destinationTag,
            int destination
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, sourceTag, source);
            BeamCompactWriter.Write(output, destinationTag, destination);
        }
    }
}
