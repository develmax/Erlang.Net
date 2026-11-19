using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? EliminateEntryArgumentCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
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
        List<(int Start, int End, int Slot)> stackOperands,
        IReadOnlyDictionary<int, BeamOperand> uses,
        BeamOperand slot,
        Dictionary<int, (int End, BeamOperand Source)> edits
    )
    {
        var byOffset = Comparer<(int Start, int End, int Slot)>.Create(
            static (left, right) => left.Start.CompareTo(right.Start));
        foreach (var (use, source) in uses)
        {
            int first = body[use].Offset;
            int last = body[use + 1].Offset;
            int indexOfOperand = stackOperands.BinarySearch((first, 0, 0), byOffset);
            for (int index = indexOfOperand < 0 ? ~indexOfOperand : indexOfOperand;
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
