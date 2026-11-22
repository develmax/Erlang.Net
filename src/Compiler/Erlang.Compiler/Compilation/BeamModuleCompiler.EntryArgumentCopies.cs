using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? EliminateEntryArgumentCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool removeSpills,
        bool allowFramelessBranches = false
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int functionStart = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = functionStart;
            functionStart = end + 1;
            if (entry + 1 >= end || body[entry].Definition.Opcode != Label)
            {
                continue;
            }

            int allocation = entry + 1;
            while (allocation < end && (body[allocation].Definition.Opcode is Label or Line
                || IsEntryFramePredicate(body[allocation].Definition.Opcode)
                || IsUnframedEntryProjection(body[allocation])))
            {
                allocation++;
            }

            if (allocation == end || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                if (!allowFramelessBranches)
                {
                    continue;
                }

                allocation = FindLinearFrameSuffix(body, entry, end);
                if (PlanLinearStackSlots(
                    body,
                    allocation,
                    end,
                    includeUnchanged: true
                ) is null && !OwnsSingleBranchFrame(
                    body,
                    entry,
                    allocation,
                    end
                ))
                {
                    continue;
                }
            }
            else if (allowFramelessBranches)
            {
                continue;
            }

            int first = allocation + 1;
            int setupEnd = first;
            while (setupEnd < end && body[setupEnd].Definition.Opcode is InitYRegisters or Move or PutTuple or PutList or Line)
            {
                setupEnd++;
            }

            if (!removeSpills && !allowFramelessBranches
                && ReadDependentEntryCallCopies(
                    body,
                    allocation,
                    setupEnd,
                    end
                ) is { } pair)
            {
                output.Write(code.Span[copied..body[pair.FirstLoad].Offset]);
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, 1);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                output.WriteByte(Move);
                WriteRegister(output, pair.SecondArgument);
                BeamCompactWriter.Write(output, XRegisterTag, 1);
                copied = body[pair.FirstLoad + 2].Offset;
                continue;
            }

            var remove = new Dictionary<int, BeamOperand>();
            var replace = new Dictionary<int, (int End, BeamOperand Source)>();
            var earlyLoads = new Dictionary<int, (int Use, BeamOperand Source)>();
            var oldLoads = new HashSet<int>();
            for (int save = first; save < setupEnd; save++)
            {
                if (body[save] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                    || source is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                    || slot.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if ((flow ??= new BeamProjectionFlow(body)).PlanEntryRegisterUses(
                    entry,
                    save,
                    end,
                    source,
                    slot
                ) is { } plan && plan.RemoveSpill == removeSpills)
                {
                    foreach (var (use, (before, register, extended)) in plan.EarlyLoads)
                    {
                        if (extended)
                        {
                            // Revisit other spills after this move changes X availability.
                            remove.Clear();
                            replace.Clear();
                            earlyLoads.Clear();
                            oldLoads.Clear();
                            remove.Add(save, slot);
                            earlyLoads.Add(before, (use, register));
                            oldLoads.Add(use);
                            break;
                        }
                    }

                    if (remove.ContainsKey(save))
                    {
                        break;
                    }

                    if (plan.RemoveSpill)
                    {
                        remove.Add(save, slot);
                    }

                    RecordEntryArgumentUses(
                        body,
                        stackOperands,
                        plan.Uses,
                        slot,
                        replace
                    );
                    foreach (var (use, (before, register, _)) in plan.EarlyLoads)
                    {
                        earlyLoads.Add(before, (use, register));
                        oldLoads.Add(use);
                    }
                }
            }

            if (replace.Count == 0 && remove.Count == 0)
            {
                continue;
            }

            var replacements = replace.OrderBy(edit => edit.Key).ToArray();
            int replacement = 0;
            for (int index = first; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (earlyLoads.TryGetValue(index, out var earlyLoad))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, earlyLoad.Source);
                    WriteRegister(output, body[earlyLoad.Use].Operands[1]);
                    copied = instruction.Offset;
                }

                if (oldLoads.Contains(index))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                    continue;
                }

                if (remove.TryGetValue(index, out BeamOperand? slot))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(InitYRegisters);
                    output.WriteByte(ExtendedListOperand);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    WriteRegister(output, slot);
                    copied = body[index + 1].Offset;
                    continue;
                }

                int next = body[index + 1].Offset;
                while (replacement < replacements.Length && replacements[replacement].Key < next)
                {
                    var (start, (finish, source)) = replacements[replacement++];
                    output.Write(code.Span[copied..start]);
                    WriteRegister(output, source);
                    copied = finish;
                }
            }
        }

        if (copied == 0)
        {
            return removeSpills && !allowFramelessBranches ? SinkEntryArgumentClobbers(code, body) : null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int FirstLoad, BeamOperand SecondArgument)? ReadDependentEntryCallCopies(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int call,
        int end
    )
    {
        if (call >= end || body[allocation].Definition.Opcode != Allocate
            || body[call] is not
            {
                Definition.Opcode: LocalCall or ExternalCall,
                Operands: [{ Value: var arity }, _]
            } || arity != 2)
        {
            return null;
        }

        int last = call - 1;
        while (last > allocation && body[last].Definition.Opcode == Line)
        {
            last--;
        }

        if (last <= allocation + 1
            || body[last - 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } second,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }
                ]
            }
            || body[last] is not { Definition.Opcode: Move, Operands: [var first, var target] }
            || first is not { Kind: BeamOperandParameters.Y, TypeIndex: null } || !IsBareX0(target))
        {
            return null;
        }

        BeamOperand? firstSource = null;
        BeamOperand? secondSource = null;
        BeamOperand live = body[allocation].Operands[1];
        for (int index = allocation + 1; index < last - 1; index++)
        {
            var instruction = body[index];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                } && source.Value < live.Value)
            {
                if (saved == first)
                {
                    firstSource = source;
                }

                if (saved == second)
                {
                    secondSource = source;
                }
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                if (instruction.Operands.Contains(first))
                {
                    firstSource = null;
                }

                if (instruction.Operands.Contains(second))
                {
                    secondSource = null;
                }
            }
            else if (instruction.Definition.Opcode != Line)
            {
                return null;
            }
        }

        // The first argument reads the old X1 before the second replaces it.
        // Both stack definitions remain available to later continuations.
        return firstSource is { Value.IsOne: true } && secondSource is { Value: var original } && original >= 2
            ? (last - 1, secondSource)
            : null;
    }

    private static bool OwnsSingleBranchFrame(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int allocation,
        int end
    ) =>
        allocation >= 0
        && ReadDisjointFrameOwnership(body, entry, end) is { } ownership
        && ownership.Uses.Count == 1
        && ownership.Owners[allocation - entry] == UnframedOwner
        && Enumerable.Range(entry, allocation - entry).Any(index =>
            body[index].Definition.Opcode == Return && ownership.Owners[index - entry] == UnframedOwner);

    private static bool IsUnframedEntryProjection(BeamInstruction instruction) =>
        instruction.Definition.Opcode is GetTupleElement or GetList or GetHead or GetTail
        && instruction.Operands.All(operand => operand.Kind != BeamOperandParameters.Y);

    private static void RecordEntryArgumentUses(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        IReadOnlyDictionary<int, BeamOperand> uses,
        BeamOperand slot,
        Dictionary<int, (int End, BeamOperand Source)> edits
    )
    {
        foreach (var (use, source) in uses)
        {
            int first = body[use].Offset;
            int last = body[use + 1].Offset;
            int indexOfOperand = FirstStackOperandAtOrAfter(stackOperands, first);
            for (int index = indexOfOperand;
                index < stackOperands.Count && stackOperands[index].Start < last;
                index++)
            {
                var (start, finish, register) = stackOperands[index];
                if (register == slot.Value)
                {
                    edits.Add(start, (finish, source));
                }
            }
        }
    }
}
