using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EliminateOverwrittenProjectionInitializations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteProjectionInitializations(code, decoded.Body);
    }

    private static byte[]? RewriteProjectionInitializations(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[index + 1].Definition.Opcode != InitYRegisters)
            {
                continue;
            }

            BeamInstruction clear = body[index + 1];
            var active = clear.Operands.Skip(1).Select(operand => (int)operand.Value).ToHashSet();
            var overwritten = new HashSet<int>();
            for (int next = index + 2; next < body.Count && active.Count > 0; next++)
            {
                BeamInstruction instruction = body[next];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == Line)
                {
                    continue;
                }

                // Only this straight-line corridor is known to define or read
                // slots without scanning the frame or following another edge.
                if (opcode is not (Move or GetTupleElement or GetList or GetHead or GetTail))
                {
                    break;
                }

                BeamOperand source = instruction.Operands[0];
                if (source.Kind == BeamOperandParameters.Y)
                {
                    active.Remove((int)source.Value);
                }

                int destination = StackWriteOperand(instruction);
                Write(instruction.Operands[destination], opcode != Move);
                if (opcode == GetList)
                {
                    Write(instruction.Operands[2], projection: true);
                }
            }

            if (overwritten.Count == 0)
            {
                continue;
            }

            int[] retained = clear.Operands.Skip(1).Select(operand => (int)operand.Value)
                .Where(slot => !overwritten.Contains(slot)).ToArray();
            output.Write(code.Span[copied..clear.Offset]);
            WriteFrameInitialization(output, retained);
            copied = body[index + 2].Offset;

            void Write(BeamOperand operand, bool projection)
            {
                if (operand.Kind == BeamOperandParameters.Y && active.Remove((int)operand.Value) && projection)
                {
                    overwritten.Add((int)operand.Value);
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
}
