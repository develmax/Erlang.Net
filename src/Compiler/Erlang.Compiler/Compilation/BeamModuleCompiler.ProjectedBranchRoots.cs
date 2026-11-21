using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearDeadProjectedBranchRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        var edits = new SortedDictionary<int, (int[] Slots, bool Replace)>();
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            info = end - 1;
            if (!Enumerable.Range(entry, end - entry).Any(index => IsEntryFramePredicate(body[index].Definition.Opcode))
                || ReadDisjointFrameOwnership(body, entry, end) is not { } frame)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            var branchFrames = new HashSet<int>();
            for (int index = entry; index < end; index++)
            {
                int owner = frame.Owners[index - entry];
                var test = body[index];
                if (owner >= 0 && IsEntryFramePredicate(test.Definition.Opcode)
                    && test.Operands[0] is { Kind: BeamOperandParameters.Label } failure
                    && flow.TryGetSingleForwardTarget(index, (int)failure.Value, out int alternate) && alternate < end
                    && frame.Owners[alternate - entry] == owner)
                {
                    branchFrames.Add(owner);
                }
            }

            // A shared return consumes the same saved record after the join;
            // arm-local projected roots can die at different calls before it.
            branchFrames.IntersectWith(flow.JoinedNestedRecordOwners(entry, end, frame.Owners));
            var projected = Enumerable.Range(entry, end - entry)
                .Where(index => body[index] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [_, _, { Kind: BeamOperandParameters.Y }]
                } && branchFrames.Contains(frame.Owners[index - entry]))
                .GroupBy(index => frame.Owners[index - entry])
                .ToDictionary(group => group.Key, group => group.Select(index => (int)body[index].Operands[2].Value).Distinct());

            var knownClears = new HashSet<(int Index, int Slot)>();
            for (int call = entry + 1; call + 1 < end; call++)
            {
                int owner = frame.Owners[call - entry];
                if (!projected.TryGetValue(owner, out var candidates) || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value.Sign: > 0 }, _]
                    })
                {
                    continue;
                }

                var roots = new List<int>();
                foreach (int register in candidates)
                {
                    var slot = new BeamOperand(BeamOperandParameters.Y, register);
                    if (flow.HasLiveSharedFrameRoot(
                        call,
                        slot,
                        knownClears,
                        projectedOnly: true
                    ) && flow.HasNoUseBeforeClosingFrame(
                        call + 1,
                        slot,
                        entry,
                        owner,
                        frame.Owners
                    ))
                    {
                        roots.Add(register);
                        knownClears.Add((call, register));
                    }
                }

                if (roots.Count == 0)
                {
                    continue;
                }

                // Place the kill after argument setup and before the call's
                // line, otherwise a final argument read could observe nil.
                int insert = call;
                while (body[insert - 1].Definition.Opcode == Line)
                {
                    insert--;
                }

                bool replace = body[insert - 1].Definition.Opcode == InitYRegisters;
                if (replace)
                {
                    insert--;
                    roots.AddRange(body[insert].Operands.Skip(1).Select(operand => (int)operand.Value));
                }

                edits.Add(insert, (roots.Distinct().Order().ToArray(), replace));
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var (index, (slots, replace)) in edits)
        {
            output.Write(code.Span[copied..body[index].Offset]);
            WriteFrameInitialization(output, slots);
            copied = body[replace ? index + 1 : index].Offset;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal IEnumerable<int> JoinedNestedRecordOwners(int entry, int end, IReadOnlyList<int> owners)
        {
            for (int join = entry + 1; join + 4 < end; join++)
            {
                int owner = owners[join - entry];
                if (owner < 0 || body[join].Definition.Opcode != Label || predecessors[join].Count < 2
                    || predecessors[join].Any(index => index < entry || index >= join || owners[index - entry] != owner))
                {
                    continue;
                }

                int operation = join + 1;
                while (operation < end && body[operation].Definition.Opcode == Line)
                {
                    operation++;
                }

                int update = operation + 2;
                if (update + 4 < end
                    && body[operation] is
                    {
                        Definition.Opcode: GcBifTwo,
                        Operands: [{ Value.IsZero: true }, { Value.Sign: > 0 }, _, _, _,
                        { Kind: BeamOperandParameters.X, Value.Sign: > 0 } result]
                    }
                    && body[operation + 1] is { Definition.Opcode: TestHeap, Operands: [_, var live] }
                    && live.Value > result.Value
                    && body[update].Definition.Opcode == UpdateRecord
                    && body[update].Operands.Skip(FirstRecordUpdateValueOperand).Any(operand => SameProjectionRegister(operand, result))
                    && IsBareX0(body[update].Operands[3])
                    && body[update + 1].Definition.Opcode == UpdateRecord
                    && body[update + 1].Operands.Skip(FirstRecordUpdateValueOperand).Any(operand =>
                        operand is { Kind: BeamOperandParameters.X, Value.IsZero: true })
                    && body[update + 2].Definition.Opcode == PutTuple
                    && body[update + 3].Definition.Opcode == Deallocate
                    && body[update + 4].Definition.Opcode == Return)
                {
                    yield return owner;
                }
            }
        }
    }
}
