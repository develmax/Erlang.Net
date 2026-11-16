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
            var used = new HashSet<int>();
            bool leaf = true;
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is TrimFrame or CatchEnter or TryEnter || instruction.Definition.Name == "debug_line")
                {
                    frameSize = -1;
                    break;
                }

                leaf &= IsFramelessInstruction(instruction);
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
                    for (int operandIndex = 0; operandIndex < instruction.Operands.Count; operandIndex++)
                    {
                        BeamOperand operand = instruction.Operands[operandIndex];
                        if (operand.Kind == BeamOperandParameters.Y
                            && !(opcode == Move && operandIndex == 1 && instruction.Operands[0].Kind == BeamOperandParameters.X))
                        {
                            used.Add((int)operand.Value);
                        }
                    }
                }
            }

            if (frameSize < 0)
            {
                continue;
            }

            int usedSlots = used.Count == 0 ? 0 : used.Max() + 1;
            bool removeFrame = leaf && usedSlots == 0;
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if ((removeFrame && opcode is (Allocate or Deallocate))
                    || (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                        && source.Kind == BeamOperandParameters.X && destination.Kind == BeamOperandParameters.Y
                        && destination.Value >= usedSlots))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                    continue;
                }

                if (removeFrame && opcode == AllocateHeap)
                {
                    var frameOperand = new BeamCursor(code[(instruction.Offset + 1)..]);
                    _ = BeamCompactReader.Read(frameOperand);
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(TestHeap);
                    output.Write(code.Span[(instruction.Offset + 1 + frameOperand.Position)..body[index + 1].Offset]);
                    copied = body[index + 1].Offset;
                    continue;
                }

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
                if (usedSlots == frameSize || operandIndex < 0 || instruction.Operands[operandIndex].Value != frameSize)
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

    private static bool IsFramelessInstruction(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Label or Line or Allocate or AllocateHeap or Deallocate or Return
            or InitYRegisters or Move or Swap or TestHeap or PutTuple or PutList or GetTupleElement
            or GetList or GetHead or GetTail or Jump or SelectValue or SelectTupleArity
            or BadMatch or BadRecord or CaseEnd or IfEnd
        || IsTrimTest(instruction.Definition.Opcode);
}
