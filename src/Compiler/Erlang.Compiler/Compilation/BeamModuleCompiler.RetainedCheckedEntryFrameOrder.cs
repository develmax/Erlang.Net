using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderRetainedCheckedEntryFrameSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arguments] }
                || arguments.Value < 2 || arguments.Value >= BeamOperandParameters.MaximumRegister
                || ReadRetainedCheckedEntryFrame(body, info, (int)arguments.Value) is not { } layout)
            {
                continue;
            }

            var (frame, end, width, firstRegister) = layout;
            int saveEnd = frame + 1 + width;
            output.Write(code.Span[copied..body[frame + 1].Offset]);
            for (int slot = 0; slot < width; slot++)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, firstRegister + width - 1 - slot);
                BeamCompactWriter.Write(output, YRegisterTag, slot);
            }

            copied = body[saveEnd].Offset;
            int operand = FirstStackOperandAtOrAfter(stackOperands, copied);
            for (int index = saveEnd; index < end; index++)
            {
                var instruction = body[index];
                int limit = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    WriteFrameInitialization(
                        output,
                        instruction.Operands.Skip(1).Select(slot => Reverse((int)slot.Value)).Order().ToArray()
                    );
                    copied = limit;
                    operand = FirstStackOperandAtOrAfter(stackOperands, copied);
                    continue;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < limit)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, Reverse(slot));
                    copied = to;
                }
            }

            info = end - 1;

            int Reverse(int slot) => width - 1 - slot;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Frame, int End, int Width, int FirstRegister)? ReadRetainedCheckedEntryFrame(
        IReadOnlyList<BeamInstruction> body,
        int info,
        int arguments
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var projected = new BeamOperand(BeamOperandParameters.X, arguments);
        int frame = info + 6;
        if (body[info + 1].Definition.Opcode != Label
            || body[info + 2] is not { Definition.Opcode: IsTaggedTuple, Operands: [var reject, var input, var arity, _] }
            || input != x0 || body[info + 3] is not
            {
                Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, var name]
            } || tuple != x0 || name != projected || field.Value >= arity.Value
            || body[info + 4] is not
            {
                Definition.Opcode: GetTupleElement, Operands: [var otherTuple, var testField, var tested]
            } || otherTuple != x0 || testField.Value >= arity.Value || testField == field
            || tested != x0 && tested != new BeamOperand(BeamOperandParameters.X, arguments + 1)
            || body[info + 5] is not
            {
                Definition.Opcode: ExactEqualTest, Operands: [var retry, var compared, { Kind: BeamOperandParameters.Integer }]
            } || compared != tested || retry == reject
            || body[frame] is not { Definition.Opcode: Allocate, Operands: [var slots, var live] }
            || live.Value != arguments + 1)
        {
            return null;
        }

        int firstRegister = tested == x0 ? 1 : 0;
        int width = arguments + 1 - firstRegister;
        if (slots.Value != width || frame + width + 2 >= body.Count)
        {
            return null;
        }

        // The only derived value saved here comes after the original
        // argument identities. OTP reserves that interval after the
        // copied arguments, then turn_yregs reverses the physical slots.
        for (int slot = 0; slot < width; slot++)
        {
            if (!IsMove(frame + 1 + slot,
                new(BeamOperandParameters.X, firstRegister + slot),
                new(BeamOperandParameters.Y, slot)))
            {
                return null;
            }
        }

        int end = frame + width + 1;
        while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        int setup = frame + width + 1;
        int firstCall = SkipLines(setup + 1, end);
        if (!IsMove(setup, projected, x0) || firstCall >= end || !IsReturningCall(firstCall, 1)
            || !HasRetainedOutgoingCall(firstCall + 1, end, width)
            || ReadDisjointFrameOwnership(
                body,
                info + 1,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } ownership
            || ownership.Uses.Count != 1 || !ownership.Uses.TryGetValue(frame, out bool[]? used) || used.Any(slot => !slot))
        {
            return null;
        }

        return (frame, end, width, firstRegister);

        bool IsMove(int index, BeamOperand source, BeamOperand destination) =>
            body[index] is { Definition.Opcode: Move, Operands: [var input, var target] }
            && input == source && target == destination;

        bool IsReturningCall(int index, int count) => body[index] is
        { Definition.Opcode: LocalCall or ExternalCall, Operands: [var passed, _] } && passed.Value == count;

        int SkipLines(int index, int limit)
        {
            while (index < limit && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool HasRetainedOutgoingCall(int start, int limit, int frameSize)
        {
            var x1 = new BeamOperand(BeamOperandParameters.X, 1);
            var x2 = new BeamOperand(BeamOperandParameters.X, 2);
            bool pair = start + 1 < limit
                && body[start] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                && body[start + 1] is { Definition.Opcode: PutTuple, Operands: [var target, _, _, _] }
                && target == x1;
            int move = start + (pair ? 2 : 0);
            if (move + (pair ? 4 : 3) >= limit || body[move] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } expired, var outgoing]
                } || expired.Value >= frameSize || outgoing != (pair ? x2 : x1) || !IsMove(move + 1, x0, expired))
            {
                return false;
            }

            int after;
            if (pair)
            {
                if (!IsMove(move + 2, x1, x0) || !IsMove(move + 3, x2, x1))
                {
                    return false;
                }

                after = move + 4;
                if (body[after].Definition.Opcode == InitYRegisters)
                {
                    after++;
                }
            }
            else
            {
                if (body[move + 2] is not
                    { Definition.Opcode: Swap, Operands: [var left, var right] } || left != x0 || right != x1)
                {
                    return false;
                }

                after = move + 3;
            }

            int call = SkipLines(after, limit);

            return call + 1 < limit && IsReturningCall(call, 2)
                && body[call + 1].Definition.Opcode is ExactEqualTest or SelectValue;
        }
    }
}
