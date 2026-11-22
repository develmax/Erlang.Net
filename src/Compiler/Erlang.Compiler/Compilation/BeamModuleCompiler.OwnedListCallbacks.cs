using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TypeOwnedListCallback(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || closures.Count == 0
            || !code.Span.Contains(CallFun) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int entry = 0; entry + 12 < body.Count; entry++)
        {
            if (ReadOwnedListCallbackTarget(body, entry) is not { } closure)
            {
                continue;
            }

            int list = entry + 2;
            int afterCall = entry + 8;
            output.Write(code.Span[copied..body[list].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            output.Write(code.Span[body[entry + 5].Offset..body[entry + 6].Offset]);
            output.WriteByte(GetList);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, YRegisterTag, 0);

            // beam_ssa_codegen's known-target call_fun2 registers the same lambda
            // as make_fun3. beam_asm resolves both through the lambda table.
            output.WriteByte(CallFunTyped);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, closure);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((FunTypeBits, null, null, null)));
            copied = body[afterCall].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, CallFunTyped);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? ScheduleSavedTupleCallbackArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || closures.Count == 0
            || !code.Span.Contains(ExternalCall) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand x2 = new(BeamOperandParameters.X, 2);
        var closureEntries = closures.Select(closure => MapClosureEntryLabel(closure.Entry)).ToHashSet();
        using var output = new MemoryStream();
        int copied = 0;
        for (int entry = 0; entry + 11 < body.Count; entry++)
        {
            if (body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                || !closureEntries.Contains((int)label.Value)
                || body[entry + 1] is not { Definition.Opcode: IsTuple, Operands: [var failure, var tuple] }
                || body[entry + 2] is not { Definition.Opcode: TestArity, Operands: [var sizeFailure, var sized, var size] }
                || failure != sizeFailure || tuple != x0 || sized != x0 || size.Value != 2
                || body[entry + 3] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value != 3 || live.Value != 3
                || body[entry + 4] is not
                { Definition.Opcode: Move, Operands: [var incoming, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved] }
                || incoming != x1
                || body[entry + 5] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var firstSource, var firstField,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } first]
                }
                || firstSource != x0 || !firstField.Value.IsZero || first == saved
                || body[entry + 6] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var secondSource, var secondField,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } second]
                }
                || secondSource != x0 || !secondField.Value.IsOne || second == first || second == saved
                || !IsMove(body[entry + 7], first, x1) || !IsMove(body[entry + 8], saved, x0))
            {
                continue;
            }

            int call = FirstInstructionAfterLines(entry + 9);
            if (call + 4 >= body.Count || body[call] is not
                { Definition.Opcode: ExternalCall, Operands: [{ Value: var arity }, _] }
                || arity != 3 || !IsMove(body[call + 1], first, x1)
                || !IsMove(body[call + 2], second, x2) || !IsMove(body[call + 3], saved, x0))
            {
                continue;
            }

            int tail = FirstInstructionAfterLines(call + 4);
            if (tail >= body.Count || body[tail] is not
                { Definition.Opcode: TailExternalCallWithFrame, Operands: [{ Value: var tailArity }, _, var released] }
                || tailArity != arity || released != width)
            {
                continue;
            }

            // The checked projections only write stack registers. Use the live
            // incoming X before loading the same outgoing X with the first field.
            output.Write(code.Span[copied..body[entry + 7].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, x1);
            WriteRegister(output, x0);
            output.Write(code.Span[body[entry + 7].Offset..body[entry + 8].Offset]);
            output.Write(code.Span[body[entry + 9].Offset..body[call + 1].Offset]);
            int[] order = [call + 1, call + 2, call + 3];
            SortOutgoingStackLoads(body, order);
            foreach (int move in order)
            {
                output.Write(code.Span[body[move].Offset..body[move + 1].Offset]);
            }

            copied = body[call + 4].Offset;
            entry = tail;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int FirstInstructionAfterLines(int start)
        {
            while (start < body.Count && body[start].Definition.Opcode == Line)
            {
                start++;
            }

            return start;
        }
    }

    private int? ReadOwnedListCallbackTarget(IReadOnlyList<BeamInstruction> body, int entry)
    {
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        BeamOperand y1 = new(BeamOperandParameters.Y, 1);
        if (entry == 0 || body[entry - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value: var arity }] }
            || arity != 2 || body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
            || body[entry + 1] is not
            { Definition.Opcode: IsNonemptyList, Operands: [var empty, var checkedList] }
            || checkedList != x0 || body[entry + 2] is not
            { Definition.Opcode: GetList, Operands: [var source, var head, var tail] }
            || source != x0 || head != new BeamOperand(BeamOperandParameters.X, 2)
            || tail != new BeamOperand(BeamOperandParameters.X, 3)
            || body[entry + 3] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, { Value: var live }] }
            || width != 2 || live != 4 || !IsMove(body[entry + 4], tail, y0) || !IsMove(body[entry + 5], x1, y1)
            || !IsMove(body[entry + 6], head, x0)
            || body[entry + 7] is not { Definition.Opcode: CallFun, Operands: [{ Value.IsOne: true }] }
            || !IsMove(body[entry + 8], y1, x1) || !IsMove(body[entry + 9], y0, x0)
            || body[entry + 10] is not
            { Definition.Opcode: TailLocalCallWithFrame, Operands: [{ Value: var recurArity }, var recur, var release] }
            || recurArity != 2 || recur.Value != label.Value || release.Value != width
            || body[entry + 11] is not { Definition.Opcode: Label, Operands: [var emptyLabel] }
            || emptyLabel.Value != empty.Value || body[entry + 12].Definition.Opcode != Return
            || exports.Any(item => MapEmittedLabel(item.Entry) == label.Value)
            || closures.Any(item => MapClosureEntryLabel(item.Entry) == label.Value))
        {
            return null;
        }

        int caller = -1;
        for (int index = 0; index < body.Count; index++)
        {
            if (index == entry + 10 || !body[index].Operands.Any(operand => operand.Value == label.Value
                && operand.Kind is BeamOperandParameters.Label or BeamOperandParameters.Failure))
            {
                continue;
            }

            if (caller >= 0 || body[index] is not
                { Definition.Opcode: LocalCall, Operands: [{ Value: var count }, var target] }
                || count != 2 || target.Value != label.Value)
            {
                return null;
            }

            caller = index;
        }

        if (caller <= 0)
        {
            return null;
        }

        for (int index = caller - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: MakeFun, Operands: [var lambda, var destination, var count, ..] }
                && destination == x1 && lambda.Value < closures.Count
                && closures[(int)lambda.Value] is { } owned
                && count.Value == owned.CaptureCount && owned.Arity == owned.CaptureCount + 1)
            {
                return (int)lambda.Value;
            }

            if (instruction.Definition.Opcode is Line or TrimFrame
                || instruction is { Definition.Opcode: Move, Operands: [_, var target] }
                    && (target == x0 || target.Kind == BeamOperandParameters.Y))
            {
                continue;
            }

            break;
        }

        return null;
    }
}
