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
                TrimDeadBranchFrames(start, end);
                continue;
            }

            int frame = (int)body[start + 1].Operands[0].Value;
            if (frame < 2 || frame > BeamOperandParameters.MaximumRegister
                || !ReadCallFrame(
                    body,
                    start + 2,
                    end,
                    frame,
                    out var lastUse,
                    out int release
                ))
            {
                TrimDeadBranchFrames(start, end);
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

                if (PlanCallFrameTrim(
                    slots,
                    lastUse,
                    initialized,
                    previousCall,
                    site,
                    CallsPureBif(instruction)
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

        void TrimDeadBranchFrames(int start, int end)
        {
            for (int index = start; index < end; index++)
            {
                if (body[index].Definition.Opcode is CatchEnter or TryEnter || body[index].Definition.Name == "debug_line")
                {
                    return;
                }
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int exit = start; exit + 1 < end; exit++)
            {
                int size = body[exit].Definition.Opcode switch
                {
                    Deallocate when body[exit + 1].Definition.Opcode == Return => 0,
                    TailExternalCallWithFrame or TailLocalCallWithFrame => 2,
                    _ => -1
                };
                if (size < 0 || body[exit].Operands[size].Value <= 0)
                {
                    continue;
                }

                var width = body[exit].Operands[size].Value;
                int site = size == 0 && width == 2 ? FindSingleLiveBranchTrimSite(body, start, exit) : -1;
                bool relocate = false;
                if (site >= 0 && !CallsPureBif(body[exit - 2])
                    && (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is { } frame
                    && frame.Uses.Count == 1
                    && frame.Uses.TryGetValue(frame.Owners[site - start], out bool[]? slots)
                    && slots.Length == width)
                {
                    relocate = true;
                }
                else
                {
                    site = FindDeadBranchTrimSite(body, start, exit);
                }

                if (site < 0 || body[site].Offset < copied)
                {
                    continue;
                }

                int offset = body[site].Offset;
                output.Write(code.Span[copied..offset]);
                int remaining = relocate ? 1 : 0;
                if (relocate)
                {
                    // Y1 has supplied the call argument. Move the saved result before removing Y0.
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    BeamCompactWriter.Write(output, YRegisterTag, 1);
                }

                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width - remaining);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, remaining);

                int operandStart = body[exit].Offset + 1;
                var cursor = new BeamCursor(code[operandStart..]);
                for (int operand = 0; operand < size; operand++)
                {
                    _ = BeamCompactReader.Read(cursor);
                }

                output.Write(code.Span[offset..(operandStart + cursor.Position)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, remaining);
                _ = BeamCompactReader.Read(cursor);
                copied = operandStart + cursor.Position;
            }
        }

        bool CallsPureBif(BeamInstruction instruction)
        {
            if (instruction.Definition.Opcode != ExternalCall)
            {
                return false;
            }

            var target = imported[(int)instruction.Operands[1].Value];

            return BeamTrimBifs.IsPure(atomNames[target.Module - 1], atomNames[target.Function - 1], target.Arity);
        }
    }

    private static int FindSingleLiveBranchTrimSite(IReadOnlyList<BeamInstruction> body, int start, int exit)
    {
        int call = exit - 2;
        if (call <= start
            || body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
            || !IsTrimRegisterMove(
                body[exit - 1],
                BeamOperandParameters.Y,
                0,
                BeamOperandParameters.X,
                0
            ))
        {
            return -1;
        }

        int load = call - 1;
        while (load > start && body[load].Definition.Opcode == Line)
        {
            load--;
        }

        if (load - 2 <= start || body[load - 2].Definition.Opcode != Label
            || !IsTrimRegisterMove(
                body[load - 1],
                BeamOperandParameters.X,
                0,
                BeamOperandParameters.Y,
                0
            )
            || !IsTrimRegisterMove(
                body[load],
                BeamOperandParameters.Y,
                1,
                BeamOperandParameters.X,
                0
            ))
        {
            return -1;
        }

        return load + 1;
    }

    private static bool IsTrimRegisterMove(
        BeamInstruction instruction,
        string source,
        int input,
        string destination,
        int result
    ) =>
        instruction is { Definition.Opcode: Move, Operands: [var from, var to] }
        && from.Kind == source && from.Value == input && from.TypeIndex is null
        && to.Kind == destination && to.Value == result && to.TypeIndex is null;

    private static int FindDeadBranchTrimSite(IReadOnlyList<BeamInstruction> body, int start, int exit)
    {
        bool returningCall = false;
        for (int index = exit - 1; index > start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is not (Line or Move or TestHeap or PutTuple or PutList or LocalCall or ExternalCall))
            {
                // A label may admit another frame state. Other control-flow instructions can leave the suffix.
                return -1;
            }

            if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                // The last stack read must feed a construction into X before an ordinary call.
                bool constructs = opcode == PutTuple && instruction.Operands[0].Kind == BeamOperandParameters.X
                    || opcode == PutList && instruction.Operands[^1].Kind == BeamOperandParameters.X;

                return returningCall && constructs && Enumerable.Range(start + 1, index - start - 1)
                    .Any(branch => body[branch].Definition.Opcode == Label) ? index + 1 : -1;
            }

            returningCall |= opcode is LocalCall or ExternalCall;
        }

        return -1;
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
