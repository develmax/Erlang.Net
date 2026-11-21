using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearRootsBeforeSharedSavedReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Jump)
            || !code.Span.Contains(Deallocate) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int info = 0; info + 6 < body.Count; info++)
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
            if (!Enumerable.Range(entry, Math.Max(0, end - entry - 3)).Any(call =>
                body[call] is { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsZero: true }, _] }
                && body[call + 1].Definition.Opcode == Move && body[call + 2].Definition.Opcode == Deallocate
                && body[call + 3].Definition.Opcode == Return))
            {
                continue;
            }

            int allocation = FindLinearFrameSuffix(body, entry, end);
            if (allocation < 0 || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                || ownership.Item2.Count != 1 || !ownership.Item2.ContainsKey(allocation)
                || !HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            int width = (int)body[allocation].Operands[0].Value;
            flow ??= new BeamProjectionFlow(body);
            if (!flow.HasJoinedSavedReturn(
                allocation,
                entry,
                end,
                width,
                ownership.Item1
            ))
            {
                continue;
            }

            // SSA codegen subtracts live variables from the definitely assigned
            // frame roots at each call, filtering out reused physical slots.
            var kills = PlanSharedFrameRootKills(
                body,
                flow,
                allocation + 1,
                end,
                width
            );
            if (kills.Count == 0)
            {
                continue;
            }

            for (int call = allocation + 1; call < end; call++)
            {
                if (ownership.Item1[call - entry] != allocation
                    || body[call].Definition.Opcode is not (LocalCall or ExternalCall))
                {
                    continue;
                }

                int line = call;
                while (body[line - 1].Definition.Opcode == Line)
                {
                    line--;
                }

                int first = line;
                int initialization = -1;
                while (first > allocation + 1 && body[first - 1].Definition.Opcode is Move or InitYRegisters)
                {
                    if (body[first - 1].Definition.Opcode == InitYRegisters)
                    {
                        if (initialization >= 0)
                        {
                            break;
                        }

                        initialization = first - 1;
                    }

                    first--;
                }

                int[] added = Enumerable.Range(first, line - first + 1)
                    .Where(kills.ContainsKey).SelectMany(index => kills[index]).Distinct().ToArray();
                if (added.Length == 0)
                {
                    continue;
                }

                // A kill before the last argument can cross that move only
                // when its old slot was not part of the original kill set.
                if (initialization >= 0
                    && body[initialization].Operands.Skip(1).Any(slot => Enumerable
                        .Range(initialization + 1, line - initialization - 1)
                        .Any(index => SameProjectionRegister(body[index].Operands[0], slot))))
                {
                    continue;
                }

                if (initialization >= 0)
                {
                    output.Write(code.Span[copied..body[initialization].Offset]);
                    copied = body[initialization + 1].Offset;
                }

                output.Write(code.Span[copied..body[line].Offset]);
                var slots = initialization < 0
                    ? added.Order().ToArray()
                    : added.Concat(body[initialization].Operands.Skip(1).Select(slot => (int)slot.Value))
                        .Distinct().Order().ToArray();
                WriteFrameInitialization(output, slots);
                copied = body[line].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasJoinedSavedReturn(
            int allocation,
            int entry,
            int end,
            int width,
            IReadOnlyList<int> owners
        )
        {
            for (int join = allocation + 1; join + 4 < end; join++)
            {
                if (body[join].Definition.Opcode != Label || owners[join - entry] != allocation
                    || predecessors[join].Count < 2)
                {
                    continue;
                }

                int call = join + 1;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 3 >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value.IsZero: true }, _]
                    } || body[call + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } saved, var result]
                    } || !IsBareX0(result) || saved.Value >= width
                    || body[call + 2].Definition.Opcode != Deallocate || body[call + 3].Definition.Opcode != Return)
                {
                    continue;
                }

                if (predecessors[join].All(DefinesSavedResult))
                {
                    return true;
                }

                bool DefinesSavedResult(int from)
                {
                    if (from <= allocation || from >= join || owners[from - entry] != allocation)
                    {
                        return false;
                    }

                    int move = body[from].Definition.Opcode == Jump ? from - 1 : from;

                    return body[move] is
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X or BeamOperandParameters.Y }, var destination]
                    } && destination == saved && (move == from || predecessors[from].SequenceEqual([move]));
                }
            }

            return false;
        }
    }
}
