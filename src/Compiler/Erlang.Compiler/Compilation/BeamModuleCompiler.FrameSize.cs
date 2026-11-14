using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveUnusedTrailingFrameSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int functionStart = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = functionStart;
            functionStart = end + 1;
            int frameSize = -1;
            int usedSlots = 0;
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is TrimFrame or CatchEnter or TryEnter || instruction.Definition.Name == "debug_line")
                {
                    frameSize = -1;
                    break;
                }

                if (opcode is Allocate or AllocateHeap)
                {
                    if (frameSize >= 0)
                    {
                        frameSize = -1;
                        break;
                    }

                    frameSize = (int)instruction.Operands[0].Value;
                }
                else if (opcode != InitYRegisters)
                {
                    foreach (BeamOperand operand in instruction.Operands)
                    {
                        if (operand.Kind == BeamOperandParameters.Y)
                        {
                            usedSlots = Math.Max(usedSlots, checked((int)operand.Value + 1));
                        }
                    }
                }
            }

            if (usedSlots >= frameSize)
            {
                continue;
            }

            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == InitYRegisters)
                {
                    BeamOperand[] kept = instruction.Operands.Skip(1).Where(slot => slot.Value < usedSlots).ToArray();
                    if (kept.Length == (int)instruction.Operands[0].Value)
                    {
                        continue;
                    }

                    output.Write(code.Span[copied..instruction.Offset]);
                    if (kept.Length != 0)
                    {
                        output.WriteByte(InitYRegisters);
                        output.WriteByte(ExtendedListOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept.Length);
                        foreach (BeamOperand slot in kept)
                        {
                            WriteRegister(output, slot);
                        }
                    }

                    copied = body[index + 1].Offset;
                    continue;
                }

                int operandIndex = opcode switch
                {
                    Allocate or AllocateHeap or Deallocate => 0,
                    TailDynamicApply => 1,
                    TailLocalCallWithFrame or TailExternalCallWithFrame => 2,
                    _ => -1
                };
                if (operandIndex < 0 || instruction.Operands[operandIndex].Value != frameSize)
                {
                    continue;
                }

                int operandStart = instruction.Offset + 1;
                var cursor = new BeamCursor(code[operandStart..]);
                for (int operand = 0; operand < operandIndex; operand++)
                {
                    _ = BeamCompactReader.Read(cursor);
                }

                output.Write(code.Span[copied..(operandStart + cursor.Position)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, usedSlots);
                _ = BeamCompactReader.Read(cursor);
                copied = operandStart + cursor.Position;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
