using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ExchangeCallResultWithSavedInput(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (PlanExchange(entry, end) is not { } plan)
            {
                continue;
            }

            var (allocation, spill, store, load, clear) = plan;
            for (int index = allocation; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (index == clear)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    WriteFrameInitialization(output, [0]);
                    copied = instruction.Offset;
                }

                int size = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (size >= 0 && instruction.Operands[size].Value == 2)
                {
                    int operandsStart = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[operandsStart..]);
                    for (int position = 0; position < size; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    output.Write(code.Span[copied..(operandsStart + cursor.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    _ = BeamCompactReader.Read(cursor);
                    copied = operandsStart + cursor.Position;
                }
                else if (index == store)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Swap);
                    WriteRegister(output, y0);
                    WriteRegister(output, x0);
                    index++;
                    copied = body[index + 1].Offset;
                }
                else if (index == spill || index == load)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, index == spill ? instruction.Operands[0] : y0);
                    WriteRegister(output, index == spill ? y0 : x0);
                    copied = body[index + 1].Offset;
                }
                else if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    // Only obsolete initializations and post-use spills can remain.
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (int Allocation, int Spill, int Store, int Load, int Clear)? PlanExchange(int entry, int end)
        {
            int allocation = FindEntryFrameAllocation(body, entry, end);
            if (allocation < 0 || body[allocation].Definition.Opcode != Allocate || body[allocation].Operands[0].Value != 2)
            {
                return null;
            }

            int call = allocation + 1;
            while (call < end && body[call].Definition.Opcode is Move or InitYRegisters or Line)
            {
                call++;
            }

            int store = call + 1;
            if (store + 2 >= end || !IsExchangeReturningCall(body[call], 1)
                || body[store] is not { Definition.Opcode: Move, Operands: [var result, var saved] } || result != x0
                || body[store + 1] is not { Definition.Opcode: Move, Operands: [var input, var argument] } || argument != x0
                || input is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || saved is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || saved.Value == input.Value || saved.Value > 1 || input.Value > 1)
            {
                return null;
            }

            int spill = -1;
            for (int index = allocation + 1; index < call; index++)
            {
                BeamInstruction setup = body[index];
                if (!setup.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    continue;
                }

                if (setup.Definition.Opcode == InitYRegisters
                    && (spill < 0 || !setup.Operands.Skip(1).Contains(input)))
                {
                    continue;
                }

                if (spill >= 0 || setup is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                    || source is not { Kind: BeamOperandParameters.X, TypeIndex: null } || destination != input)
                {
                    return null;
                }

                spill = index;
            }

            int second = store + 2;
            while (second < end && (body[second].Definition.Opcode == Line
                || body[second] is { Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.X }] }
                    && source.Kind != BeamOperandParameters.Y))
            {
                second++;
            }

            int load = second + 2;
            if (spill < 0 || load + 1 >= end || !IsExchangeReturningCall(body[second], 1)
                || body[second + 1] is not { Definition.Opcode: Move, Operands: [var secondResult, var secondArgument] }
                || secondResult != x0 || secondArgument != x1
                || body[load] is not { Definition.Opcode: Move, Operands: [var retained, var firstArgument] }
                || retained != saved || firstArgument != x0)
            {
                return null;
            }

            int clear = load + 1;
            int consume = clear;
            while (consume < end && body[consume].Definition.Opcode == Line)
            {
                consume++;
            }

            if (consume == end || !IsExchangeReturningCall(body[consume], 2))
            {
                return null;
            }

            for (int index = consume + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Name == "executable_line"
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                        && !(instruction.Definition.Opcode == InitYRegisters
                            || instruction is { Definition.Opcode: Move, Operands: [var value, var destination] }
                                && value == x0 && destination == saved))
                {
                    return null;
                }
            }

            // The copy island has no entry labels or collector between spill/load pairs.
            // Prove that each frame exit and every branch join share the same allocation.
            if (ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                || ownership.Uses.Count != 1 || ownership.Owners[consume - entry] != allocation)
            {
                return null;
            }

            return (allocation, spill, store, load, clear);
        }
    }

    private static bool IsExchangeReturningCall(BeamInstruction instruction, int minimumArity) =>
        instruction.Definition.Opcode is LocalCall or ExternalCall && instruction.Operands[0].Value >= minimumArity;
}
