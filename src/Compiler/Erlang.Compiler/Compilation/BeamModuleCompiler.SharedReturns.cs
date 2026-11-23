using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct CodeEdit(
        int Start,
        int End,
        int Tag,
        int Value,
        bool IsJump = false,
        bool IsRemoval = false
    );

    private int[]? emittedLabels;
    private int sharedReturnLabelCount;

    private int MapEmittedLabel(int label) => emittedLabels?[label] ?? label;

    private byte[]? ShareReturnBlocks(ReadOnlyMemory<byte> code)
    {
        byte[]? rewritten = null;
        int labelCount = nextLabel;
        bool shareReturns = true;
        while (true)
        {
            if (ShareReturnBlocksOnce(code, labelCount, shareReturns) is not { } pass)
            {
                if (!shareReturns)
                {
                    break;
                }

                // OTP expands short returns after sharing, then removes unused labels.
                shareReturns = false;
                continue;
            }

            rewritten = pass.Code;
            code = rewritten;
            labelCount -= pass.RemovedLabels;

            if (emittedLabels is null)
            {
                emittedLabels = pass.Labels;
            }
            else
            {
                for (int label = 1; label < emittedLabels.Length; label++)
                {
                    emittedLabels[label] = pass.Labels[emittedLabels[label]];
                }
            }
        }

        sharedReturnLabelCount = nextLabel - labelCount;

        return rewritten;
    }

    private (byte[] Code, int[] Labels, int RemovedLabels)? ShareReturnBlocksOnce(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool shareReturns
    )
    {
        var labelOperands = new List<(int Start, int End, int Label)>();
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
                (start, end, label) => labelOperands.Add((start, end, label)),
                (start, end, slot) => stackOperands.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        if (!shareReturns && ExpandShortReturnJumps(code, body) is { } expanded)
        {
            return (expanded, Enumerable.Range(0, labelCount).ToArray(), 0);
        }

        var redirects = new Dictionary<int, int>();
        var edits = new List<CodeEdit>();
        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var importsByIndex = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        int dictionaryLookupImport = !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && atoms.TryGetValue(ErlangModuleNames.Module, out int erlangAtom)
            && atoms.TryGetValue(AutoImportNames.Get, out int getAtom)
            && imports.TryGetValue((erlangAtom, getAtom, 1), out int getImport) ? getImport : -1;

        bool CanDiscardExternalTailFrame(BeamInstruction instruction) =>
            !IsTrappingTailBif(importsByIndex[(int)instruction.Operands[1].Value], atomNames);

        bool IsColdMapErrorTail(BeamInstruction instruction)
        {
            var import = importsByIndex[(int)instruction.Operands[1].Value];

            return import.Arity == 1 && instruction.Operands[0].Value.IsOne
                && atomNames[import.Module - 1] == ErlangModuleNames.Module
                && atomNames[import.Function - 1] == ErlangModuleNames.Error;
        }

        bool IsNonCollectingBodyProjection(BeamInstruction instruction)
        {
            if (ModuleOptimizationDisabled || instruction.Operands.Count == 0
                || instruction.Operands[0] is not { Kind: BeamOperandParameters.Failure, Value.IsZero: true })
            {
                return false;
            }

            string? function = instruction switch
            {
                { Definition.Opcode: BifOne, Operands.Count: 4 } => ErlangModuleNames.Head,
                {
                    Definition.Opcode: BifTwo,
                    Operands: [_, _, { Kind: BeamOperandParameters.Integer, Value.Sign: > 0 }, _, _]
                } => ErlangModuleNames.Element,
                _ => null
            };
            if (function is null)
            {
                return false;
            }

            var import = importsByIndex[(int)instruction.Operands[1].Value];

            return import.Arity == instruction.Operands.Count - 3
                && atomNames[import.Module - 1] == ErlangModuleNames.Module
                && atomNames[import.Function - 1] == function;
        }

        bool IsPublicEntry(BeamInstruction instruction) => exportedFunctions.Contains((
            atomNames[(int)instruction.Operands[1].Value - 1],
            (int)instruction.Operands[2].Value
        ));

        var exits = importsByIndex.Select((entry, index) => (entry, index))
            .Where(item => atomNames[item.entry.Module - 1] == ErlangModuleNames.Module
                && atomNames[item.entry.Function - 1] is ErlangModuleNames.Error or ErlangModuleNames.Exit
                    or ErlangModuleNames.Throw or ErlangRaiseBifNames.Raise)
            .Select(item => item.index).ToHashSet();
        int functionEnd = body.Count;
        for (int index = body.Count - 1; index >= 0; index--)
        {
            if (body[index].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            if (shareReturns)
            {
                CollectSharedReturns(
                    code,
                    body,
                    index + 1,
                    functionEnd,
                    exits,
                    redirects,
                    edits
                );
            }

            CollectForwardingLabels(
                body,
                index + 1,
                functionEnd,
                redirects,
                edits
            );
            functionEnd = index;
        }

        foreach (int label in redirects.Keys.ToArray())
        {
            int target = redirects[label];
            while (redirects.TryGetValue(target, out int nextTarget))
            {
                target = nextTarget;
            }

            redirects[label] = target;
        }

        CollectRedundantJumps(body, redirects, edits);
        if (redirects.Count == 0 && edits.Count == 0)
        {
            CollectUnreachableBlocks(
                body,
                labelOperands,
                redirects,
                edits
            );
        }

        if (redirects.Count == 0 && edits.Count == 0)
        {
            CollectUnusedFallthroughLabels(
                body,
                labelOperands,
                redirects,
                edits
            );
            CollectIdentityMoves(body, edits);
        }

        if (redirects.Count == 0 && edits.Count == 0)
        {
            var closureEntries = shareReturns || closures.Count == 0 ? null : closures
                .Where(closure => closure.Body is not null || closure.General is not null)
                .Select(closure => MapClosureEntryLabel(closure.Entry))
                .ToHashSet();
            byte[]? simplified = SimplifyValueSelections(code, body, labelOperands)
                ?? (shareReturns ? null : OrderSharedEntryAtomClauses(
                    code,
                    body,
                    labelOperands,
                    atomNames
                ))
                ?? (SsaTypeInferenceEnabled ? CombineAtomMembershipTests(
                    code,
                    body,
                    labelOperands,
                    atomNames
                ) : null)
                ?? InvertBranchTests(code, body, labelOperands)
                ?? NormalizeIntegerComparisons(code, body)
                ?? NormalizeNilEqualityTests(code, body)
                ?? NormalizeNonnumericComparisonTests(code, body)
                ?? AnnotateBinaryExtractRanges(code, body, atomNames)
                ?? NormalizeTaggedTupleBranches(code, body)
                ?? (shareReturns ? null : ShareCheckedEntryRecordTests(code, body, labelOperands))
                ?? AnnotateKeyFindTupleTests(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? RemoveTerminalTupleAlias(code, body)
                ?? PlaceCheckedFieldCallFrame(code, body)
                ?? FoldListHeadSwap(code, body)
                ?? ReuseReconstructedCons(code, body)
                ?? CombineConstructionHeapChecks(code, body)
                ?? CombineStackAndHeapAllocation(code, body, dictionaryLookupImport)
                ?? KeepTerminalCallResultsInX0(code, body, stackOperands)
                ?? KeepAdjacentResultsInX0(code, body, stackOperands)
                ?? DelayCallResultStoreThroughConstruction(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? ForwardCallResult(code, body)
                ?? SeparateBinaryArgumentReturnFrame(code, body)
                ?? EliminateEntryArgumentCopies(
                    code,
                    body,
                    stackOperands,
                    removeSpills: true
                )
                ?? RemoveProtectedEntryArgumentReloads(code, body)
                ?? UseSavedBranchOperands(code, body)
                ?? ForwardSavedCallResults(code, body, stackOperands)
                ?? PlaceEntryBifBeforeAllocation(
                    code,
                    body,
                    stackOperands,
                    instruction => IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex)
                )
                ?? KeepCallResultForArithmeticFanout(
                    code,
                    body,
                    stackOperands,
                    atomNames,
                    importsByIndex
                )
                ?? DelayCallResultStoreThroughArithmetic(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? KeepValidatedCallValueInRegisters(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? StoreBifResultsDirectly(code, body)
                ?? (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
                    ? StoreArithmeticResultAcrossZeroFrame(
                        code,
                        body,
                        instruction => IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex)
                    )
                    : null)
                ?? ConstructSendTupleInArgument(code, body)
                ?? StoreConstructionResultsDirectly(code, body)
                ?? ReuseReturningTupleRegister(code, body)
                ?? SinkTupleReadsAcrossCalls(code, body, stackOperands)
                ?? RemoveRepeatedFailureProjections(
                    code,
                    body,
                    labelOperands,
                    shareCheckedFallbacks: !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
                )
                ?? ForwardStackProjections(code, body)
                ?? ForwardInteriorCallArgumentCopies(code, body)
                // Remove unread leaf slots before their projections become X writes.
                ?? RemoveDeadStackProjections(code, body, requiresOrdinaryCall: false)
                ?? RemoveDeadCheckedEntryListHeads(code, body)
                ?? ForwardDuplicatedTrailingFrameReads(code, body, stackOperands)
                ?? RemoveUnusedTrailingFrameSlots(code, body)
                ?? ReallocateLeafFrameRegisters(
                    code,
                    body,
                    stackOperands,
                    canDiscardExternalTailFrame: CanDiscardExternalTailFrame
                )
                ?? DelayArityTagFrames(code, body)
                ?? DelayEntryStackFrame(
                    code,
                    body,
                    stackOperands,
                    CanDiscardExternalTailFrame,
                    IsPublicEntry
                )
                ?? SplitCheckedConversionFrames(code, body)
                ?? SinkTupleReadsPastGuards(code, body)
                ?? SimplifyFrameInitialization(code, body)
                ?? FoldTerminalCalls(code, body)
                ?? ScheduleCallResultStores(code, body)
                ?? ExchangeCallResultWithSavedInput(code, body)
                ?? ExchangeCallResultWithDeadArgument(code, body, stackOperands)
                ?? ReuseStackSlots(code, body, stackOperands)
                ?? OrderIndependentStackStores(code, body)
                ?? LoadCallArgumentsBeforeReusedResultStore(code, body)
                ?? ComposeTailArgumentCopies(code, body)
                ?? OrderIndependentCallArguments(code, body)
                ?? ReallocateSingletonRecordTail(code, body)
                ?? RetainPositionedBinaryTail(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? TrimAllocationLiveRegisters(code, body)
                ?? NormalizeTailCallFrames(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? KeepMatchedCallProjectionInX(code, body)
                ?? MoveHeapTestsBeforeNeutralInstructions(
                    code,
                    body,
                    atomNames,
                    importsByIndex
                )
                ?? RemoveProvenLocalRecordChecks(code, body, atomNames)
                ?? (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
                    ? null : RemoveRepeatedSavedRecordChecks(code, body))
                ?? (shareReturns ? null : EliminateEntryArgumentCopies(
                    code,
                    body,
                    stackOperands,
                    removeSpills: true,
                    allowFramelessBranches: true
                ))
                ?? (shareReturns ? null : ReuseStackSlots(
                    code,
                    body,
                    stackOperands,
                    useValueLiveness: true,
                    closureEntries: closureEntries
                ))
                // Reloads also identify the unframed returns when short return jumps are expanded.
                ?? (shareReturns ? null : EliminateEntryArgumentCopies(
                    code,
                    body,
                    stackOperands,
                    removeSpills: false
                ))
                ?? (shareReturns ? null : MoveTerminalErrorBlocks(code, body, labelCount))
                ?? (shareReturns ? null : ForwardDuplicatedOwnedFrameReads(code, body, stackOperands))
                ?? (shareReturns ? null : CompactDisjointFrames(code, body, stackOperands))
                ?? (shareReturns ? null : ReallocateLeafFrameRegisters(
                    code,
                    body,
                    stackOperands,
                    afterCanonicalization: true,
                    isArithmetic: instruction => IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex),
                    canDiscardExternalTailFrame: CanDiscardExternalTailFrame,
                    isBodyProjection: IsNonCollectingBodyProjection,
                    isColdMapErrorTail: !ModuleOptimizationDisabled && SsaTypeInferenceEnabled ? IsColdMapErrorTail : null
                ))
                // Moving a matched list prefix into X must not hide the original frame from leaf allocation.
                ?? (shareReturns ? null : DelayEntryStackFrame(
                    code,
                    body,
                    stackOperands,
                    CanDiscardExternalTailFrame,
                    IsPublicEntry,
                    closedListMatchesOnly: true
                ))
                ?? (shareReturns ? null : PromotePostCallProjectionSlots(
                    code,
                    body,
                    stackOperands
                ))
                ?? (shareReturns ? null : PromoteTransientFrameSlots(code, body, stackOperands))
                ?? (shareReturns ? null : RemoveSelectFallbackFrame(
                    code,
                    body,
                    labelOperands,
                    stackOperands
                ))
                // Preserve the specialized select fallback schedule before reallocating other owned frames.
                ?? (shareReturns ? null : ReallocateLeafFrameRegisters(
                    code,
                    body,
                    stackOperands,
                    afterCanonicalization: true,
                    isArithmetic: instruction => IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex),
                    canDiscardExternalTailFrame: CanDiscardExternalTailFrame,
                    ownedBranchesOnly: true
                ))
                // Keep saved map inputs visible to the frame and alias passes first.
                ?? KeepAdjacentResultsInX0(
                    code,
                    body,
                    stackOperands,
                    mapOnly: true
                )
                // Call-aware frame rewrites consume complete destructuring shapes.
                // Once they settle, remove unread projections and revisit frame compaction.
                ?? (shareReturns ? null : RemoveDeadStackProjections(code, body, requiresOrdinaryCall: true))
                ?? (shareReturns ? null : RemoveOverwrittenTupleProjections(code, body))
                ?? (shareReturns ? null : StoreRecordUpdateInCallArgument(code, body))
                ?? (shareReturns ? null : LocalizeCheckedRecordCallFrame(
                    code,
                    body,
                    instruction => IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex),
                    atomNames,
                    importsByIndex
                ))
                // Earlier fixed-point passes may still need the original failure reload.
                ?? (shareReturns ? null : ReuseProducerBinaryMatchFailure(code, body, atomNames));

            if (simplified is not null)
            {
                return (simplified, Enumerable.Range(0, labelCount).ToArray(), 0);
            }

            if (ShareFallthroughTupleReturns(code, body) is { } sharedFallthrough)
            {
                return (sharedFallthrough, Enumerable.Range(0, labelCount).ToArray(), 0);
            }

            if (!shareReturns && !ModuleOptimizationDisabled && SsaTypeInferenceEnabled
                && OrderEntryListKinds(code, body, labelOperands) is { } listLayout)
            {
                return (listLayout, Enumerable.Range(0, labelCount).ToArray(), 0);
            }

            return shareReturns ? null : OrderNestedEntryLabels(
                code,
                body,
                labelOperands,
                atomNames,
                labelCount
            );
        }

        var remappedLabels = new int[labelCount];
        int next = 1;
        for (int label = 1; label < labelCount; label++)
        {
            if (!redirects.ContainsKey(label))
            {
                remappedLabels[label] = next++;
            }
        }
        foreach (var (label, target) in redirects)
        {
            remappedLabels[label] = remappedLabels[target];
        }

        var removed = edits.OrderBy(edit => edit.Start).ToArray();
        int removedIndex = 0;
        foreach (var (start, end, label) in labelOperands)
        {
            while (removedIndex < removed.Length && removed[removedIndex].End <= start)
            {
                removedIndex++;
            }

            if (removedIndex < removed.Length && removed[removedIndex].Start <= start)
            {
                continue;
            }

            int target = remappedLabels[label];
            if (target != label)
            {
                edits.Add(new(
                    start,
                    end,
                    LabelTag,
                    target
                ));
            }
        }
        for (int index = 0; index < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var operand] })
            {
                continue;
            }

            int label = (int)operand.Value;
            int mapped = remappedLabels[label];
            if (mapped != label && !redirects.ContainsKey(label))
            {
                edits.Add(new(
                    body[index].Offset + 1,
                    body[index + 1].Offset,
                    BeamWire.UnsignedTag,
                    mapped
                ));
            }
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var edit in edits.OrderBy(edit => edit.Start))
        {
            output.Write(code.Span[copied..edit.Start]);
            if (edit.IsJump)
            {
                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, remappedLabels[edit.Value]);
            }
            else if (edit.Value != 0)
            {
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            }
            copied = edit.End;
        }
        output.Write(code.Span[copied..]);

        return (output.ToArray(), remappedLabels, redirects.Count);
    }

    private static byte[]? InvertBranchTests(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int labelIndex = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction test = body[index];
            byte inverse = test.Definition.Opcode switch
            {
                IsLess => IsGreaterOrEqual,
                IsGreaterOrEqual => IsLess,
                NumericEqualTest => NumericNotEqualTest,
                NumericNotEqualTest => NumericEqualTest,
                ExactEqualTest => ExactNotEqualTest,
                ExactNotEqualTest => ExactEqualTest,
                _ => 0
            };
            if (inverse == 0 || test.Operands[0] is not { Kind: BeamOperandParameters.Label } failure
                || body[index + 1] is not { Definition.Opcode: Jump, Operands: [var target] }
                || target.Kind != BeamOperandParameters.Label)
            {
                continue;
            }

            if (test.Definition.Opcode == ExactEqualTest && target.Value == failure.Value)
            {
                output.Write(code.Span[copied..test.Offset]);
                copied = body[index + 1].Offset;
                continue;
            }

            int successor = index + 2;
            while (body[successor] is { Definition.Opcode: Label, Operands: [var label] }
                && label.Value != failure.Value)
            {
                successor++;
            }

            if (body[successor].Definition.Opcode != Label)
            {
                continue;
            }

            int labelOffset = test.Offset + 1;
            while (labelIndex < labelOperands.Count && labelOperands[labelIndex].Start < labelOffset)
            {
                labelIndex++;
            }

            if (labelIndex == labelOperands.Count || labelOperands[labelIndex].Start != labelOffset)
            {
                continue;
            }

            output.Write(code.Span[copied..test.Offset]);
            output.WriteByte(inverse);
            BeamCompactWriter.Write(output, LabelTag, target.Value);
            output.Write(code.Span[labelOperands[labelIndex].End..body[index + 1].Offset]);
            copied = body[index + 2].Offset;
            index++;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static void CollectForwardingLabels(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var labels = new HashSet<int>();
        for (int index = start + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is CatchEnter or TryEnter or LoopReceive or Wait or WaitTimeout)
            {
                return;
            }

            if (instruction.Definition.Opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value);
            }
        }

        var forwarding = new Dictionary<int, int>();
        for (int index = start + 1; index < end; index++)
        {
            if (body[index] is not { Definition.Opcode: Jump, Operands: [var operand] }
                || !labels.Contains((int)operand.Value))
            {
                continue;
            }

            int first = index;
            while (first > start + 1 && body[first - 1].Definition.Opcode == Label)
            {
                first--;
            }

            for (int cursor = first; cursor < index; cursor++)
            {
                forwarding.Add((int)body[cursor].Operands[0].Value, (int)operand.Value);
            }
        }

        for (int index = start + 1; index + 1 < end; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var operand] }
                || !forwarding.ContainsKey((int)operand.Value))
            {
                continue;
            }

            int jump = index + 1;
            while (body[jump].Definition.Opcode == Label)
            {
                jump++;
            }

            int target = (int)operand.Value;
            var visited = new HashSet<int>();
            while (visited.Add(target) && forwarding.TryGetValue(target, out int next))
            {
                target = next;
            }

            if (!forwarding.ContainsKey(target))
            {
                for (int cursor = index; cursor < jump; cursor++)
                {
                    redirects.Add((int)body[cursor].Operands[0].Value, target);
                }

                bool fallsThrough = !EndsBeamBlock(body[index - 1].Definition.Opcode);
                edits.Add(new(
                    body[index].Offset,
                    body[jump + 1].Offset,
                    LabelTag,
                    fallsThrough ? target : 0,
                    fallsThrough
                ));
            }

            index = jump;
        }
    }

    private static void CollectRedundantJumps(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyDictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var replacement = edits.ToDictionary(edit => edit.Start);
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (replacement.TryGetValue(body[index].Offset, out var previous) && !previous.IsJump)
            {
                while (body[index + 1].Offset < previous.End)
                {
                    index++;
                }

                continue;
            }

            int target;
            int end;
            if (previous.IsJump)
            {
                target = previous.Value;
                end = previous.End;
            }
            else if (body[index] is { Definition.Opcode: Jump, Operands: [var label] })
            {
                target = (int)label.Value;
                end = body[index + 1].Offset;
            }
            else
            {
                continue;
            }

            target = redirects.GetValueOrDefault(target, target);
            int next = index + 1;
            while (body[next].Offset < end)
            {
                next++;
            }

            int lastRemovedInstruction = next - 1;
            while (body[next] is { Definition.Opcode: Label, Operands: [var successor] }
                && !redirects.ContainsKey((int)successor.Value))
            {
                if (successor.Value == target)
                {
                    if (previous.IsJump)
                    {
                        edits.Remove(previous);
                    }

                    edits.Add(new(
                        body[index].Offset,
                        end,
                        LabelTag,
                        0
                    ));
                    break;
                }

                next++;
            }

            index = lastRemovedInstruction;
        }
    }

    private void CollectSharedReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        IReadOnlySet<int> exits,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var unprotected = UnprotectedReturnLabels(body, start, end);
        var returns = new Dictionary<string, int>(StringComparer.Ordinal);
        // OTP beam_jump:share_1 chooses the last occurrence of a sequence.
        for (int index = end - 2; index > start; index--)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var entry] })
            {
                continue;
            }

            int label = (int)entry.Value;
            if (unprotected is not null && !unprotected.Contains(label))
            {
                continue;
            }

            int terminal = index + 1;
            int removedLines = 0;
            while (terminal < end - 1 && body[terminal].Definition.Opcode is
                Move or Swap or Line or TestHeap or GetTupleElement or PutTuple or PutList or Deallocate)
            {
                if (body[terminal].Definition.Opcode == Line)
                {
                    removedLines++;
                }

                terminal++;
            }

            if (terminal >= end - 1 || body[terminal].Definition.Opcode is not
                (Return or TailLocalCall or TailLocalCallWithFrame or TailExternalCall or TailExternalCallWithFrame))
            {
                continue;
            }

            // Keep the legacy return shapes in functions with exception handlers.
            if (unprotected is not null && (removedLines != 0 || terminal - index > 3
                || body[terminal].Definition.Opcode is TailExternalCall or TailExternalCallWithFrame
                || Enumerable.Range(index + 1, terminal - index - 1).Any(offset =>
                    body[offset].Definition.Opcode != Deallocate && body[offset] is not
                    { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }] })))
            {
                continue;
            }

            // Exit calls can be lowered after sharing in OTP. Their final bytecode
            // can hide distinct frame state at the original error sites.
            if (body[terminal].Definition.Opcode is TailExternalCall or TailExternalCallWithFrame
                && exits.Contains((int)body[terminal].Operands[1].Value))
            {
                continue;
            }

            int endOffset = body[terminal + 1].Offset;
            string key = Convert.ToHexString(code.Span[body[index + 1].Offset..endOffset]);
            if (!returns.TryGetValue(key, out int target))
            {
                returns.Add(key, label);
                continue;
            }

            redirects.Add(label, target);
            // Retain an edge for a predecessor that entered the block by fallthrough.
            bool fallsThrough = !EndsBeamBlock(body[index - 1].Definition.Opcode);
            edits.Add(new(
                body[index].Offset,
                endOffset,
                LabelTag,
                fallsThrough ? target : 0,
                fallsThrough
            ));
            lineInstructions -= removedLines;
        }
    }

    private static bool EndsBeamBlock(int opcode) =>
        opcode is Return or Jump or SelectValue or SelectTupleArity or TailLocalCall or TailLocalCallWithFrame
            or TailExternalCall or TailExternalCallWithFrame or TailDynamicApply or CaseEnd or BadMatch
            or BadRecord or IfEnd or TryCaseEnd or Raise or LoopReceiveEnd or Wait;
}
