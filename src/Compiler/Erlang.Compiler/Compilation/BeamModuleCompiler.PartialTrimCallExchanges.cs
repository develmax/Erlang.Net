using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalescePartialTrimCallExchanges(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        int stackOperand = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadCallExchange(start, end) is not { } plan)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.OwnsSinglePartialTrim(
                start,
                plan.Allocation,
                plan.Trim,
                end
            ))
            {
                continue;
            }

            // The call result and the next argument exchange locations. The
            // discarded lowest slot serves only as a temporary for that copy.
            for (int index = plan.Allocation; index < plan.Trim - 3; index++)
            {
                int offset = body[index].Offset;
                int next = body[index + 1].Offset;
                int widthOperand = LinearFrameSizeOperand(body[index].Definition.Opcode);
                if (widthOperand >= 0)
                {
                    var compact = new BeamCursor(code[(offset + 1)..]);
                    for (int i = 0; i < widthOperand; i++)
                    {
                        _ = BeamCompactReader.Read(compact);
                    }

                    output.Write(code.Span[copied..(offset + 1 + compact.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, plan.Width - 1);
                    _ = BeamCompactReader.Read(compact);
                    copied = offset + 1 + compact.Position;
                    continue;
                }

                if (body[index].Definition.Opcode == InitYRegisters)
                {
                    output.Write(code.Span[copied..offset]);
                    int[] slots = body[index].Operands.Skip(1).Where(slot => slot.Value > 0)
                        .Select(slot => (int)slot.Value - 1).ToArray();
                    if (slots.Length > 0)
                    {
                        WriteFrameInitialization(output, slots);
                    }

                    copied = next;
                    continue;
                }

                while (stackOperand < decoded.StackOperands.Count && decoded.StackOperands[stackOperand].Start < offset)
                {
                    stackOperand++;
                }

                while (stackOperand < decoded.StackOperands.Count && decoded.StackOperands[stackOperand].Start < next)
                {
                    var operand = decoded.StackOperands[stackOperand++];
                    output.Write(code.Span[copied..operand.Start]);
                    BeamCompactWriter.Write(output, YRegisterTag, operand.Slot - 1);
                    copied = operand.End;
                }
            }

            output.Write(code.Span[copied..body[plan.Trim - 3].Offset]);
            output.WriteByte(Swap);
            BeamCompactWriter.Write(output, YRegisterTag, plan.Saved - 1);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            copied = body[plan.Trim + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, Swap);
        output.Write(code.Span[copied..]);

        return output.ToArray();

        (int Allocation, int Trim, int Width, int Saved)? ReadCallExchange(int start, int end)
        {
            int allocation = FindLinearFrameSuffix(body, start, end);
            if (allocation < 0 || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                return null;
            }

            var width = body[allocation].Operands[0].Value;
            if (width < 2 || width > BeamOperandParameters.MaximumRegister)
            {
                return null;
            }

            int trim = -1;
            for (int index = start; index < end; index++)
            {
                if (index == allocation)
                {
                    continue;
                }

                var op = body[index].Definition.Opcode;
                if (op is Allocate or AllocateHeap or TryEnter or CatchEnter or TryEnd or TryCase or CatchEnd
                    or TryCaseEnd or Raise or DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd
                    or Wait or WaitTimeout or RemoveMessage or ReceiveTimeout)
                {
                    return null;
                }

                if (op == TrimFrame)
                {
                    if (trim >= 0)
                    {
                        return null;
                    }

                    trim = index;
                }
            }

            if (trim < allocation + 5 || body[trim].Operands is not [{ Value.IsOne: true }, var kept]
                || kept.Value + 1 != width || body[trim - 4].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                return null;
            }

            var x0 = new BeamOperand(BeamOperandParameters.X, 0);
            var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
            if (body[trim - 3] is not { Definition.Opcode: Move, Operands: [var result, var scratch] }
                || result != x0 || scratch != y0
                || body[trim - 2] is not { Definition.Opcode: Move, Operands: [var saved, var argument] }
                || argument != x0 || saved is not { Kind: BeamOperandParameters.Y, TypeIndex: null, Value.Sign: > 0 }
                || saved.Value >= width
                || body[trim - 1] is not { Definition.Opcode: Move, Operands: [var copy, var replace] }
                || copy != scratch || replace != saved)
            {
                return null;
            }

            for (int index = allocation + 1; index < trim - 3; index++)
            {
                var instruction = body[index];
                int widthOperand = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (widthOperand >= 0 && instruction.Operands[widthOperand].Value != width
                    || instruction.Definition.Opcode != InitYRegisters
                        && instruction.Operands.Any(operand => SameProjectionRegister(operand, y0)))
                {
                    return null;
                }
            }

            return (allocation, trim, (int)width, (int)saved.Value);
        }
    }
}
