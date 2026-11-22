using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ClosedCatchTupleGroup(
        int Arity,
        int Select,
        int Failure,
        int[] Targets
    );

    private static void CollectClosedCatchArityReturnRelays(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references,
        Dictionary<int, (int Target, int End)> relays
    )
    {
        BeamProjectionFlow? flow = null;
        Dictionary<int, int>? incoming = null;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadClosedCatchTupleGroups(
                body,
                start,
                end,
                ref flow
            ) is not { } scope)
            {
                continue;
            }

            incoming ??= references.GroupBy(item => item.Label)
                .ToDictionary(group => group.Key, group => group.Count());
            var returns = new List<(int Arity, int Index, int Label)>();
            foreach (var group in scope.Groups)
            {
                foreach (int target in group.Targets.Distinct())
                {
                    if (incoming.GetValueOrDefault(target) != group.Targets.Count(label => label == target)
                        || !flow!.TryGetForwardLabel(group.Select, target, out int label)
                        || !IsClosedCatchIdentityReturn(
                            body,
                            label,
                            end,
                            scope.Frame
                        )
                        || !EndsBeamBlock(body[label - 1].Definition.Opcode)
                        || relays.ContainsKey(label))
                    {
                        continue;
                    }

                    returns.Add((group.Arity, label, target));
                }
            }

            if (returns.Select(item => item.Arity).Distinct().Count() < 2)
            {
                continue;
            }

            var last = returns.MaxBy(item => item.Index);
            foreach (var earlier in returns.Where(item => item.Arity != last.Arity))
            {
                relays.Add(earlier.Index, (last.Label, earlier.Index + 3));
            }
        }
    }

    private byte[]? HoistClosedCatchTupleFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnd)
            || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadClosedCatchTupleGroups(
                body,
                start,
                end,
                ref flow
            ) is not { } scope)
            {
                continue;
            }

            var edits = new SortedDictionary<int, (bool Insert, int Projection)>();
            foreach (var group in scope.Groups)
            {
                if (!flow!.TryGetForwardLabel(group.Select, group.Failure, out int failure)
                    || !IsOriginalCaseFailure(failure))
                {
                    continue;
                }

                int projection = -1;
                bool valid = true;
                foreach (int target in group.Targets.Distinct())
                {
                    if (!flow.TryGetForwardLabel(group.Select, target, out int label) || label + 1 >= end)
                    {
                        valid = false;
                        break;
                    }

                    if (IsClosedCatchIdentityReturn(
                        body,
                        label,
                        end,
                        scope.Frame
                    ))
                    {
                        continue;
                    }

                    if (projection >= 0 || !flow.TryGetSingleForwardTarget(group.Select, target, out _)
                        || body[label + 1] is not
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [var original, { Kind: BeamOperandParameters.Unsigned } field, var destination]
                        } || original != x0 || destination != scope.Handler || field.Value <= 0 || field.Value >= group.Arity)
                    {
                        valid = false;
                        break;
                    }

                    projection = label + 1;
                }

                if (valid && projection >= 0)
                {
                    edits.Add(group.Select, (true, projection));
                    edits.Add(projection, (false, projection));
                }
            }

            foreach (var (index, (insert, projection)) in edits)
            {
                int offset = body[index].Offset;
                output.Write(code.Span[copied..offset]);
                if (insert)
                {
                    output.Write(code.Span[body[projection].Offset..body[projection + 1].Offset]);
                    copied = offset;
                }
                else
                {
                    copied = body[index + 1].Offset;
                }
            }

            bool IsOriginalCaseFailure(int label)
            {
                int index = label + 1;
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index < end && body[index] is { Definition.Opcode: CaseEnd, Operands: [var original] }
                    && original == x0;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsClosedCatchIdentityReturn(
        IReadOnlyList<BeamInstruction> body,
        int label,
        int end,
        BeamOperand frame
    ) => label + 2 < end && body[label + 1] is { Definition.Opcode: Deallocate, Operands: [var width] }
        && width == frame && body[label + 2].Definition.Opcode == Return;

    private static (BeamOperand Frame, BeamOperand Handler, List<ClosedCatchTupleGroup> Groups)? ReadClosedCatchTupleGroups(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        if (end - start < 14 || body[start].Definition.Opcode != Label
            || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [var frame, _] })
        {
            return null;
        }

        int enter = -1;
        int closed = -1;
        BeamOperand? handler = null;
        for (int index = start + 2; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == CatchEnter && enter < 0 && instruction.Operands[0] is
                { Kind: BeamOperandParameters.Y, TypeIndex: null } slot && slot.Value >= 0 && slot.Value < frame.Value)
            {
                enter = index;
                handler = slot;
                continue;
            }

            if (instruction.Definition.Opcode == CatchEnd && enter >= 0 && closed < 0 && instruction.Operands[0] == handler)
            {
                closed = index;
                continue;
            }

            if (instruction.Definition.Opcode is CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase or Allocate
                or AllocateHeap or DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout)
            {
                return null;
            }
        }

        if (enter < 0 || handler is null || closed < enter + 3 || closed + 2 >= end
            || body[closed - 2].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[closed + 1] is not
            {
                Definition.Opcode: IsTuple,
                Operands: [{ Kind: BeamOperandParameters.Label } invalid, var tuple]
            } || !IsBareX0(tuple)
            || body[closed + 2] is not
            {
                Definition.Opcode: SelectTupleArity,
                Operands: [var checkedTuple, var rejected, _, _, _, _, _, ..]
            } arities || !SameProjectionRegister(checkedTuple, tuple) || rejected != invalid
            || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                enter,
                closed - 2,
                (int)body[enter].Operands[1].Value,
                out int landing
            ) || landing != closed - 1)
        {
            return null;
        }

        int selectArity = closed + 2;
        var groups = new List<ClosedCatchTupleGroup>();
        for (int option = 3; option + 1 < arities.Operands.Count; option += 2)
        {
            var size = arities.Operands[option];
            var target = arities.Operands[option + 1];
            if (size.Value <= 1 || size.Value > int.MaxValue || target.Kind != BeamOperandParameters.Label
                || !flow.TryGetSingleForwardTarget(selectArity, (int)target.Value, out int label)
                || label + 2 >= end
                || body[label + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.IsZero: true },
                    { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null } tag]
                } || source != tuple
                || body[label + 2] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var compared, var failure, _, _, _, ..]
                } selector || compared != tag || failure != invalid)
            {
                continue;
            }

            var keys = selector.Operands.Skip(3).Chunk(2).ToArray();
            if (keys.Any(pair => pair is not
                [{ Kind: BeamOperandParameters.Atom }, { Kind: BeamOperandParameters.Label }]
                || pair[1] == failure))
            {
                continue;
            }

            groups.Add(new(
                (int)size.Value,
                label + 2,
                (int)failure.Value,
                keys.Select(pair => (int)pair[1].Value).ToArray()
            ));
        }

        return groups.Count >= 2 ? (frame, handler, groups) : null;
    }
}
