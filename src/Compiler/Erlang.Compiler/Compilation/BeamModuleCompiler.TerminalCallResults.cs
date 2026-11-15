using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? KeepTerminalCallResultsInX0(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int stackIndex = 0;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        for (int index = 0; index + 6 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 1] is not { Definition.Opcode: Move, Operands: [var source, var saved] }
                || source != result || saved.Kind != BeamOperandParameters.Y
                || body[index + 2] is not { Definition.Opcode: TestHeap, Operands: [_, var live] } heap
                || !live.Value.IsZero || body[index + 4].Definition.Opcode != Deallocate
                || body[index + 5].Definition.Opcode != Return)
            {
                continue;
            }

            BeamInstruction construction = body[index + 3];
            bool returnsSavedResult = construction.Definition.Opcode switch
            {
                PutTuple => construction.Operands[0] == result
                    && construction.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, saved)),
                PutList => construction.Operands[2] == result
                    && (SameProjectionRegister(construction.Operands[0], saved)
                        || SameProjectionRegister(construction.Operands[1], saved)),
                _ => false
            };
            if (!returnsSavedResult)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index + 1].Offset]);
            // The final compact operand of test_heap is the zero live-register count.
            output.Write(code.Span[heap.Offset..(construction.Offset - 1)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = construction.Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < construction.Offset)
            {
                stackIndex++;
            }

            int end = body[index + 4].Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < end)
            {
                var (start, finish, slot) = stackOperands[stackIndex++];
                if (slot == saved.Value)
                {
                    output.Write(code.Span[copied..start]);
                    WriteRegister(output, result);
                    copied = finish;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? ForwardTailCallResult(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                || body[index + 1] is not { Definition.Opcode: Move, Operands: [var source, var saved] }
                || source != result || saved.Kind != BeamOperandParameters.Y
                || FindTailResultReload(body, index + 2, saved) is not int reload)
            {
                continue;
            }

            BeamOperand destination = body[reload].Operands[1];
            output.Write(code.Span[copied..body[index + 1].Offset]);
            if (destination != result)
            {
                output.WriteByte(Move);
                WriteRegister(output, result);
                WriteRegister(output, destination);
            }

            output.Write(code.Span[body[index + 2].Offset..body[reload].Offset]);
            copied = body[reload + 1].Offset;
            index = reload;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? FindTailResultReload(IReadOnlyList<BeamInstruction> body, int start, BeamOperand saved)
    {
        var touched = new HashSet<int>();
        int? reload = null;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var source, var destination] })
            {
                return instruction.Definition.Opcode is TailExternalCallWithFrame or TailLocalCallWithFrame or TailDynamicApply
                    ? reload : null;
            }

            if (SameProjectionRegister(source, saved))
            {
                if (reload.HasValue || destination is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                    || touched.Contains((int)destination.Value))
                {
                    return null;
                }

                reload = index;
            }
            else if (SameProjectionRegister(destination, saved))
            {
                return null;
            }
            else if (reload is null)
            {
                // An earlier move must neither read nor overwrite the replacement register.
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.X)
                    {
                        touched.Add((int)operand.Value);
                    }
                }
            }
        }

        return null;
    }
}
