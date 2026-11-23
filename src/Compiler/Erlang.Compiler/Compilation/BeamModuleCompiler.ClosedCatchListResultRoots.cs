using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepClosedCatchNilResultInX0(ReadOnlyMemory<byte> code, int labelCount) =>
        KeepClosedCatchNilResultInX0(code, labelCount, []);

    private byte[]? KeepClosedCatchNilResultInX0(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Dictionary<int, int> tupleFilterResults
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        BeamProjectionFlow? flow = null;
        var entries = new HashSet<int>();
        var edits = new List<CodeEdit>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (end - start < 12
                || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [var width, _] })
            {
                continue;
            }

            int landing = -1;
            for (int index = start + 2; index < end; index++)
            {
                if (body[index].Definition.Opcode == CatchEnd)
                {
                    landing = index;
                    break;
                }
            }

            if (landing < 0 || !body.Skip(start + 2).Take(landing - start - 2)
                .Any(instruction => instruction.Definition.Opcode == CatchEnter))
            {
                continue;
            }

            for (int spill = landing + 2; spill + 3 < end; spill++)
            {
                if (body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                    }
                    || !IsBareX0(result) || body[spill + 1] is not
                    {
                        Definition.Opcode: IsNil,
                        Operands: [{ Kind: BeamOperandParameters.Label } nonempty, var tested]
                    } || tested != slot)
                {
                    continue;
                }

                int tail = spill + 2;
                while (tail < end && (body[tail].Definition.Opcode == Line || body[tail] is
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.X }]
                    } && !SameProjectionRegister(source, slot)))
                {
                    tail++;
                }

                flow ??= new BeamProjectionFlow(body);
                if (tail >= end || body[tail].Definition.Opcode is not (TailLocalCallWithFrame or TailExternalCallWithFrame)
                    || body[tail].Operands[^1] != width
                    || !flow.TryGetSingleForwardTarget(spill + 1, (int)nonempty.Value, out int target)
                    || target <= tail)
                {
                    continue;
                }

                int heap = target + 1;
                while (heap < end && body[heap].Definition.Opcode == Line)
                {
                    heap++;
                }

                int construct = heap + 1;
                if (construct + 2 >= end || body[heap] is not
                    {
                        Definition.Opcode: TestHeap,
                        Operands: [_, { Kind: BeamOperandParameters.Unsigned, Value.IsZero: true } live]
                    }
                    || body[construct] is not { Definition.Opcode: PutTuple, Operands: [var output, _, ..] } tuple
                    || output != result
                    || body[construct + 1] is not { Definition.Opcode: Deallocate, Operands: [var release] }
                    || release != width || body[construct + 2].Definition.Opcode != Return
                    || !tuple.Operands.Skip(2).Any(operand => operand == slot)
                    || tuple.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, slot) && operand != slot)
                    || flow.ReadRetainedResultHeapRoots(spill, end, [spill + 1, construct]) is not { Count: 1 } roots
                    || !roots.Contains(heap))
                {
                    continue;
                }

                // The reused Y register may have held a different value before
                // this call or on other paths. Rewrite only these dominated reads.
                var proposed = body.Take(end).ToList();
                proposed[spill + 1] = body[spill + 1] with { Operands = [nonempty, result] };
                proposed[heap] = body[heap] with { Operands = [body[heap].Operands[0], live with { Value = 1 }] };
                proposed[construct] = tuple with
                {
                    Operands = tuple.Operands.Select(operand => operand == slot ? result : operand).ToArray()
                };
                proposed.RemoveAt(spill);
                if (PlanBranchStackSlots(proposed, start, proposed.Count) is not { Trim: null })
                {
                    continue;
                }

                entries.Add((int)label.Value);
                edits.Add(new(
                    body[spill].Offset,
                    body[spill + 1].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                int liveEnd = body[construct].Offset;
                edits.Add(new(
                    liveEnd - CompactRegisterCountWidth(0),
                    liveEnd,
                    BeamWire.UnsignedTag,
                    1
                ));
                AddReads(spill + 1);
                AddReads(construct);
                break;

                void AddReads(int index)
                {
                    int next = body[index + 1].Offset;
                    for (int position = FirstStackOperandAtOrAfter(operands, body[index].Offset);
                        position < operands.Count && operands[position].Start < next;
                        position++)
                    {
                        var (from, to, register) = operands[position];
                        if (register == slot.Value)
                        {
                            edits.Add(new(
                                from,
                                to,
                                XRegisterTag,
                                0
                            ));
                        }
                    }
                }
            }
        }

        edits.Sort((first, second) => first.Start.CompareTo(second.Start));
        if (ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } compacted)
        {
            return null;
        }

        byte[]? compactedCode = RewriteReusedStackSlots(
            rewritten,
            compacted.Body,
            compacted.StackOperands,
            useValueLiveness: false,
            closureEntries: null,
            atomicOnly: false,
            entriesToCompact: entries
        );
        if (compactedCode is not null && tupleFilterResults.Count != 0
            && AnnotateListProducerResults(
                compactedCode,
                labelCount,
                tupleFilterResults,
                entries
            ) is { } annotated)
        {
            return OrderTypeOperandsByFirstUse(annotated, labelCount).ToArray();
        }

        return compactedCode;
    }
}
