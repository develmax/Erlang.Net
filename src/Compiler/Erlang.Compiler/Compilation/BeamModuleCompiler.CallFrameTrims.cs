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

        if (ReuseOutgoingStackValueSlots(code, body, stackOperands) is { } assigned)
        {
            code = assigned;
            stackOperands.Clear();
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
                    out int release,
                    allowCompleteRelease: !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
                ))
            {
                TrimDeadBranchFrames(start, end);
                continue;
            }

            bool endsWithCompleteTrim = body[release].Definition.Opcode == TrimFrame;
            int consumedClear = -1;
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

                int argumentEnd = site;
                bool pure = CallsPureBif(instruction);

                // OTP places the X0 load after the kills unless it reads a killed value.
                var outgoingKills = ReadOutgoingStackValueKills(
                    body,
                    previousCall,
                    site,
                    release,
                    instruction
                );
                if (outgoingKills.Length == 0 && site > previousCall + 1 && body[site - 1] is
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
                    pure,
                    outgoingKills
                ) is { } trim)
                {
                    // Keep the existing placement unless reading the final list
                    // argument first proves a narrower two-to-one trim.
                    if (site < argumentEnd && trim.Removed == slots.Count - 2
                        && instruction.Operands[0].Value == 2)
                    {
                        int[] finalArgument = ReadOutgoingStackValueKills(
                            body,
                            previousCall,
                            argumentEnd,
                            release,
                            instruction,
                            allowBinaryListCall: true
                        );
                        if (finalArgument.Length == 1 && PlanCallFrameTrim(
                            slots,
                            lastUse,
                            initialized,
                            previousCall,
                            argumentEnd,
                            pure,
                            finalArgument
                        ) is { } afterLoad && afterLoad.Removed == slots.Count - 1)
                        {
                            site = argumentEnd;
                            trim = afterLoad;
                        }
                    }

                    if (endsWithCompleteTrim)
                    {
                        if (consumedClear >= 0 || site <= previousCall + 2
                            || body[site - 1] is not
                            {
                                Definition.Opcode: InitYRegisters,
                                Operands: [{ Value.IsOne: true }, { Kind: BeamOperandParameters.Y, Value.IsOne: true }]
                            }
                            || !ReadsFinalClosureArgument(body, start, site - 2)
                            || lastUse[1] != site - 1 || trim.Removed != 1
                            || trim.Kills.Length != 0 || trim.Moves is not [(0, 1)])
                        {
                            previousCall = call;

                            continue;
                        }

                        // OTP consumes this last clear by relocating the lowest
                        // live root into its dead slot before releasing Y0.
                        consumedClear = --site;
                    }

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
                    if (index == consumedClear)
                    {
                        copied = body[index + 1].Offset;

                        continue;
                    }

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

            if (endsWithCompleteTrim)
            {
                output.Write(code.Span[copied..body[release].Offset]);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, slots.Count);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                copied = body[release + 1].Offset;

                continue;
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
            BeamProjectionFlow? flow = null;
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
                int retained = size == 0 && width >= 2 && width <= BeamOperandParameters.MaximumRegister
                    ? width == 2 ? 0 : (int)width - 1
                    : -1;
                int site = retained >= 0 ? FindSingleLiveBranchTrimSite(
                    body,
                    start,
                    exit,
                    retained,
                    ref flow
                ) : -1;
                if (site < 0 || CallsPureBif(body[exit - 2])
                    || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not { } frame
                    || frame.Uses.Count != 1
                    || !frame.Uses.TryGetValue(frame.Owners[site - start], out bool[]? slots)
                    || slots.Length != width)
                {
                    retained = -1;
                    site = FindDeadBranchTrimSite(body, start, exit);
                    if (site < 0 && size == 0 && width >= 3 && body[start].Definition.Opcode == Label
                        && registerSelectorEntries.Contains((int)body[start].Operands[0].Value))
                    {
                        site = FindTerminalConstructionTrimSite(body, start, exit);
                    }
                }

                int loaded = site < 0 ? FindFinalLoadedCallTrimSite(body, start, exit) : -1;
                if (loaded >= 0
                    && (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is { } joined
                    && joined.Owners[loaded - start] == joined.Owners[exit - start]
                    && joined.Uses.TryGetValue(joined.Owners[loaded - start], out bool[]? roots)
                    && roots.Length == width)
                {
                    site = loaded;
                }

                if (site < 0 || body[site].Offset < copied)
                {
                    continue;
                }

                int offset = body[site].Offset;
                output.Write(code.Span[copied..offset]);
                int remaining = retained >= 0 ? 1 : 0;
                if (retained == 0)
                {
                    // Y1 has supplied the call argument. Move the saved result before removing Y0.
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    BeamCompactWriter.Write(output, YRegisterTag, 1);
                }

                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width - remaining);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, remaining);

                if (retained > 0)
                {
                    int restore = body[exit - 1].Offset;
                    output.Write(code.Span[offset..restore]);
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    BeamCompactWriter.Write(output, XRegisterTag, 0);
                    offset = body[exit].Offset;
                }

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

    private static int FindSingleLiveBranchTrimSite(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int exit,
        int retained,
        ref BeamProjectionFlow? flow
    )
    {
        int call = exit - 2;
        if (call <= start
            || body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
            || !IsTrimRegisterMove(
                body[exit - 1],
                BeamOperandParameters.Y,
                retained,
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
                retained
            )
            || body[load] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } argument,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                ]
            }
            || (retained == 0 ? argument.Value != 1 : argument.Value >= retained))
        {
            return -1;
        }

        if (retained > 0)
        {
            // The exact-test rejection owns this terminal call. Its top slot
            // contains the saved result, and the argument has already left Y.
            int landing = load - 2;
            int test = landing - 4;
            if (test <= start || body[landing - 1].Definition.Opcode != Return
                || body[landing - 2].Definition.Opcode != Deallocate
                || body[test] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } target,
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true },
                    { Kind: BeamOperandParameters.Atom }
                    ]
                }
                || target.Value != body[landing].Operands[0].Value
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    test,
                    (int)target.Value,
                    out int destination
                ) || destination != landing)
            {
                return -1;
            }
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

    private static int FindFinalLoadedCallTrimSite(IReadOnlyList<BeamInstruction> body, int start, int exit)
    {
        if (body[exit] is not
            { Definition.Opcode: TailExternalCallWithFrame, Operands: [{ Value.IsZero: true }, _, _] })
        {
            return -1;
        }

        int call = exit - 1;
        while (call > start && body[call].Definition.Opcode == Line)
        {
            call--;
        }

        if (call <= start || body[call] is not { Definition.Opcode: ExternalCall, Operands: [var arity, _] }
            || arity.Value != 2)
        {
            return -1;
        }

        int firstLine = call;
        while (firstLine > start && body[firstLine - 1].Definition.Opcode == Line)
        {
            firstLine--;
        }

        // Both incoming call arguments have taken their last stack reads.
        // The block has no later label, frame operand or protected call edge.
        if (firstLine < start + 3 || body[firstLine - 3].Definition.Opcode != Label
            || !ReadsStackArgument(body[firstLine - 2], 1, out var second)
            || !ReadsStackArgument(body[firstLine - 1], 0, out var first)
            || first == second)
        {
            return -1;
        }

        return firstLine;

        static bool ReadsStackArgument(BeamInstruction instruction, int argument, out int slot)
        {
            slot = -1;
            if (instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } || destination.Value != argument)
            {
                return false;
            }

            slot = (int)saved.Value;

            return true;
        }
    }

    private static int FindTerminalConstructionTrimSite(IReadOnlyList<BeamInstruction> body, int start, int exit)
    {
        bool constructs = false;
        bool called = false;
        for (int index = exit - 1; index > start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Label || IsEntryFramePredicate(opcode))
            {
                return called ? index + 1 : -1;
            }

            if (opcode is not (Line or Move or TestHeap or PutTuple or PutList or LocalCall or ExternalCall))
            {
                return -1;
            }

            if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                return called && instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y }, { Kind: BeamOperandParameters.X }]
                } ? index + 1 : -1;
            }

            constructs |= opcode is PutTuple or PutList;
            called |= constructs && opcode is LocalCall or ExternalCall;
        }

        return -1;
    }

    private static bool ReadsFinalClosureArgument(IReadOnlyList<BeamInstruction> body, int start, int load)
    {
        if (!IsTrimRegisterMove(
            body[load],
            BeamOperandParameters.Y,
            1,
            BeamOperandParameters.X,
            0
        ))
        {
            return false;
        }

        var closure = new BeamOperand(BeamOperandParameters.Y, 1);
        for (int index = load - 1; index > start; index--)
        {
            var instruction = body[index];
            if (instruction.Operands.Any(operand => SameProjectionRegister(operand, closure)))
            {
                return instruction.Definition.Opcode == MakeFun && instruction.Operands[1] == closure;
            }
        }

        return false;
    }

    private static bool ReadCallFrame(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame,
        out int[] lastUse,
        out int release,
        bool allowCompleteRelease = false
    )
    {
        lastUse = new int[frame];
        Array.Fill(lastUse, -1);
        release = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (allowCompleteRelease && opcode == TrimFrame
                && instruction.Operands is [var removed, { Value.IsZero: true }] && removed.Value == frame)
            {
                release = index;

                return true;
            }

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

    private static byte[]? ReuseOutgoingStackValueSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int encoded = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            int frame = FindLinearFrameSuffix(body, entry, end);
            if (frame < 0 || !ReadCallFrame(
                body,
                frame + 1,
                end,
                (int)body[frame].Operands[0].Value,
                out _,
                out int release
            ))
            {
                continue;
            }

            int previousCall = frame;
            for (int index = frame + 1; index < release; index++)
            {
                BeamInstruction call = body[index];
                if (!IsFrameTrimCall(call.Definition.Opcode))
                {
                    continue;
                }

                int site = index;
                while (site > previousCall + 1 && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }

                int[] killed = ReadOutgoingStackValueKills(
                    body,
                    previousCall,
                    site,
                    release,
                    call
                );
                previousCall = index;
                if (killed.Length < 3)
                {
                    continue;
                }

                // Live ranges that end at the call release their registers. The
                // pinned allocator chooses the lowest free virtual register,
                // then reverses its number for the physical Y layout.
                int[] definitions = Enumerable.Range(index + 1, release - index - 1)
                    .Select(definition =>
                    {
                        BeamInstruction instruction = body[definition];
                        int destination = StackAllocationDestination(instruction);

                        return destination >= 0 && instruction.Operands[destination] is { Kind: BeamOperandParameters.Y } slot
                            ? (int)slot.Value : -1;
                    })
                    .Where(slot => killed.Contains(slot))
                    .Distinct()
                    .ToArray();
                if (definitions.Length < 2)
                {
                    continue;
                }

                int[] reassigned = definitions.OrderDescending().ToArray();
                if (reassigned.SequenceEqual(definitions))
                {
                    continue;
                }

                var mapping = definitions.Zip(reassigned).ToDictionary(pair => pair.First, pair => pair.Second);
                int replaceStart = body[index + 1].Offset;
                int replaceEnd = body[release].Offset;
                while (encoded < stackOperands.Count && stackOperands[encoded].Start < replaceStart)
                {
                    encoded++;
                }

                while (encoded < stackOperands.Count && stackOperands[encoded].Start < replaceEnd)
                {
                    var (from, to, slot) = stackOperands[encoded++];
                    if (mapping.TryGetValue(slot, out int destination) && slot != destination)
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, destination);
                        copied = to;
                    }
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int[] ReadOutgoingStackValueKills(
        IReadOnlyList<BeamInstruction> body,
        int previousCall,
        int site,
        int release,
        BeamInstruction call,
        bool allowBinaryListCall = false
    )
    {
        if (site <= previousCall + 3 || call.Definition.Opcode is not (LocalCall or ExternalCall)
            || call.Operands is not [{ Value: var arity }, _]
            || arity < 4 && (!allowBinaryListCall || arity != 2)
            || body[site - 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, { Kind: BeamOperandParameters.X, Value.IsZero: true }]
            })
        {
            return [];
        }

        var loaded = new List<int>();
        var destinations = new HashSet<int>();
        int first = site - 1;
        while (first > previousCall && body[first] is
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null, Value: var slot },
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var argument }]
            })
        {
            if (argument >= arity || !destinations.Add((int)argument) || loaded.Contains((int)slot))
            {
                return [];
            }

            loaded.Add((int)slot);
            first--;
        }

        BeamOperand? construction = body[first].Definition.Opcode switch
        {
            PutList => body[first].Operands[^1],
            PutTuple => body[first].Operands[0],
            _ => null
        };
        if (loaded.Count + 1 != arity || arity == 2 && body[first].Definition.Opcode != PutList
            || construction is not { Kind: BeamOperandParameters.X, TypeIndex: null }
            || construction.Value >= arity || destinations.Contains((int)construction.Value))
        {
            return [];
        }

        // A later definition keeps the storage slot but ends this value's lifetime.
        // End at the first read or write so a use after an intervening call stays live.
        return IsDead(loaded[0]) ? loaded.Where(IsDead).ToArray() : [];

        bool IsDead(int slot)
        {
            for (int index = site; index < release; index++)
            {
                BeamInstruction instruction = body[index];
                int write = StackAllocationDestination(instruction);
                var operands = instruction.Operands;
                bool reads = false;
                bool writes = false;
                for (int operand = 0; operand < operands.Count; operand++)
                {
                    if (operands[operand].Kind != BeamOperandParameters.Y || operands[operand].Value != slot)
                    {
                        continue;
                    }

                    if (operand == write || instruction.Definition.Opcode == InitYRegisters)
                    {
                        writes = true;
                    }
                    else
                    {
                        reads = true;
                    }
                }

                if (reads || writes)
                {
                    return !reads;
                }
            }

            return true;
        }
    }

    private static (int Removed, int[] Kills, (int Source, int Destination)[] Moves)? PlanCallFrameTrim(
        IReadOnlyList<int> slots,
        int[] lastUse,
        bool[] initialized,
        int previousCall,
        int site,
        bool pure,
        IReadOnlyList<int> outgoingKills
    )
    {
        int[] killed = Enumerable.Range(0, slots.Count)
            .Where(slot => initialized[slots[slot]] && (outgoingKills.Contains(slots[slot])
                || lastUse[slots[slot]] > previousCall && lastUse[slots[slot]] < site))
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
