using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static int[]? PlanNestedRecordColdCases(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        var labels = new Dictionary<int, int>();
        var coldRegions = new Dictionary<int, int>();
        int caseFailures = 0;
        int recordFailure = -1;
        int selections = 0;
        bool hasSelectedRegisterFailure = false;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == SelectValue)
            {
                selections++;
            }

            if (instruction.Definition.Opcode != Label)
            {
                continue;
            }

            labels.Add((int)instruction.Operands[0].Value, index);
            if (index == start || !EndsBeamBlock(body[index - 1].Definition.Opcode))
            {
                continue;
            }

            int exit = index + 1;
            while (exit < end && body[exit].Definition.Opcode == Line)
            {
                exit++;
            }

            if (exit >= end || exit + 1 < end && body[exit + 1].Definition.Opcode != Label)
            {
                continue;
            }

            if (body[exit] is
                {
                    Definition.Opcode: CaseEnd,
                    Operands: [{ Kind: BeamOperandParameters.X } rejected]
                })
            {
                // A selected tag can occupy a different register than the rejected tuple.
                // Preserve the existing X0 failure shape and prove ownership for another register.
                if (!rejected.Value.IsZero)
                {
                    bool ownsSelectedFailure = Enumerable.Range(start, index - start).Any(step => body[step] is
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var selector, var target, ..]
                    } && target.Value == instruction.Operands[0].Value && SameProjectionRegister(selector, rejected));
                    if (!ownsSelectedFailure)
                    {
                        continue;
                    }

                    hasSelectedRegisterFailure = true;
                }

                caseFailures++;
            }
            else if (body[exit] is { Definition.Opcode: BadRecord, Operands: [{ Kind: BeamOperandParameters.Y }] })
            {
                if (recordFailure >= 0)
                {
                    return null;
                }

                recordFailure = index;
            }
            else
            {
                continue;
            }

            coldRegions.Add(index, exit);
        }

        if (selections < 2 || caseFailures < 2 || recordFailure < 0
            || !HasNestedRecordFailureGuard(
                body,
                start,
                end,
                recordFailure,
                coldRegions[recordFailure]
            ))
        {
            return null;
        }

        var cold = coldRegions.SelectMany(region => Enumerable.Range(region.Key, region.Value - region.Key + 1)).ToHashSet();
        if (!hasSelectedRegisterFailure)
        {
            int lastWarm = end - 1;
            while (cold.Contains(lastWarm))
            {
                lastWarm--;
            }

            if (!coldRegions.Keys.Any(index => index < lastWarm))
            {
                return null;
            }
        }

        int[]? reversePostorder = ReadForwardCaseReversePostorder(
            body,
            start,
            end,
            labels,
            hasSelectedRegisterFailure ? ReadFunctionFailure(body, start) : null
        );
        int[] warm = Enumerable.Range(start + 1, end - start - 1).Where(index => !cold.Contains(index)).ToArray();
        if (reversePostorder is null || !reversePostorder.Where(index => index != start && !cold.Contains(index)).SequenceEqual(warm))
        {
            return null;
        }

        return warm.Concat(reversePostorder.Where(cold.Contains)).ToArray();
    }

    private static bool HasNestedRecordFailureGuard(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int failure,
        int exit
    )
    {
        BeamOperand label = body[failure].Operands[0];
        BeamOperand failed = body[exit].Operands[0];
        int check = -1;
        for (int index = start + 1; index < failure; index++)
        {
            if (body[index] is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var record, _, _]
                } && rejected.Value == label.Value && SameProjectionRegister(record, failed))
            {
                check = index;
                break;
            }
        }

        return check >= 0 && Enumerable.Range(check + 1, end - check - 1).Any(index =>
            body[index].Definition.Opcode == UpdateRecord && SameProjectionRegister(body[index].Operands[2], failed));
    }

    private static int? ReadFunctionFailure(IReadOnlyList<BeamInstruction> body, int entry)
    {
        int index = entry - 1;
        if (index < 0 || body[index].Definition.Opcode != FunctionInfo)
        {
            return null;
        }

        index--;
        while (index >= 0 && body[index].Definition.Opcode == Line)
        {
            index--;
        }

        return index >= 0 && body[index].Definition.Opcode == Label
            ? (int)body[index].Operands[0].Value
            : null;
    }

    private static int[]? ReadForwardCaseReversePostorder(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        Dictionary<int, int> labels,
        int? functionFailure = null
    )
    {
        var pending = new Stack<(int Index, bool Visited)>();
        var seen = new HashSet<int>();
        var postorder = new List<int>(end - start);
        pending.Push((start, false));
        while (pending.TryPop(out var step))
        {
            if (step.Visited)
            {
                postorder.Add(step.Index);
                continue;
            }

            if (!seen.Add(step.Index))
            {
                continue;
            }

            pending.Push((step.Index, true));
            BeamInstruction instruction = body[step.Index];
            byte opcode = instruction.Definition.Opcode;
            if (!EndsBeamBlock(opcode))
            {
                if (step.Index + 1 >= end)
                {
                    return null;
                }

                pending.Push((step.Index + 1, false));
            }

            if (opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame)
            {
                continue;
            }

            // beam_ssa visits a failed edge before the fallthrough, and a
            // select default before the physically sorted case operands.
            foreach (BeamOperand operand in instruction.Operands.Reverse())
            {
                if (operand.Kind != BeamOperandParameters.Label)
                {
                    continue;
                }

                if (IsEntryFramePredicate(opcode) && functionFailure == (int)operand.Value)
                {
                    continue;
                }

                if (!(IsEntryFramePredicate(opcode) || opcode is SelectValue or SelectTupleArity or Jump)
                    || !labels.TryGetValue((int)operand.Value, out int target) || target <= step.Index)
                {
                    return null;
                }

                pending.Push((target, false));
            }
        }

        if (seen.Count != end - start)
        {
            return null;
        }

        postorder.Reverse();

        return postorder.ToArray();
    }
}
