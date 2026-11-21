using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? OrderFinalPhysicalLabels(ReadOnlyMemory<byte> code, int labelCount)
    {
        return OrderFinalPhysicalLabelsCore(code, labelCount, formMultiKeySelections: true);
    }

    private (ReadOnlyMemory<byte> Code, int LabelCount)? OrderFinalPhysicalLabelsCore(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool formMultiKeySelections
    )
    {
        var references = new List<(int Start, int End, int Label)>();
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
                recordLabelOperand: (start, end, label) => references.Add((start, end, label))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        if (formMultiKeySelections && !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && ShareGuardedLocalTails(code, body, references) is { } guardedTails)
        {
            return OrderFinalPhysicalLabelsCore(guardedTails, labelCount, formMultiKeySelections: true)
                ?? (guardedTails, labelCount);
        }

        if (formMultiKeySelections && !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && CombineAtomMembershipTests(
                code,
                body,
                references,
                multiKey: true
            ) is { } selected)
        {
            // Only a changed stream needs its physical label offsets to be read again.
            return OrderFinalPhysicalLabelsCore(selected, labelCount, formMultiKeySelections: false)
                ?? (selected, labelCount);
        }

        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && ClearOverwrittenOutgoingRoots(code, body) is { } initialized)
        {
            return OrderFinalPhysicalLabelsCore(initialized, labelCount, formMultiKeySelections: false)
                ?? (initialized, labelCount);
        }

        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && (PartitionClosedAtomTupleReturns(code, body, references)
                ?? PartitionDeferredTupleReturns(code, body, references)) is { } partitioned)
        {
            return OrderFinalPhysicalLabelsCore(partitioned, labelCount, formMultiKeySelections: false)
                ?? (partitioned, labelCount);
        }

        var sharedFrameEntries = new HashSet<int>();
        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && ShareFinalFrameReturns(
                code,
                body,
                references,
                labelCount,
                sharedFrameEntries
            ) is { } shared)
        {
            shared.Code = FinalizeSharedFrameExits(shared.Code, shared.LabelCount, sharedFrameEntries);

            return OrderFinalPhysicalLabelsCore(shared.Code, shared.LabelCount, formMultiKeySelections: false)
                ?? shared;
        }

        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && ShareProjectedConstructionTails(code, body) is { } constructionTails)
        {
            return OrderFinalPhysicalLabelsCore(constructionTails, labelCount, formMultiKeySelections: false)
                ?? (constructionTails, labelCount);
        }

        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && ShareStackCopyTails(
                code,
                body,
                references,
                labelCount
            ) is { } stackCopyTails)
        {
            return OrderFinalPhysicalLabelsCore(stackCopyTails, labelCount + 1, formMultiKeySelections: false)
                ?? (stackCopyTails, labelCount + 1);
        }

        // Entry literal partitioning may consume a failure relay to build its select.
        // Resolve the remaining own-function relays after the branch layout is final.
        var relays = ModuleOptimizationDisabled ? [] : CollectFinalFunctionFailureRelays(code, body);
        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled)
        {
            CollectClosedSelectReturnRelays(body, references, relays);
        }

        var labels = new int[labelCount];
        var edits = new List<CodeEdit>();
        if (!ModuleOptimizationDisabled)
        {
            CollectExactRegisterEdits(body, edits);
            CollectRedundantRecordChecks(
                body,
                references,
                relays,
                edits
            );
        }

        var aliases = new List<(int Original, int Target)>();
        int physical = 1;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var operand] })
            {
                continue;
            }

            int original = (int)operand.Value;
            if (original <= 0 || original >= labelCount || labels[original] != 0)
            {
                return null;
            }

            if (relays.TryGetValue(index, out var relay))
            {
                labels[original] = -1;
                aliases.Add((original, relay.Target));
                edits.Add(new CodeEdit(
                    body[index].Offset,
                    body[relay.End].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                continue;
            }

            labels[original] = physical++;
            if (original != labels[original])
            {
                edits.Add(new CodeEdit(
                    body[index].Offset + 1,
                    body[index + 1].Offset,
                    BeamWire.UnsignedTag,
                    labels[original]
                ));
            }
        }

        // Late block sharing can point forward. Assign surviving label ordinals
        // before resolving aliases, independently of their physical order.
        foreach (var (original, target) in aliases)
        {
            labels[original] = labels[target];
        }

        if (references.Any(reference => reference.Label != 0 && !IsDefined(reference.Label))
            || exports.Any(entry => !IsDefined(MapEmittedLabel(entry.Entry)))
            || locals.Any(entry => !IsDefined(MapEmittedLabel(entry.Entry)))
            || closures.Any(closure => !IsDefined(MapClosureEntryLabel(closure.Entry))))
        {
            return null;
        }

        if (!ModuleOptimizationDisabled)
        {
            CollectDirectConstructionDestinationEdits(
                code,
                body,
                edits
            );
            RedirectIsolatedTupleArityFailures(
                body,
                references,
                labels,
                edits
            );
        }

        if (physical == labelCount && edits.Count == 0)
        {
            return null;
        }

        // Final block layout is fixed. Compose the prior redirects for exports, local
        // metadata and closures whose entries were planned before compact code emission.
        ReadOnlyMemory<byte> ordered = edits.Count == 0 ? code : RewriteLabelOperands(
            code,
            references,
            labels,
            edits
        );
        if (emittedLabels is null)
        {
            emittedLabels = labels;
        }
        else
        {
            for (int label = 1; label < emittedLabels.Length; label++)
            {
                emittedLabels[label] = labels[emittedLabels[label]];
            }
        }

        return (ordered, physical);

        bool IsDefined(int label) => label > 0 && label < labels.Length && labels[label] > 0;
    }

    private static Dictionary<int, (int Target, int End)> CollectFinalFunctionFailureRelays(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var relays = new Dictionary<int, (int Target, int End)>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int failure = EntryFrameFailureLabel(body, start);
            if (failure <= 0 || body.Skip(start).Take(end - start).Any(
                instruction => instruction.Definition.Opcode is CatchEnter or TryEnter or LoopReceive or Wait or WaitTimeout
            ))
            {
                continue;
            }

            for (int index = start + 1; index + 1 < end; index++)
            {
                if (body[index].Definition.Opcode != Label
                    || body[index + 1] is not { Definition.Opcode: Jump, Operands: [var target] }
                    || target.Value != failure)
                {
                    continue;
                }

                int following = EndsBeamBlock(body[index - 1].Definition.Opcode) ? index + 2 : index + 1;
                relays.Add(index, (failure, following));
            }

            CollectSharedConstructionReturns(
                code,
                body,
                start,
                end,
                relays
            );
        }

        return relays;
    }

    private static void CollectSharedConstructionReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        Dictionary<int, (int Target, int End)> redirects
    )
    {
        var owners = new Dictionary<string, int>(StringComparer.Ordinal);
        // beam_jump shares identical sequences with their last occurrence.
        // At this stage stack operands and exact projection destinations are fixed.
        for (int index = end - 2; index > start; index--)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var label] }
                || !EndsBeamBlock(body[index - 1].Definition.Opcode))
            {
                continue;
            }

            int terminal = index + 1;
            bool constructs = false;
            while (terminal < end && body[terminal].Definition.Opcode is
                TestHeap or GetTupleElement or PutTuple or PutList or Deallocate)
            {
                constructs |= body[terminal].Definition.Opcode is PutTuple or PutList;
                terminal++;
            }

            if (!constructs || terminal >= end || body[terminal].Definition.Opcode != Return)
            {
                continue;
            }

            string shape = Convert.ToHexString(code.Span[body[index + 1].Offset..body[terminal + 1].Offset]);
            if (owners.TryGetValue(shape, out int canonical))
            {
                redirects.Add(index, (canonical, terminal + 1));
            }
            else
            {
                owners.Add(shape, (int)label.Value);
            }
        }
    }
}
