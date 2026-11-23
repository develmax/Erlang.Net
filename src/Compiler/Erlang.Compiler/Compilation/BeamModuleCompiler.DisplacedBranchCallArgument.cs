using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseDisplacedBranchCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int frame = FindLinearFrameSuffix(body, start, end);
            if (frame < 0 || body[frame] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value: var width }, _]
                }
                || width <= 1 || width > BeamOperandParameters.MaximumRegister
                || ReadDisplacedBranchCallArgument(
                    body,
                    start,
                    end,
                    frame,
                    (int)width - 1
                ) is not { } plan)
            {
                continue;
            }

            var (store, retired) = plan;
            int saved = (int)width - 1;
            int operand = FirstStackOperandAtOrAfter(stackOperands, body[frame].Offset);
            for (int index = frame; index < end; index++)
            {
                var instruction = body[index];
                if (index == store)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    var x0 = new BeamOperand(BeamOperandParameters.X, 0);
                    var x1 = new BeamOperand(BeamOperandParameters.X, 1);
                    var slot = new BeamOperand(BeamOperandParameters.Y, retired);

                    // The old argument must leave Y before the result overwrites it.
                    EmitBinaryRegisterInstruction(Move, slot, x1);
                    EmitBinaryRegisterInstruction(Move, x0, slot);
                    EmitBinaryRegisterInstruction(Swap, x0, x1);
                    index += 3;
                    copied = body[index + 1].Offset;

                    continue;
                }

                if (LinearFrameSizeOperand(instruction.Definition.Opcode) is >= 0 and var size)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        instruction.Offset,
                        copied,
                        size,
                        saved
                    );

                    continue;
                }

                int next = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    int[] clears = instruction.Operands.Skip(1)
                        .Where(slot => index > store || slot.Value != saved)
                        .Select(slot => slot.Value == saved ? retired : (int)slot.Value)
                        .Order().ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    WriteFrameInitialization(output, clears);
                    copied = next;

                    continue;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    if (slot == saved)
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, retired);
                        copied = to;
                    }
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void EmitBinaryRegisterInstruction(byte opcode, BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(opcode);
            WriteRegister(output, source);
            WriteRegister(output, destination);
        }
    }

    private static (int Store, int Retired)? ReadDisplacedBranchCallArgument(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame,
        int saved
    )
    {
        var slot = new BeamOperand(BeamOperandParameters.Y, saved);
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        for (int store = frame + 1; store + 4 < end; store++)
        {
            if (store > frame + 1 && body[store - 1].Definition.Opcode is LocalCall or ExternalCall
                && body[store] is { Definition.Opcode: Move, Operands: [var returned, var destination] }
                && returned == x0 && destination == slot
                && body[store + 1] is { Definition.Opcode: Move, Operands: [var source, var nextArgument] }
                && source == slot && nextArgument == x1
                && body[store + 2] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retired, var firstArgument]
                }
                && retired.Value < saved && firstArgument == x0
                && body[store + 3] is { Definition.Opcode: InitYRegisters, Operands: [{ Value.IsOne: true }, var cleared] }
                && cleared == retired)
            {
                int call = store + 4;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value: var arity }, _]
                    }
                    || arity != 2 || !CanReuseDisplacedBranchSlot(
                        body,
                        start,
                        end,
                        frame,
                        call,
                        slot,
                        retired
                    ))
                {
                    return null;
                }

                return (store, (int)retired.Value);
            }

            var instruction = body[store];
            if (instruction.Definition.Opcode is not
                    (Move or Swap or Line or InitYRegisters or GetTupleElement or LocalCall or ExternalCall
                    or TestHeap or PutTuple or PutList or MakeFun)
                || instruction.Definition.Opcode != InitYRegisters
                    && instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
            {
                return null;
            }
        }

        return null;
    }

    private static bool CanReuseDisplacedBranchSlot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame,
        int call,
        BeamOperand saved,
        BeamOperand retired
    )
    {
        if (ReadDisjointFrameOwnership(
            body,
            start,
            end,
            trackSiblingTrimEpochs: true
        ) is not { } owned
            || owned.Uses.Count != 1 || !owned.Uses.ContainsKey(frame)
            || owned.Owners[call - start] != frame)
        {
            return false;
        }

        bool retainsResult = false;
        for (int index = frame + 1; index < end; index++)
        {
            var instruction = body[index];
            bool hasSlots = instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y);
            if ((hasSlots || LinearFrameSizeOperand(instruction.Definition.Opcode) >= 0)
                && owned.Owners[index - start] != frame)
            {
                return false;
            }

            if (index <= call)
            {
                continue;
            }

            if (instruction.Operands.Any(operand => SameProjectionRegister(operand, retired)))
            {
                return false;
            }

            retainsResult |= instruction.Definition.Opcode != InitYRegisters
                && instruction.Operands.Any(operand => SameProjectionRegister(operand, saved));
        }

        return retainsResult;
    }
}
