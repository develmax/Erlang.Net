using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimStackBetweenCalls(ReadOnlyMemory<byte> code, int labelCount)
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordStackOperand: (start, end, slot) => stackOperands.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 1 >= end || body[start].Definition.Opcode != Label
                || body[start + 1].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            int frame = (int)body[start + 1].Operands[0].Value;
            if (frame < 2 || frame > BeamOperandParameters.MaximumRegister
                || !ReadCallFrame(body, start + 2, end, frame, out var lastUse, out int release))
            {
                continue;
            }

            var slots = Enumerable.Range(0, frame).ToList();
            int[] mapped = slots.ToArray();
            var initialized = new bool[frame];
            int previousCall = -1;
            var trims = new Dictionary<int, (int Removed, int[] Kills, (int Source, int Destination)[] Moves)>();
            for (int call = start + 2; call < release; call++)
            {
                BeamInstruction instruction = body[call];
                if (!IsFrameTrimCall(instruction.Definition.Opcode))
                {
                    int destination = StackAllocationDestination(instruction);
                    if (destination >= 0 && instruction.Operands[destination].Kind == BeamOperandParameters.Y)
                    {
                        initialized[(int)instruction.Operands[destination].Value] = true;
                    }

                    if (instruction.Definition.Opcode == InitYRegisters)
                    {
                        foreach (BeamOperand slot in instruction.Operands.Skip(1))
                        {
                            initialized[(int)slot.Value] = true;
                        }
                    }

                    continue;
                }

                // Slots dead at the first call belong to entry allocation and argument lowering.
                if (previousCall < 0)
                {
                    previousCall = call;
                    continue;
                }

                int site = call;
                while (site > previousCall + 1 && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }

                // OTP leaves the final X0 argument load beside the call when it can.
                if (site > previousCall + 1 && body[site - 1] is
                    { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X, Value.IsZero: true }] })
                {
                    site--;
                }

                bool pure = false;
                if (instruction.Definition.Opcode == ExternalCall)
                {
                    var import = imported[(int)instruction.Operands[1].Value];
                    pure = BeamTrimBifs.IsPure(atomNames[import.Module - 1], atomNames[import.Function - 1], import.Arity);
                }

                if (PlanCallFrameTrim(
                    slots,
                    lastUse,
                    initialized,
                    previousCall,
                    site,
                    pure
                ) is { } trim)
                {
                    trims.Add(site, trim);
                    foreach (var (source, destination) in trim.Moves)
                    {
                        slots[destination] = slots[source];
                    }

                    slots.RemoveRange(0, trim.Removed);
                }

                previousCall = call;
            }

            if (trims.Count == 0)
            {
                continue;
            }

            slots = Enumerable.Range(0, frame).ToList();
            for (int index = start + 2; index < release; index++)
            {
                int offset = body[index].Offset;
                if (trims.TryGetValue(index, out var trim))
                {
                    output.Write(code.Span[copied..offset]);
                    WriteFrameInitialization(output, trim.Kills);
                    foreach (var (source, destination) in trim.Moves)
                    {
                        output.WriteByte(Move);
                        BeamCompactWriter.Write(output, YRegisterTag, source);
                        BeamCompactWriter.Write(output, YRegisterTag, destination);
                        slots[destination] = slots[source];
                    }

                    slots.RemoveRange(0, trim.Removed);
                    for (int slot = 0; slot < slots.Count; slot++)
                    {
                        mapped[slots[slot]] = slot;
                    }

                    output.WriteByte(TrimFrame);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, trim.Removed);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, slots.Count);
                    copied = offset;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < body[index + 1].Offset)
                {
                    var (from, to, original) = stackOperands[operand++];
                    if (mapped[original] != original)
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, mapped[original]);
                        copied = to;
                    }
                }
            }

            int size = LinearFrameSizeOperand(body[release].Definition.Opcode);
            int operandStart = body[release].Offset + 1;
            var cursor = new BeamCursor(code[operandStart..]);
            for (int index = 0; index < size; index++)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            output.Write(code.Span[copied..(operandStart + cursor.Position)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, slots.Count);
            _ = BeamCompactReader.Read(cursor);
            copied = operandStart + cursor.Position;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }

    private static bool ReadCallFrame(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame,
        out int[] lastUse,
        out int release
    )
    {
        lastUse = new int[frame];
        Array.Fill(lastUse, -1);
        release = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Label or Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter or CatchEnd
                or TryEnd or TryCase or Wait or WaitTimeout or LoopReceive or LoopReceiveEnd or ReceiveTimeout or RemoveMessage
                || instruction.Definition.Name == "debug_line"
                || opcode is not (LocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label))
            {
                return false;
            }

            int size = LinearFrameSizeOperand(opcode);
            if (size >= 0)
            {
                release = index;
                return instruction.Operands[size].Value == frame
                    && (opcode != Deallocate || index + 1 < end && body[index + 1].Definition.Opcode == Return);
            }

            if (EndsBeamBlock(opcode))
            {
                return false;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y)
                {
                    if (operand.Value >= frame)
                    {
                        return false;
                    }

                    lastUse[(int)operand.Value] = index;
                }
            }
        }

        return false;
    }

    private static (int Removed, int[] Kills, (int Source, int Destination)[] Moves)? PlanCallFrameTrim(
        IReadOnlyList<int> slots,
        int[] lastUse,
        bool[] initialized,
        int previousCall,
        int site,
        bool pure
    )
    {
        int[] killed = Enumerable.Range(0, slots.Count)
            .Where(slot => initialized[slots[slot]] && lastUse[slots[slot]] > previousCall && lastUse[slots[slot]] < site)
            .ToArray();
        if (killed.Length == 0)
        {
            return null;
        }

        (int Removed, int[] Kills, (int Source, int Destination)[] Moves)? recipe = null;
        var moves = new List<(int Source, int Destination)>();
        int last = slots.Count - 1;
        for (int first = 0; first <= last; first++)
        {
            if (lastUse[slots[first]] >= site)
            {
                while (last > first && lastUse[slots[last]] >= site)
                {
                    last--;
                }

                if (last <= first || !initialized[slots[first]])
                {
                    break;
                }

                moves.Add((first, last--));
            }

            int[] remaining = killed.Where(slot => slot > first && moves.All(move => move.Destination != slot)).ToArray();
            int cost = remaining.Length + moves.Count + (pure ? moves.Count == 0 ? 1 : 2 : 0);
            if (cost <= killed.Length)
            {
                recipe = (first + 1, remaining, moves.AsEnumerable().Reverse().ToArray());
            }
        }

        return recipe;
    }

    private static bool IsFrameTrimCall(byte opcode) => opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped;
}
