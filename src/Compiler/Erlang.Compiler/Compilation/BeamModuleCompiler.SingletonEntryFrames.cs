using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceSingletonListCheckBeforeFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNil)
            || !code.Span.Contains(GetList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream();
        int copied = 0;
        int firstOperand = 0;

        for (int info = 1; info + 9 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsOne: true }] }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsNonemptyList, Operands: [var entryFailure, var input] }
                || entryFailure.Value != EntryFrameFailureLabel(body, info + 1) || input != x0
                || body[info + 3] is not { Definition.Opcode: Allocate, Operands: [var width, { Value.IsOne: true }] }
                || width.Value != 2
                || body[info + 4] is not { Definition.Opcode: GetList, Operands: [var source, var head, var tail] }
                || source != x0 || head != y1 || tail != y0
                || body[info + 5] is not { Definition.Opcode: IsNil, Operands: [var rejected, var checkedTail] }
                || checkedTail != y0 || rejected == entryFailure)
            {
                continue;
            }

            int first = info + 6;
            int end = first;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int relay = -1;
            bool valid = true;
            for (int index = first; index < end; index++)
            {
                var instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == Label && instruction.Operands[0].Value == rejected.Value)
                {
                    if (relay >= 0 || index < first + 1 || index + 2 >= end
                        || !EndsBeamBlock(body[index - 1].Definition.Opcode)
                        || body[index + 1] is not { Definition.Opcode: Deallocate, Operands: [var released] }
                        || released != width
                        || body[index + 2] is not { Definition.Opcode: Jump, Operands: [var target] }
                        || target != entryFailure)
                    {
                        valid = false;
                        break;
                    }

                    relay = index;
                }
                else if (opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter
                    or LoopReceive or Wait or WaitTimeout || instruction.Definition.Name == "debug_line"
                    || (opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame or TailDynamicApply
                        && instruction.Operands[^1] != width)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && operand.Value == rejected.Value)
                    || (opcode != InitYRegisters && instruction.Operands.Any(operand =>
                        operand.Kind == BeamOperandParameters.Y && operand.Value != y1.Value)))
                {
                    valid = false;
                    break;
                }
            }

            if (!valid || relay < 0)
            {
                continue;
            }

            // Only the head survives into the framed branch. Keep x0 intact because
            // the entry exception consumes the original argument on either failure.
            output.Write(code.Span[copied..body[info + 3].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            BeamCompactWriter.Write(output, XRegisterTag, 2);
            output.WriteByte(IsNil);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            BeamCompactWriter.Write(output, XRegisterTag, 2);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            WriteRegister(output, y0);
            copied = body[first].Offset;

            for (int index = first; index < end; index++)
            {
                var instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                int next = body[index + 1].Offset;
                if (index == relay + 1)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = next;
                    continue;
                }

                if (opcode == InitYRegisters)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (instruction.Operands.Skip(1).Any(operand => operand.Value == y1.Value))
                    {
                        WriteFrameInitialization(output, [0]);
                    }

                    copied = next;
                    continue;
                }

                if (opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame or TailDynamicApply)
                {
                    int size = next - CompactRegisterCountWidth((int)width.Value);
                    output.Write(code.Span[copied..size]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    copied = next;
                    continue;
                }

                while (firstOperand < stackOperands.Count && stackOperands[firstOperand].Start < instruction.Offset)
                {
                    firstOperand++;
                }

                while (firstOperand < stackOperands.Count && stackOperands[firstOperand].Start < next)
                {
                    var (from, to, _) = stackOperands[firstOperand++];
                    output.Write(code.Span[copied..from]);
                    WriteRegister(output, y0);
                    copied = to;
                }
            }

            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
