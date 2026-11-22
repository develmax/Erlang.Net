using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepTerminalPairAcrossHeap(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || sourceModule is not { DeclaredFunctions: var functions } || OnLoadFunction is not null
            || !code.Span.Contains(TestHeap) || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteTerminalPairConstruction(
            code,
            decoded.Body,
            decoded.StackOperands,
            ReadLocalFixedPairResultProof(functions, decoded.Body)
        );
    }

    private Func<int, bool> ReadLocalFixedPairResultProof(
        IEnumerable<FunctionDefinition> functions,
        IReadOnlyList<BeamInstruction> body,
        bool emittedAtomIndices = false
    )
    {
        var declarations = functions.ToDictionary(item => (item.Name, item.Arity));
        var entries = new Dictionary<int, FunctionDefinition>();
        var names = atoms.Select(atom => (atom.Key, Index: emittedAtomIndices ? MapEmittedAtom(atom.Value) : atom.Value))
            .Where(atom => atom.Index != 0).ToDictionary(atom => atom.Index, atom => atom.Key);
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var atom, var arity] }
                && body[index + 1] is { Definition.Opcode: Label, Operands: [var entry] }
                && names.TryGetValue((int)atom.Value, out string? name)
                && declarations.TryGetValue((name, (int)arity.Value), out var definition))
            {
                entries.Add((int)entry.Value, definition);
            }
        }

        var analyzer = new GeneralEmitter(
            this,
            new Dictionary<(string Name, int Arity), int>(),
            declarations,
            default,
            frameSize: 0
        );
        var results = new Dictionary<int, bool>();

        return HasPair;

        bool HasPair(int label)
        {
            if (!results.TryGetValue(label, out bool pair))
            {
                pair = entries.TryGetValue(label, out var definition) && analyzer.HasFixedTupleResult(definition, 2);
                results.Add(label, pair);
            }

            return pair;
        }
    }

    private static byte[]? RewriteTerminalPairConstruction(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        Func<int, bool> hasPair
    )
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
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
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int call = entry; call + 7 < end; call++)
            {
                if (body[call] is not { Definition.Opcode: LocalCall, Operands: [_, var target] }
                    || body[call + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var firstSource, { Value.IsZero: true },
                        { Kind: BeamOperandParameters.X, TypeIndex: null } head]
                    } || firstSource != x0 || head == x0
                    || body[call + 2] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var secondSource, { Value.IsOne: true }, var tail]
                    } || secondSource != x0 || tail != x0
                    || body[call + 3] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var throughTail, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedTail]
                    } || throughTail != x0
                    || body[call + 4] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var throughHead, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedHead]
                    } || throughHead != head || savedHead == savedTail
                    || body[call + 5] is not { Definition.Opcode: TestHeap, Operands: [.., { Value.IsZero: true }] }
                    || !hasPair((int)target.Value)
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        entry,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frames)
                {
                    continue;
                }

                int allocation = frames.Owners[call - entry];
                if (allocation < entry || body[allocation].Offset < copied || body[allocation].Definition.Opcode != Allocate
                    || PlanTerminalPairConstruction(
                        body,
                        frames.Owners,
                        entry,
                        end,
                        allocation,
                        call,
                        [savedHead, savedTail]
                    ) is not { } plan)
                {
                    continue;
                }

                var (release, replacements) = plan;
                int width = frames.Uses[allocation].Length - replacements.Count;
                int operand = FirstStackOperandAtOrAfter(stack, body[allocation].Offset);
                for (int index = allocation; index <= release; index++)
                {
                    int offset = body[index].Offset;
                    int next = body[index + 1].Offset;
                    while (operand < stack.Count && stack[operand].Start < offset)
                    {
                        operand++;
                    }

                    if (index == call + 1)
                    {
                        output.Write(code.Span[copied..offset]);
                        WriteHeapRoots(
                            output,
                            code,
                            body[call + 5].Offset,
                            body[call + 6].Offset,
                            0,
                            1
                        );
                        WriteField(0, (int)savedHead.Value);
                        WriteField(1, (int)savedTail.Value);
                        copied = body[call + 6].Offset;
                        index = call + 5;
                        continue;
                    }

                    if (body[index].Definition.Opcode == InitYRegisters)
                    {
                        output.Write(code.Span[copied..offset]);
                        WriteFrameInitialization(
                            output,
                            body[index].Operands.Skip(1).Select(item => (int)item.Value)
                                .Where(slot => !replacements.ContainsKey(slot)).Select(Remap).ToArray()
                        );
                        copied = next;
                        continue;
                    }

                    if (index == allocation || index == release)
                    {
                        copied = WritePostCallProjectionOperand(
                            output,
                            code,
                            offset,
                            copied,
                            LinearFrameSizeOperand(body[index].Definition.Opcode),
                            width
                        );
                    }

                    while (operand < stack.Count && stack[operand].Start < next)
                    {
                        var (from, to, slot) = stack[operand++];
                        output.Write(code.Span[copied..from]);
                        WriteReplacement(slot);
                        copied = to;
                    }
                }

                call = release;

                void WriteField(int field, int slot)
                {
                    output.WriteByte(GetTupleElement);
                    WriteRegister(output, x0);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
                    WriteReplacement(slot);
                }

                void WriteReplacement(int slot)
                {
                    if (replacements.TryGetValue(slot, out int register))
                    {
                        BeamCompactWriter.Write(output, XRegisterTag, register);
                    }
                    else
                    {
                        BeamCompactWriter.Write(output, YRegisterTag, Remap(slot));
                    }
                }

                int Remap(int slot) => slot - replacements.Keys.Count(removed => removed < slot);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Release, Dictionary<int, int> Replacements)? PlanTerminalPairConstruction(
        IReadOnlyList<BeamInstruction> body,
        int[] owners,
        int entry,
        int end,
        int allocation,
        int call,
        BeamOperand[] fields
    )
    {
        int release = Array.LastIndexOf(owners, allocation) + entry;
        if (release <= call + 6 || release + 1 >= end || body[release].Definition.Opcode != Deallocate
            || body[release + 1].Definition.Opcode != Return
            || Enumerable.Range(allocation + 1, release - allocation).Any(index =>
                owners[index - entry] != allocation || body[index].Definition.Opcode == Label))
        {
            return null;
        }

        var prefix = body.Skip(allocation + 1).Take(call - allocation - 1).ToArray();
        var construction = body.Skip(call + 6).Take(release - call - 6).ToArray();
        if (prefix.Any(instruction => instruction.Definition.Opcode is not (LocalCall or ExternalCall)
                && (EndsBeamBlock(instruction.Definition.Opcode)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label)))
            || construction.Any(instruction => instruction.Definition.Opcode is not (PutTuple or PutList or Move or UpdateRecord or GetTupleElement)))
        {
            return null;
        }

        var usedX = construction.SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.X)
            .Select(operand => (int)operand.Value).ToHashSet();
        var replacements = new Dictionary<int, int>();
        foreach (BeamOperand field in fields)
        {
            var lastReference = prefix.LastOrDefault(instruction =>
                instruction.Operands.Any(operand => SameProjectionRegister(operand, field)));
            bool removed = !prefix.Any(instruction => instruction.Definition.Opcode != InitYRegisters
                    && instruction.Operands.Any(operand => SameProjectionRegister(operand, field)))
                && construction.Any(instruction => instruction.Operands.Any(operand => SameProjectionRegister(operand, field)))
                && !construction.Any(instruction =>
                    SameProjectionRegister(instruction.Operands[StackWriteOperand(instruction)], field));
            if (!removed)
            {
                // The slot remains in the frame. Its last reference must
                // already have cleared its old term before the collector.
                if (lastReference?.Definition.Opcode != InitYRegisters)
                {
                    return null;
                }

                continue;
            }

            int scratch = Enumerable.Range(1, Math.Min(usedX.Count + 1, FirstReservedXRegister - 1))
                .FirstOrDefault(register => !usedX.Contains(register));
            if (scratch == 0)
            {
                return null;
            }

            usedX.Add(scratch);
            replacements.Add((int)field.Value, scratch);
        }

        return replacements.Count == 0 ? null : (release, replacements);
    }
}
