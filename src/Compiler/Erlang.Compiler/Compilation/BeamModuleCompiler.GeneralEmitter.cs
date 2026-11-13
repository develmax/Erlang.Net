using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private Dictionary<(string Name, int Arity), (int Slots, int Labels, int ListFailures, IReadOnlyDictionary<int, int>? TupleFrames)> PlanGeneralFunctions(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyList<BeamSourceLocation> sourceLines,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        var plans = new Dictionary<(string Name, int Arity), (int Slots, int Labels, int ListFailures, IReadOnlyDictionary<int, int>? TupleFrames)>();
        var placeholderEntries = localFunctions.Keys.ToDictionary(key => key, _ => 2);
        placeholderEntries.Add(("module_info", 0), 2);
        placeholderEntries.Add(("module_info", 1), 2);
        foreach (var function in functions)
        {
            if (ReadCapturedTableList(function, localFunctions) is not null)
            {
                placeholderEntries.TryAdd((MappingName(function.Name, 0), 2), 2);
            }
            if (ReadPageComprehension(function, localFunctions) is not null)
            {
                placeholderEntries.TryAdd((MappingName(function.Name, function.Arity), 3), 2);
            }
        }
        for (int index = 0; index < functions.Count; index++)
        {
            var function = functions[index];
            if (function.IsInlineFailure)
            {
                continue;
            }

            if (tupleMappingCases.ContainsKey((function.Name, function.Arity)) || capturedListFilters.ContainsKey((function.Name, function.Arity)))
            {
                continue;
            }

            if (ReadDiscardedListCalls(function, localFunctions) is not null || ReadTableProjection(function.Clauses) is not null
                || ReadSplitListJoin(function.Clauses) is not null || ReadColumnMapping(function.Clauses) is not null
                || ReadAllocatorRows(function.Clauses) is not null)
            {
                continue;
            }

            bool nestedCall = function.Clauses is [{ Body: Expr.Call call }]
                && call.Arguments.Any(argument => argument is Expr.Call
                    or Expr.Tuple { Items: [Expr.Literal { Value: Atom }, Expr.Call] }
                    or Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Call });
            bool computedScalar = function.Clauses is [
            {
                Body: Expr.Unary { Operator: ErlangOperators.Not, Operand: Expr.Call }
                or Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Literal, Right: Expr.Call }
            }];
            bool optionDispatch = function.Clauses is [
                {
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: not null, Arguments: [Expr.Literal, Expr.Variable, Expr.Literal]
                        }, Clauses: [{ Patterns: [Pattern.Literal { Value: Atom }] }, { Patterns: [Pattern.Literal { Value: Atom }] }]
                    }
                }];
            bool detailTab = function.Clauses is [
                {
                    Body: Expr.Sequence
                    {
                        Items: [
                Expr.Match { Value: Expr.Call { Module: null } },
                Expr.Match { Pattern: Pattern.Literal, Value: Expr.Call { Module: not null } }, Expr.Variable]
                    }
                }]
                || function.Clauses is [
                    {
                        Body: Expr.Sequence
                        {
                            Items: [
                    Expr.Match, Expr.Call, Expr.Call, Expr.Match,
                    Expr.Match { Value: Expr.Call { Function: AutoImportNames.Apply } }, Expr.Call, Expr.Call, Expr.Variable]
                        }
                    }];
            bool pageRecord = function.Clauses is [
            {
                Body: Expr.Sequence
                {
                    Items: [
                Expr.Call, Expr.Call, Expr.Match, Expr.Call, Expr.Call, Expr.Tuple { Items: [Expr.Variable, Expr.RecordConstruction] }]
                }
            }];
            bool mapDefaultCase = function.Clauses is [
                {
                    Body: Expr.Sequence
                    {
                        Items: [
                Expr.Match { Value: Expr.Call { Module: "maps", Function: "get" } }, Expr.Case]
                    }
                }];
            bool detailPage = function.Clauses is [
                {
                    Body: Expr.Sequence
                    {
                        Items: [
                Expr.Match, Expr.Call { Module: not null, Arguments: [Expr.Variable, Expr.Literal, Expr.Variable] }
            ]
                    }
                }] || function.Clauses is [
                {
                    Body: Expr.Sequence
                    {
                        Items: [
                Expr.Match, Expr.Match, Expr.Match { Value: Expr.Call { Arguments: [_, _, _, Expr.Call] } }, Expr.Call
            ]
                    }
                }];
            bool interleavedCalls = function.Clauses is [
                {
                    Patterns: [], Body: Expr.Sequence
                    { Items: [Expr.Call, Expr.Match, .., Expr.Call, Expr.List or Expr.Binary { Operator: ErlangOperators.Append }] }
                }];
            bool matchCase = function.Clauses is [{ Body: Expr.Sequence { Items: [Expr.Match, Expr.Case] } } clause]
                && ReadSignedWidth(clause) is null
                || function.Clauses is [
                    {
                        Body: Expr.Sequence
                        {
                            Items: [
                    Expr.Match { Value: Expr.Call { Module: null } },
                    Expr.Match { Value: Expr.Call { Module: not null } },
                    Expr.Match { Value: Expr.Call { Module: not null } },
                    Expr.Case
                ]
                        }
                    }];
            bool nestedCase = function.Clauses is [{ Body: Expr.Case decision }]
                && decision.Clauses.Any(branch => branch.Body is Expr.Case or Expr.Maybe
                    or Expr.Sequence { Items: [Expr.Match { Value: Expr.Case }, Expr.Case] });
            bool configuredTable = function.Clauses is [
            {
                Body: Expr.Case
                {
                    Clauses: [
                    { Body: Expr.Sequence { Items: [Expr.Match, Expr.Match, Expr.Match, Expr.Match, Expr.Call] } },
                    { Body: Expr.Call }, { Body: Expr.Call }
            ]
                }
            }];
            bool taggedParser = function.Clauses is [
                {
                    Body: Expr.Case
                    {
                        Clauses: [
                        { Patterns: [Pattern.Tuple], Body: Expr.Call { Arguments: [Expr.Tuple { Items: [Expr.Literal, Expr.Tuple] }] } },
                        { Patterns: [Pattern.Tuple], Body: Expr.Call { Module: null } }, ..
            ]
                    }
                }];
            bool recordDetails = function.Clauses is [
                {
                    Body: Expr.Case
                    {
                        Clauses: [
                        {
                            Body: Expr.Sequence
                            {
                                Items: [Expr.Match, Expr.Match
                                {
                                    Value: Expr.Call
                                    { Arguments: [_, Expr.List { Items: [Expr.RecordAccess, _] }] }
                                }, Expr.Tuple]
                            }
                        }, _
            ]
                    }
                }];
            bool collectionBody = function.Clauses is [{ Body: Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Tuple }, Expr.Call] } } tupleCall]
                && ReadLocalBinding(tupleCall) is null
                || function.Clauses is [{ Body: Expr.Sequence { Items: [Expr.Match, Expr.ListComprehension] } }]
                || function.Clauses is [{ Body: Expr.Sequence { Items: [Expr.Match, Expr.Call { Function: var helper }] } }]
                    && helper == MappingName(function.Name, function.Arity);
            bool sequenceFields = function.Clauses is [{ Body: Expr.Sequence sequence } single]
                && sequence.Items.OfType<Expr.Match>().Any(match => match.Pattern is Pattern.Tuple or Pattern.List)
                && ReadLocalBinding(single) is null;
            bool signedByteMatch = function.Clauses is [{ Body: Expr.Sequence signedSequence }]
                && signedSequence.Items.OfType<Expr.Match>().Any(match => match.Pattern is BitPattern
                {
                    Segments: [{ Specification: { Type: BitSegmentTypes.Integer, Signed: true, Unit: BitSyntaxDefaults.BinaryUnit } }]
                });
            bool protectedExpression = function.Clauses is [{ Body: Expr.Try } protectedClause]
                && ReadProtectedCall(protectedClause) is null;
            bool computedRecord = function.Clauses is [{ Body: Expr.RecordConstruction record }]
                && RequiresRecordFrame(RecordExpressions.Create(record));
            bool generalClosure = function.Clauses is [{ Guard: null, Body: Expr.Fun { SelfName: null } anonymous } parent]
                && HasUnconditionalParameters(parent)
                && ReadReturnedClosure(
                    function,
                    parent,
                    anonymous,
                    sourceLines[index],
                    0,
                    0,
                    null
                ) is null;
            bool computedAppend = function.Clauses is [{ Body: Expr.Binary { Operator: ErlangOperators.Append } append }]
                && !IsDirectAppend(append);
            bool catchExpression = function.Clauses is [{ Body: Expr.Catch }]
                || function.Clauses is [{ Body: Expr.Tuple tuple }] && tuple.Items.Any(item => item is Expr.Catch);
            bool recordPredicate = function.Clauses is [{ Body: var recordBody }] && ReadRecordTest(recordBody) is not null;
            bool numericComparison = function.Clauses is [{ Body: Expr.Binary binary }] && IsNumericEquality(binary);
            bool subtraction = function.Clauses is [
                {
                    Body: Expr.Binary { Operator: ErlangOperators.Minus }
                or Expr.Unary { Operator: ErlangOperators.Minus, Operand: not Expr.Literal }
                }];
            bool functionReference = function.Clauses is [{ Body: Expr.Call or Expr.Tuple or Expr.List } referenceClause]
                && LocalFunctionEntries.Walk(referenceClause.Body).Any(expression => expression is Expr.FunctionReference);
            bool conditional = function.Clauses is [{ Body: Expr.If or Expr.Block }]
                || function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body).Any(expression => expression is Expr.Maybe))
                || function.Clauses is [{ Body: Expr.Sequence conditionalSequence }]
                    && conditionalSequence.Items.Any(item => item is Expr.If or Expr.Match { Value: Expr.If });
            bool bitExpression = function.Clauses.Any(clause =>
                ReadSignedWriter(clause) is null && ContainsComputedBits(clause.Body));
            bool mapExpression = function.Clauses.Any(clause => ContainsComputedMap(clause.Body));
            bool additionalOperations = ReadBerTag(function.Clauses) is null && ReadBerContinuation(function.Clauses) is null
                && function.Clauses.Any(clause =>
                ReadSignedWidth(clause) is null && ContainsAdditionalOperation(clause.Body, includeElement: false)
                || clause.Guard is { } guard && ContainsAdditionalOperation(guard, includeElement: true));
            bool listOrFunctionGuard = function.Clauses.Any(clause =>
                clause.Guard is { } guard && LocalFunctionEntries.Walk(guard).Any(IsListOrFunctionGuardCall));
            bool simpleGuardCall = function.Clauses.Any(clause =>
                clause.Guard is { } guard && LocalFunctionEntries.Walk(guard).Any(IsSimpleGuardCall));
            bool guardedRecordAccess = function.Clauses.Any(clause =>
                clause.Guard is { } guard && LocalFunctionEntries.Walk(guard).Any(expression => expression is Expr.RecordAccess));
            bool functionValueCall = function.Clauses.Any(clause =>
                HasFunctionValueCall(clause.Body));
            bool selectiveReceive = function.Clauses.Any(clause =>
                LocalFunctionEntries.Walk(clause.Body).Any(expression => expression is Expr.Receive));
            bool dictionaryAssertion = function.Clauses is [
                {
                    Patterns: [Pattern.Variable key],
                    Body: Expr.Sequence { Items: [Expr.Match { Value: var lookup }, _, _] }
                }] && IsDictionaryLookup(lookup, key, placeholderEntries);
            bool guardedOption = function.Clauses is [{ Guard: null, Patterns: [], Body: var optionBody }]
                && GeneralEmitter.ReadGuardedOption(optionBody, placeholderEntries) is { Success: Expr.Variable returned } option
                && returned.Name == option.Value.Name && AttributeLiteral.TryNormalizeConstant(option.Fallback, out _);
            bool fallbackPlan = false;
            if (HasSupportedClauses(function) && !IsBooleanPatternPredicate(function.Clauses) && !nestedCall && !matchCase && !collectionBody && !sequenceFields
                && !protectedExpression && !conditional && !signedByteMatch && !nestedCase && !configuredTable && !taggedParser && !computedScalar && !computedRecord
                && !optionDispatch && !generalClosure && !computedAppend && !catchExpression && !recordPredicate
                && !numericComparison && !subtraction && !functionReference && !selectiveReceive && !functionValueCall && !guardedRecordAccess && !simpleGuardCall && !listOrFunctionGuard && !additionalOperations && !mapExpression && !bitExpression
                && !dictionaryAssertion && !GeneralEmitter.HasNegatedPredicateTail(function, localFunctions)
                && !guardedOption && !recordDetails && !detailPage && !detailTab && !pageRecord && !mapDefaultCase && !interleavedCalls && ReadClosureReplyCase(function) is null
                && GeneralEmitter.ReadForwardedCallResult(function, placeholderEntries) is null
                && !HasClosedAtomSelector(function, localFunctions) && ReadTupleKeyFilter(function) is null
                && ReadTupleProjectionHelper(function) is null && ReadTupleFieldComprehensionHelper(function) is null
                && !function.IsClosedListMapper && ReadTaggedAccumulator(function) is null)
            {
                if (!NeedsGeneralBody(function, sourceLines[index], placeholderEntries))
                {
                    continue;
                }

                fallbackPlan = true;
            }
            var planner = new BeamModuleCompiler(name, sourceModule: sourceModule) { exportedFunctions = exportedFunctions };
            foreach (var (entry, filter) in capturedListFilters)
            {
                planner.capturedListFilters.Add(entry, filter);
            }

            using var scratch = planner.instructions;
            var location = sourceLines[index];
            planner.StartFunction(function.Name, function.Arity, location);
            var emitter = new GeneralEmitter(
                planner,
                placeholderEntries,
                localFunctions,
                location,
                0
            );
            try
            {
                emitter.Function(function);
            }
            catch (NotSupportedException) when (fallbackPlan)
            {
                continue;
            }
            catch (NotSupportedException error)
            {
                throw new NotSupportedException($"{name}:{function.Name}/{function.Arity}: {error.Message}", error);
            }
            plans.Add((function.Name, function.Arity), (emitter.SlotCount, planner.nextLabel - 1, emitter.ListFailureCount, emitter.TupleFrames));
        }

        return plans;
    }

    private bool NeedsGeneralBody(
        FunctionDefinition function,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (function.Clauses is not [{ Guard: null } clause]
            || !(HasUnconditionalParameters(clause) || HasNilParameter(clause)))
        {
            return false;
        }

        var probe = new BeamModuleCompiler(name, sourceModule: sourceModule) { exportedFunctions = exportedFunctions };
        using var scratch = probe.instructions;
        probe.numericWidthFunctions.UnionWith(numericWidthFunctions);
        probe.StartFunction(function.Name, function.Arity, location);
        try
        {
            if (!probe.TryEmitReturnedClosure(function, location))
            {
                probe.EmitFunctionBody(function.Clauses, location, entries);
            }

            return false;
        }
        catch (NotSupportedException)
        {
            return true;
        }
    }

    private static bool RequiresRecordFrame(Expr.Tuple tuple) =>
        !AttributeLiteral.TryNormalizeConstant(tuple, out _)
        && tuple.Items.Any(member => member is not (Expr.Variable or Expr.Literal { Value: Atom or Nil })
            && !(member is Expr.Literal { Value: Integer integer }
                && integer.Value >= int.MinValue && integer.Value <= int.MaxValue));

    private sealed partial class GeneralEmitter(
        BeamModuleCompiler owner,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        BeamSourceLocation location,
        int frameSize,
        int plannedLabels = 0,
        int plannedListFailures = 0,
        FunctionDefinition? lexicalOwner = null,
        LiftedClosure? recursiveClosure = null,
        int recursiveClosureIndex = 0,
        Dictionary<string, int>? guardFailureOperands = null,
        IReadOnlyDictionary<int, int>? plannedTupleFrames = null
    )
    {
        private Dictionary<string, int> variables = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (int Slot, int Index)> deferredPairMembers = new(StringComparer.Ordinal);
        private readonly List<(int Label, TaggedBranchRange Range, BeamSourceLocation Location)> decoderRangeFailures = [];
        private int nextSlot;
        private int peakSlot;
        private int decoderSlotCount;
        private int? decoderListLabel;
        private int? decoderRangeLabel;
        private FunctionDefinition? parentFunction = lexicalOwner;
        private int anonymousOrdinal;
        internal int SlotCount => peakSlot;
        internal int ListFailureCount { get; private set; }

        private int Slot()
        {
            int slot = nextSlot++;
            peakSlot = Math.Max(peakSlot, nextSlot);

            int register = DecoderSlot(slot);
            if (owner.receiveScratchRegisters is { } scratch)
            {
                scratch.TryAdd(register, ReceiveGuardLiveRegisters);
            }

            return register;
        }

        // Reversing the reserved prefix is its own inverse.
        private int DecoderSlot(int slot) => slot < decoderSlotCount ? decoderSlotCount - 1 - slot : slot;

        private int Save()
        {
            int slot = Slot();
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slot
            );

            return slot;
        }

        private void Load(int slot, int register = 0) => owner.MoveRegister(
            YRegisterTag,
            slot,
            XRegisterTag,
            register
        );

        private int Label() => owner.nextLabel++;

        private void Mark(int label) => owner.Op(BeamEmissionFormat.Label, BeamWire.UnsignedTag, label);

        private void JumpTo(int label) => owner.Op(Jump, LabelTag, label);

        internal void Function(FunctionDefinition function)
        {
            parentFunction ??= function;
            if (recursiveClosure is { SelfName: not null } or { GuardFailure: not null })
            {
                EmitGeneralFunction(function);

                return;
            }
            if (TryEmitRoundedCallCase(function))
            {
                return;
            }

            if (function.Clauses is [var containerClause]
                && (owner.TryEmitLocalFunctionContainer(containerClause, entries) || owner.TryEmitConstantBoundTuple(containerClause)))
            {
                return;
            }
            if (function.Clauses is [{ Guard: null } mapClause]
                && (owner.TryEmitComputedMap(mapClause, location, entries)
                    || owner.TryEmitArgumentTupleMap(mapClause) || TryEmitDirectMap(mapClause)))
            {
                return;
            }
            if (function.Clauses is [{ Guard: null, Body: Expr.Binary { Operator: ErlangOperators.SubtractList } } listClause]
                && owner.TryEmitDirectAppend(listClause, location, entries))
            {
                return;
            }
            if (function.Clauses is [{ Guard: null } clockClause] && owner.TryEmitClockDelta(clockClause, location))
            {
                return;
            }
            if (recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null
                && function.Clauses is [var tagged, { Guard: null } fallback]
                && tagged.Patterns.Any(pattern => pattern is Pattern.Tuple)
                && HasUnconditionalParameters(fallback)
                && (TryEmitUnguardedTupleFieldCall(function) || TryEmitSeparateEntryFrames(function, owner.nextLabel - 2)))
            {
                return;
            }

            if (function.Clauses is [{ Guard: null, Body: Expr.Sequence sequence } constantClause]
                && HasUnconditionalParameters(constantClause)
                && sequence.Items.Count > 0
                && sequence.Items.Take(sequence.Items.Count - 1).All(item => FoldLiteralMatch(item) is Expr.Literal)
                && AttributeLiteral.TryNormalizeConstant(FoldLiteralMatch(sequence.Items[^1]), out Term constantResult))
            {
                owner.EmitConstant(constantResult, 0);
                owner.instructions.WriteByte(Return);

                return;
            }

            if (TryEmitNifErrorStub(function) || TryEmitCallResultConstruction(function)
                || TryEmitCallWithRetainedArgument(function) || TryEmitCapturedFoldRecordAppend(function)
                || TryEmitWxTextCommand(function)
                || TryEmitConstantRemoteCall(function) || TryEmitWxOptionCommand(function) || TryEmitWxOptionClosure(function) || TryEmitWxStaticCommand(function)
                || TryEmitWxScalarTupleOverload(function) || TryEmitWxObjectOverload(function) || TryEmitWxUnaryObjectOverload(function)
                || TryEmitWxObjectOptionOverload(function) || TryEmitWxClassUnionCommand(function) || TryEmitWxObjectCommand(function)
                || TryEmitWxGuardedForwardOverload(function) || TryEmitWxRepackedTupleCommand(function)
                || TryEmitWxTupleOverload(function) || TryEmitWxStaticTupleCommand(function)
                || TryEmitTupleOfListHeads(function) || TryEmitDirectFixedBitPattern(function) || TryEmitDirectMapPattern(function) || TryEmitDirectTryAfter(function) || TryEmitDirectBits(function) || TryEmitDirectFunctionValueCall(function) || TryEmitDirectArithmetic(function) || TryEmitDirectBooleanNot(function) || TryEmitDirectValueGuard(function) || TryEmitTaggedListResult(function) || TryEmitNumericComparison(function)
                || TryEmitGuardedLocalForward(function) || TryEmitRecordPredicateFunction(function) || TryEmitDirectGuardedRecordAccess(function) || TryEmitDirectTypeOrArityGuard(function) || TryEmitDirectListOrFunctionGuard(function) || TryEmitDirectCatch(function)
                || TryEmitDiscardedBranchCatch(function) || TryEmitEntryCatchResultDispatch(function) || TryEmitNestedListCallCatch(function)
                || TryEmitCaughtTaggedSingleton(function) || TryEmitCaughtTaggedApply(function)
                || owner.TryEmitDictionaryCall(function.Clauses, location, entries) || TryEmitNegatedPredicateTail(function)
                || TryEmitStateReturningDispatch(function) || TryEmitCheckedMapCalls(function)
                || TryEmitMapGuardedSend(function) || TryEmitTupleNodeGuard(function)
                || TryEmitForwardedChildList(function) || TryEmitSupervisorReceiverOptions(function)
                || TryEmitChildCountDispatch(function) || TryEmitConfiguredTableStart(function)
                || TryEmitTaggedParserResult(function) || TryEmitEvaluatedConfiguration(function)
                || TryEmitConfigurationDecode(function) || TryEmitProtectedCacheCall(function)
                || TryEmitConvertedCallTail(function) || TryEmitClosureWithConvertedArgument(function)
                || TryEmitSharedTuplePrefix(function) || TryEmitComputedScalar(function)
                || owner.TryEmitNestedTupleDynamicTail(function, location)
                || TryEmitBoundReplyReceive(function) || TryEmitRecordTransportOpen(function)
                || TryEmitLiteralTimeoutReceive(function) || TryEmitTimedMonitorReceive(function) || TryEmitConditionalTupleTail(function)
                || TryEmitGuardedSocketRecord(function) || TryEmitSocketOptionDispatch(function)
                || TryEmitSocketOptionRead(function) || TryEmitSocketStatsDispatch(function)
                || TryEmitSocketAddressDispatch(function) || TryEmitTupleResultAppend(function)
                || TryEmitRecordOptionValues(function) || TryEmitRecordFieldDispatch(function)
                || TryEmitGuardedPortResolution(function)
                || TryEmitNewTransportListener(function) || TryEmitListenerOwnership(function)
                || TryEmitListenerSocketRecord(function) || TryEmitConstantOptionDispatch(function)
                || TryEmitGuardedListLookup(function) || TryEmitClosedOptionValidation(function)
                || TryEmitPartitionedOptions(function) || TryEmitActiveSocketCheck(function)
                || TryEmitListenerReuse(function) || TryEmitMaybeListenerOpen(function)
                || TryEmitMaybeListenerBind(function) || TryEmitMaybeSocketConnect(function)
                || TryEmitMaybePlainSocketConnect(function)
                || TryEmitProtectedOutputSetup(function)
                || TryEmitOutputDispatch(function)
                || TryEmitLinkedRecordClosures(function)
                || TryEmitSocketAccept(function)
                || TryEmitSocketControllerTransfer(function)
                || TryEmitTupleKeyFilter(function) || TryEmitTupleFieldComprehension(function) || TryEmitClosedListMapper(function)
                || TryEmitTupleProjectionHelper(function) || TryEmitTaggedAccumulator(function)
                || TryEmitCertificateNameCheck(function)
                || TryEmitTlsServerHandshake(function)
                || TryEmitRemoteResultTailCall(function) || TryEmitListResultTailCall(function) || TryEmitLiteralSelectorTailCall(function)
                || TryEmitClosedAtomSelector(function) || TryEmitSplitTupleFormat(function)
                || TryEmitTaggedPairWithSavedCall(function) || TryEmitTaggedPairProjection(function)
                || TryEmitPreservedTaggedResult(function)
                || TryEmitReportedStateEvent(function)
                || TryEmitLiteralRedirectAndAddition(function) || TryEmitTaggedBinaryFormatting(function)
                || TryEmitLiteralRedirectAndIntegerTable(function) || TryEmitRecordDetailsCase(function)
                || TryEmitClosureReplyCase(function)
                || TryEmitMatchedRecordReply(function) || TryEmitLocalPageLookup(function) || TryEmitPageWithDefault(function)
                || TryEmitMemoryDetailPage(function) || TryEmitIndexedDetailPage(function) || TryEmitBinaryDigitList(function)
                || TryEmitBinaryDetailLookup(function) || TryEmitTermDetailLookup(function)
                || TryEmitCallbackPageList(function) || TryEmitTermCallbackPage(function) || TryEmitInterleavedCallList(function)
                || TryEmitDynamicDetailReply(function) || TryEmitConditionalProgressPulse(function) || TryEmitDetailWindow(function)
                || TryEmitCheckedDetailTab(function) || TryEmitScrolledDetailPanel(function) || TryEmitPageComprehension(function)
                || TryEmitNestedRecordEvent(function) || TryEmitRecordCleanup(function) || TryEmitGuardedPageArguments(function)
                || TryEmitReturnedRecordUpdate(function) || TryEmitPageRecord(function) || TryEmitDelayedPageInfo(function) || TryEmitRecordFieldCallUpdate(function) || TryEmitKnownRecordDispatch(function) || TryEmitWindowRecordExpansion(function) || TryEmitHtmlLinkEvent(function) || TryEmitListTupleTailCall(function) || TryEmitSupervisorChildNotification(function) || TryEmitProtectedListenerLookup(function)
                || TryEmitCapturedTableList(function) || TryEmitReadTableWithWarning(function)
                || TryEmitMemoryInfoFields(function) || TryEmitRecursiveFieldTitles(function) || TryEmitRecursiveTermExpansion(function)
                || TryEmitListDetails(function) || TryEmitMapDetailsPage(function) || TryEmitAtomListTitle(function)
                || TryEmitGuardedOption(function) || TryEmitOptionList(function))
            {
                return;
            }

            if (ReadClosedLocalMapping(function) is { } mapping
                && entries.TryGetValue((function.Name, 1), out int mappingEntry)
                && (mapping.Mapped.Module is not null || entries.ContainsKey((mapping.Mapped.Function, 2)))
                && (mapping.Conversion is not { Module: null } conversion
                    || !entries.ContainsKey((conversion.Function, 1))))
            {
                owner.EmitClosedLocalMapping(
                    mapping.Mapped,
                    mapping.Argument,
                    mapping.Conversion,
                    entries.GetValueOrDefault((mapping.Mapped.Function, 2)),
                    mappingEntry,
                    location,
                    mapping.Suffix
                );

                return;
            }

            if (TryEmitSequenceEncoding(function) || TryEmitSequenceEncodingWrapper(function)
                || TryEmitPairForwarding(function) || TryEmitForwardedCallResult(function)
                || TryEmitCapturedComprehensionParent(function)
                || TryEmitTerminalDecodedRange(function)
                || TryEmitOptionalDecode(function) || TryEmitSingletonListForwarding(function) || TryEmitEmptyOrSingleton(function)
                || TryEmitListLookup(function) || TryEmitSignedListLength(function))
            {
                return;
            }

            if (ReadSignedOctetAccumulator(function) is { } signedOctets
                && entries.TryGetValue((function.Name, 2), out int signedOctetEntry))
            {
                owner.EmitSignedOctetAccumulator(
                    signedOctets.Step,
                    signedOctets.Negative,
                    HasByteOctetAccumulator(function, signedOctets.Step),
                    HasOctetSign(function, signedOctets.Step, signedOctets.Negative)
                        ? IntegerTypeBits : ReadOctetNumberType(function, signedOctets.Negative),
                    signedOctetEntry,
                    location
                );

                return;
            }

            if (ReadMatchAndCollect(function) is { } collection
                && (collection.Guard.Module is not null || !entries.ContainsKey(("is_binary", 1)))
                && entries.TryGetValue((collection.Read.Function, 2), out int reader)
                && entries.TryGetValue((collection.Collect.Function, 1), out int collector))
            {
                owner.EmitMatchAndCollect(
                    collection.Read,
                    collection.Decision,
                    reader,
                    collector,
                    location
                );

                return;
            }

            if (ReadOctetAccumulator(function) is { } octetAccumulator
                && entries.TryGetValue((function.Name, 2), out int octetAccumulatorEntry))
            {
                long? lower = NaturalArgumentLowerBound(function, 0);
                owner.EmitOctetAccumulator(
                    octetAccumulator,
                    octetAccumulatorEntry,
                    location,
                    lower,
                    lower.HasValue ? IntegerTypeBits : ReadOctetNumberType(function, negative: false)
                );

                return;
            }

            if (ReadTaggedLengthEncoding(function) is { } taggedLength
                && (taggedLength.Size.Module is not null || !entries.ContainsKey(("byte_size", 1)))
                && entries.TryGetValue((function.Name, 3), out int taggedEntry)
                && entries.TryGetValue((taggedLength.Length.Function, 1), out int lengthEntry)
                && localFunctions.TryGetValue((taggedLength.Length.Function, 1), out var lengthFunction)
                && ReadLengthEncoding(lengthFunction) is { } lengthFlow
                && (lengthFlow.Length.Module is not null || !entries.ContainsKey(("length", 1)))
                && (lengthFlow.Exit.Module is not null || !entries.ContainsKey(("exit", 1))))
            {
                PairFacts joined = ReadJoinedNumericArgument(function, 2);
                bool numeric = (joined & PairFacts.Numeric) != 0;
                long? lower = numeric && (joined & PairFacts.Natural) != PairFacts.Natural
                    ? null : NaturalArgumentLowerBound(function, 2);
                owner.EmitTaggedLengthEncoding(
                    taggedLength,
                    taggedEntry,
                    lengthEntry,
                    location,
                    lower,
                    lower.HasValue || numeric
                );

                return;
            }

            if (ReadLengthEncoding(function) is { } length
                && (length.Length.Module is not null || !entries.ContainsKey(("length", 1)))
                && (length.Exit.Module is not null || !entries.ContainsKey(("exit", 1)))
                && entries.TryGetValue((length.Octets.Function, 1), out int octetsEntry))
            {
                bool properOctets = (InferPairFacts(length.Octets) & PairFacts.ProperList) != 0;
                owner.EmitLengthEncoding(
                    length,
                    octetsEntry,
                    location,
                    properOctets,
                    NaturalArgumentLowerBound(function, 0)
                );

                return;
            }

            if (ReadBitPartCollection(function) is { } bitParts
                && (bitParts.Finish.Module is not null || !entries.ContainsKey(("list_to_binary", 1)))
                && entries.TryGetValue((function.Name, 3), out int bitCollectionEntry))
            {
                owner.EmitBitPartCollection(
                    bitParts,
                    bitCollectionEntry,
                    location,
                    HasNaturalBitPartTotal(function, bitParts)
                );

                return;
            }

            if (ReadPartCollection(function) is { } parts
                && !entries.ContainsKey(("is_list", 1))
                && (parts.Finish.Module is not null || !entries.ContainsKey(("list_to_binary", 1)))
                && entries.TryGetValue((function.Name, 2), out int collectionEntry)
                && entries.TryGetValue((parts.Nested.Function, 1), out int nestedEntry)
                && entries.TryGetValue((parts.Bits.Function, 3), out int bitsEntry))
            {
                owner.EmitPartCollection(
                    parts,
                    collectionEntry,
                    nestedEntry,
                    bitsEntry,
                    location
                );

                return;
            }

            if (ReadTagMatch(function) is { } exit
                && (exit.Module is not null || !entries.ContainsKey(("exit", 1)))
                && entries.TryGetValue((function.Name, 2), out int entry))
            {
                owner.EmitTagMatch(exit, entry, location);

                return;
            }

            if (TryEmitSignedDecode(function) || TryEmitFixedDecode(function)
                || TryEmitLiteralEncoder(function) || TryEmitMeasuredEncoder(function)
                || owner.TryEmitSingletonListConstructor(function, location, entries) || TryEmitNestedListRegistration(function)
                || TryEmitListPrefixTailCall(function) || TryEmitSingletonListTailCall(function)
                || TryEmitListCallAndConstant(function) || TryEmitTerminalCallList(function) || TryEmitAppendFormatTail(function)
                || TryEmitRegisteredProcessCase(function)
                || TryEmitTaggedTupleAndConstantsTail(function) || TryEmitTupleElementAtomDispatchTail(function)
                || TryEmitTupleListTailCall(function)
                || TryEmitNestedArgumentTailCall(function)
                || TryEmitNilArgumentDispatch(function) || TryEmitRecordListPrefix(function)
                || TryEmitGuardedTaggedFieldTailCall(function)
                || TryEmitTupleRemoteDispatch(function)
                || TryEmitBinaryLiteralDispatch(function) || owner.TryEmitTupleMembership(function)
                || owner.TryEmitTupleResultDispatch(function)
                || owner.TryEmitLiteralTupleReturns(function)
                || TryEmitSocketConnectSequence(function) || TryEmitSocketAcceptSequence(function) || TryEmitSocketReplySequence(function)
                || TryEmitPathOptionList(function) || TryEmitTaggedCallProjection(function) || TryEmitPairedListTailCalls(function)
                || TryEmitRegisterIntegerMembership(function) || TryEmitNestedTaggedForward(function)
                || TryEmitRegisterPredicate(function) || TryEmitAtomCheckedTailCall(function)
                || TryEmitComputedLiteralDispatch(function) || TryEmitLiteralTailClauses(function)
                || TryEmitGuardedConstantReturns(function) || TryEmitGuardedRemoteTailCall(function)
                || TryEmitLiteralPatternTailCall(function) || TryEmitNilArgumentReturn(function)
                || TryEmitRepeatedArgumentReturns(function) || TryEmitLiteralConstantReturn(function)
                || TryEmitNilTailTupleFallback(function)
                || TryEmitGuardedIntegerForward(function) || TryEmitProjectedChoice(function)
                || TryEmitSingleOptionalField(function) || TryEmitDeferredTupleFields(function)
                || TryEmitRegisterLookupCase(function) || TryEmitForwardedCaseInput(function)
                || TryEmitProtectedProjection(function) || TryEmitProtectedProjectionArgument(function)
                || TryEmitListDecoderEntry(function) || TryEmitRegisterListClause(function)
                || TryEmitCheckedListIdentity(function) || TryEmitListConversionTuple(function)
                || TryEmitGroupedArityDispatch(function) || owner.TryEmitAtomReplacementDispatch(function)
                || owner.TryEmitAtomIntegerDispatch(function, location)
                || owner.TryEmitAtomAliasDispatch(function, entries) || TryEmitLiteralResultDispatch(function)
                || TryEmitRegisterTailCall(function)
                || TryEmitNonAllocatingBifOperands(function) || TryEmitRegisterConstructedArgument(function)
                || TryEmitEmbeddedConstructorTail(function) || TryEmitReusedTupleCaseTail(function)
                || TryEmitLiteralEntryCallCase(function) || TryEmitEffectfulTupleOrAtomCase(function))
            {
                return;
            }
            EmitGeneralFunction(function);
        }

        private bool TryEmitRegisterTailCall(FunctionDefinition function, bool dispatchOnly = false)
        {
            var branches = new List<(Expr.Call Target, (byte Opcode, int Register)? Guard, (int? Register, Term? Value)[]? List, (int Register, Atom Tag)? Tuple)>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause.Body is not Expr.Call { DynamicRemote: false } call)
                {
                    return false;
                }

                if (call.Arguments.Any(argument => !ReferenceEquals(FoldLiteralMatch(argument), argument)))
                {
                    call = call with { Arguments = call.Arguments.Select(FoldLiteralMatch).ToArray() };
                }

                var tuple = ReadRegisterTupleTail(clause, call);
                if (tuple is null && clause.Patterns.Any(pattern => pattern is not
                        (Pattern.Variable { } or Pattern.Literal { Value: Atom or Integer or Nil })))
                {
                    return false;
                }

                var parameters = new HashSet<string>(StringComparer.Ordinal);
                foreach (Pattern.Variable parameter in clause.Patterns.OfType<Pattern.Variable>())
                {
                    if (parameter.Name != VariableScopeNames.Wildcard && !parameters.Add(parameter.Name))
                    {
                        return false;
                    }
                }

                Expr.Call target = owner.ResolveImportedCall(call, entries);
                var list = ReadRegisterTailList(clause, target);
                if (target.Module == ErlangModuleNames.Module
                    || target.Module is null && !entries.ContainsKey((target.Function, target.Arguments.Count))
                    || tuple is null && list is null && target.Arguments.Any(argument => argument is Expr.Variable variable
                        ? !parameters.Contains(variable.Name)
                        : !AttributeLiteral.TryNormalizeConstant(argument, out _)))
                {
                    return false;
                }

                var guard = ReadEntryTypeGuard(clause, clause.Guard);
                if (clause.Guard is not null && guard is null)
                {
                    return false;
                }

                branches.Add((target, guard, list, tuple));
            }

            int functionInfo = owner.nextLabel - 2;
            int selector = Enumerable.Range(0, function.Arity).FirstOrDefault(
                index => function.Clauses[0].Patterns[index] is Pattern.Literal { Value: Atom or Integer },
                -1
            );
            bool hasFallback = HasUnconditionalParameters(function.Clauses[^1]);
            int keyCount = branches.Count - (hasFallback ? 1 : 0);
            if (selector >= 0 && keyCount >= 2
                && branches.All(branch => branch.Guard is null && branch.List is null && branch.Tuple is null)
                && function.Clauses.Take(keyCount).All(clause =>
                    clause.Patterns[selector] is Pattern.Literal { Value: Atom or Integer }
                    && clause.Patterns.Where((_, index) => index != selector).All(pattern => pattern is Pattern.Variable)))
            {
                var keys = function.Clauses.Take(keyCount)
                    .Select(clause => ((Pattern.Literal)clause.Patterns[selector]).Value).ToArray();
                if (keys.Distinct().Count() == keys.Length
                    && (keys.All(key => key is Atom) || keys.All(key => key is Integer)))
                {
                    if (!hasFallback && keys[0] is Atom
                        && ReadFiniteAtomArguments(function, selector) is { } values
                        && values.All(atom => keys.Contains(atom)) && values.Count <= 2)
                    {
                        int[] reachable = Enumerable.Range(0, keyCount)
                            .Where(index => keys[index] is Atom atom && values.Contains(atom))
                            .OrderByDescending(index => keys[index], Comparer<Term>.Create(
                                (left, right) => TermOrder.Compare(left, right, exact: true)
                            )).ToArray();
                        if (reachable is [var first, var second])
                        {
                            int alternative = Label();
                            owner.Op(ExactEqualTest, LabelTag, alternative);
                            owner.Operand(XRegisterTag, selector);
                            owner.EmitPatternOperand(keys[first]);
                            EmitReachableTail(first);
                            Mark(alternative);
                            EmitReachableTail(second);
                        }
                        else
                        {
                            EmitReachableTail(reachable[0]);
                        }

                        return true;
                    }

                    var indexedKeys = keys.Select((key, index) => (Key: key, Index: index));
                    var ordered = keys[0] is Atom
                        ? indexedKeys.OrderBy(branch => branch.Key, Comparer<Term>.Create(
                            (left, right) => TermOrder.Compare(left, right, exact: true)
                        ))
                        : indexedKeys.OrderBy(branch => ((Integer)branch.Key).Value);
                    var targets = new List<(int Index, EntryCall? Plan)>();
                    var dispatch = new List<(Term Key, int Target)>();
                    foreach (var (key, index) in ordered)
                    {
                        var target = branches[index].Target;
                        EntryCall? plan = target.Module is null && (hasFallback || key is Integer)
                            ? ReadEntryCall(
                                target,
                                function.Clauses[index].Patterns
                                .Select(pattern => (pattern as Pattern.Variable)?.Name ?? VariableScopeNames.Wildcard).ToArray()
                            )
                            : null;
                        int shared = plan is null ? -1 : targets.FindIndex(branch =>
                            branch.Plan is { } previous && previous.Call.Function == plan.Call.Function
                            && previous.Call.Arguments.Count == plan.Call.Arguments.Count
                            && previous.Moves.SequenceEqual(plan.Moves) && previous.Constants.SequenceEqual(plan.Constants));
                        if (shared < 0)
                        {
                            shared = targets.Count;
                            targets.Add((index, plan));
                        }

                        dispatch.Add((key, shared));
                    }

                    int[] targetOrder = Enumerable.Range(0, targets.Count).Reverse().ToArray();
                    var labels = new int[targets.Count];
                    foreach (int targetIndex in targetOrder)
                    {
                        labels[targetIndex] = Label();
                    }

                    int failure = hasFallback ? Label() : functionInfo;
                    var bounds = keys[0] is Integer ? owner.IntegerEntryBounds(function, selector, localFunctions) : null;
                    owner.EmitMembershipSelect(
                        selector,
                        failure,
                        dispatch.Select(entry => (entry.Key, labels[entry.Target])),
                        bounds.HasValue ? IntegerTypeBits : keys[0] is Atom ? ReadAtomEntryType(function, selector) : 0,
                        bounds
                    );
                    foreach (int targetIndex in targetOrder)
                    {
                        var branch = targets[targetIndex];
                        Mark(labels[targetIndex]);
                        owner.EmitCall(
                            function.Clauses[branch.Index],
                            location,
                            branches[branch.Index].Target,
                            entries,
                            orderTailMoves: true
                        );
                    }

                    void EmitReachableTail(int index) => owner.EmitCall(
                        function.Clauses[index],
                        location,
                        branches[index].Target,
                        entries,
                        orderTailMoves: true
                    );

                    if (hasFallback)
                    {
                        Mark(failure);
                        owner.EmitCall(
                            function.Clauses[^1],
                            location,
                            branches[^1].Target,
                            entries,
                            orderTailMoves: true
                        );
                    }

                    return true;
                }
            }

            if (dispatchOnly)
            {
                return false;
            }

            for (int index = 0; index < function.Clauses.Count; index++)
            {
                Clause clause = function.Clauses[index];
                bool last = index == function.Clauses.Count - 1;
                var (target, guard, list, tuple) = branches[index];
                bool hasTests = guard is not null || !HasUnconditionalParameters(clause);
                int failure = hasTests && !last ? Label() : functionInfo;
                for (int register = 0; register < clause.Patterns.Count; register++)
                {
                    if (tuple?.Register == register)
                    {
                        EmitTupleShapeTest(
                            XRegisterTag,
                            register,
                            2,
                            failure
                        );
                    }
                    else if (clause.Patterns[register] is Pattern.Literal literal)
                    {
                        owner.Op(ExactEqualTest, LabelTag, failure);
                        owner.Operand(XRegisterTag, register);
                        owner.LiteralOperand(literal.Value);
                    }
                }

                if (guard is { } test)
                {
                    owner.Op(test.Opcode, LabelTag, failure);
                    owner.Operand(XRegisterTag, test.Register);
                }

                if (tuple is { } pair)
                {
                    EmitRegisterTupleTail(target, pair.Register, pair.Tag);
                }
                else if (list is null)
                {
                    owner.EmitCall(
                        clause,
                        location,
                        target,
                        entries,
                        orderTailMoves: true
                    );
                }
                else
                {
                    EmitRegisterTailList(target, list);
                }

                if (!hasTests || last)
                {
                    break;
                }

                Mark(failure);
            }

            return true;
        }

        private bool TryEmitNifErrorStub(FunctionDefinition function)
        {
            const string nifError = "nif_error";
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Call
                    {
                        Module: ErlangModuleNames.Module, Function: nifError, DynamicRemote: false,
                        Arguments: [var reason]
                    } call
                } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            if (!AttributeLiteral.TryNormalizeConstant(reason, out Term constant))
            {
                if (clause.Patterns is not [Pattern.Variable parameter]
                    || owner.IsDeclaredNative((function.Name, function.Arity))
                    || ReadPrivateIntegerConstant(function, 0, new()) is not { } value
                    || !AttributeLiteral.TryNormalizeConstant(reason, out constant,
                        new Dictionary<string, Term>(StringComparer.Ordinal) { [parameter.Name] = value }))
                {
                    return false;
                }
            }

            owner.EmitConstant(constant, 0);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, nifError, 1));

            return true;
        }

        private void EmitGeneralFunction(FunctionDefinition function, int? entryInfo = null)
        {
            int functionInfo = entryInfo ?? owner.nextLabel - 2;
            if (recursiveClosure is null)
            {
                function = FoldLeadingCaseConstants(FoldLeadingDynamicModule(function));
            }
            if (recursiveClosure is null && TryEmitDynamicUnicodePair(function, functionInfo))
            {
                return;
            }
            var clauses = function.Clauses;
            if (recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null
                && TryForwardingDispatch(function, out bool fallbackEmitted))
            {
                if (fallbackEmitted)
                {
                    return;
                }
                clauses = [clauses[^1]];
            }
            if (recursiveClosure is null && clauses.Count == function.Clauses.Count)
            {
                int emitted = EmitGuardedBifTailPrefix(clauses, functionInfo);
                if (emitted == clauses.Count)
                {
                    return;
                }

                if (emitted != 0)
                {
                    clauses = clauses.Skip(emitted).ToArray();
                }
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && TryEmitConstructedEntryWithFallback(function))
            {
                return;
            }

            if (recursiveClosure is null && function.Clauses.Count == 1 && function.Arity != 0
                && TryEmitTupleEntryWithRegisterFields(clauses[0], functionInfo, baseSlot: null))
            {
                return;
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && (TryEmitGroupedTupleEntry(function, functionInfo)
                    || TryEmitSharedTaggedTupleDispatch(function, functionInfo)))
            {
                return;
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && TryEmitConsCallMatch(clauses, functionInfo))
            {
                return;
            }

            if (recursiveClosure is null && function.Clauses.Count == 1
                && (TryEmitEntryListDefault(clauses[0]) || TryEmitEntryCallTuple(clauses[0])))
            {
                return;
            }

            if (recursiveClosure is null && clauses is [{ Guard: null, Body: Expr.Call } direct]
                && HasUnconditionalParameters(direct)
                && TryEmitEntryArithmeticCall(
                    function,
                    direct,
                    direct.Body,
                    direct.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray(),
                    -1,
                    null
                ))
            {
                return;
            }

            if (recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null
                && clauses.Count == 1
                && (TryEmitEntryCallComparison(clauses[0]) || TryEmitEntryCallTupleTail(clauses[0])
                    || TryEmitEntryTupleOrAtomCase(clauses[0]) || TryEmitEntryDiscardedTaggedMatch(clauses[0])
                    || TryEmitEntryTupleMatchResult(clauses[0])
                    || TryEmitNestedTupleFieldCalls(clauses[0])
                    || TryEmitEntryTupleArithmeticResult(clauses[0])
                    || TryEmitEntryTupleAndCallResult(clauses[0])
                    || recursiveClosure is null && TryEmitEntryBinaryReturn(clauses[0])
                    || TryEmitEntryArithmeticCase(clauses[0])
                    || TryEmitSingletonListResultPrefix(clauses[0])
                    || TryEmitCheckedListEntry(clauses[0])
                    || TryEmitEntryLookupProjection(clauses[0]) || TryEmitEntryTaggedForward(clauses[0]) || TryEmitEntryTaggedProjection(clauses[0]) || TryEmitEntryTaggedTailCall(clauses[0])
                    || TryEmitEntryTaggedCallTuple(clauses[0]) || TryEmitCallTupleCaseProjection(clauses[0])
                    || TryEmitTupleCaseCallContinuation(clauses[0])
                    || TryEmitEntryRegisterCase(clauses[0])
                    || TryEmitTupleResultCase(clauses[0])
                    || TryEmitSavedCallSequenceCase(clauses[0]) || TryEmitEntryClosureTail(clauses[0])
                    || TryEmitSavedCallSequenceTuple(clauses[0])
                    || TryEmitEntryBifTail(clauses[0]) || TryEmitEntryDynamicApplyTail(clauses[0])
                    || TryEmitDictionarySequenceTail(clauses[0])
                    || TryEmitEntryAtomTailDispatch(clauses[0]) || TryEmitEntryAtomCallTails(clauses[0]) || TryEmitEntryCallCaseWithFallback(clauses[0]) || TryEmitDiscardedCallSequence(clauses[0]) || TryEmitNestedEntryCalls(clauses[0]) || TryEmitSiblingEntryCalls(clauses[0])
                    || TryEmitCheckedCallSequence(clauses[0]) || TryEmitEntryCallWithComputedOperands(clauses[0])))
            {
                return;
            }

            if (recursiveClosure is null && function.Arity > 0 && function.Clauses.Count == 1
                && (TryEmitDeferredRecordList(clauses[0], functionInfo)
                    || TryEmitTupleEntryWithRegisterFields(
                        clauses[0],
                        functionInfo,
                        nextSlot,
                        checkShape: true
                    )
                    || TryEmitPlannedTupleEntry(clauses[0], functionInfo)))
            {
                return;
            }

            if (recursiveClosure is null && clauses is [var construction] && TryEmitEntryConstructionSequence(construction))
            {
                return;
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && (TryEmitGuardedListConstructors(function, functionInfo)
                    || TryEmitNilOrTupleResultConsTail(function)
                    || TryEmitListRecordMerge(function, functionInfo)
                    || TryEmitSharedListRecordTail(function, functionInfo)
                    || TryEmitRecordGuardedListCase(function, functionInfo)
                    || TryEmitConsAndNilTailCalls(clauses, functionInfo)
                    || TryEmitGuardedConsCaseDispatch(function, functionInfo)
                    || TryEmitSingletonConsDispatch(function, functionInfo)
                    || TryEmitArithmeticListEntry(function, functionInfo)
                    || TryEmitLiteralTupleClosures(function, functionInfo)
                    || TryEmitSeparateEntryFrames(function, functionInfo)))
            {
                return;
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && TryEmitSharedConsLookahead(function))
            {
                clauses = [clauses[^1]];
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count)
            {
                int emitted = EmitRegisterConsLiteralPrefix(function);
                if (emitted > 0)
                {
                    clauses = clauses.Skip(emitted).ToArray();
                }
            }

            if (recursiveClosure is null && clauses.Count == 1
                && TryEmitEmptyListClosureCallChain(function, clauses[0], functionInfo))
            {
                return;
            }

            var listFallback = recursiveClosure is null && clauses.Count == function.Clauses.Count
                ? ReadEntryListReturn(clauses)
                : null;
            int listFallbackLabel = 0;
            if (listFallback is { } list)
            {
                if (TryEmitListHeadTail(clauses, list, functionInfo)
                    || TryEmitListHeadConstructedCalls(clauses, list, functionInfo))
                {
                    return;
                }

                if (clauses.Count == 2 && TryEmitConsEntryInRegisters(
                    clauses[0], list.Input, functionInfo, EmitListFallbackTest, allowFramedBody: true))
                {
                    EmitEmptyListReturn(list, listFallbackLabel, functionInfo);

                    return;
                }
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count
                && TryEmitStructuralListEntry(function, functionInfo))
            {
                return;
            }

            if (listFallback is not null)
            {
                EmitListFallbackTest();
                clauses = clauses.Take(clauses.Count - 1).ToArray();
            }

            void EmitListFallbackTest()
            {
                listFallbackLabel = Label();
                owner.EmitNonemptyList(listFallback!.Value.Input, listFallbackLabel);
            }

            if (recursiveClosure is null && clauses.Count == function.Clauses.Count)
            {
                function = FoldEntryResultCase(function);
                clauses = function.Clauses;
            }

            Pattern?[]? entryPatterns = null;
            (bool Shape, bool Arity, bool Tag)[]? sharedEntryTests = null;
            Expr? entryGuard = null;
            int capturedEntryFailure = recursiveClosure?.EntryPatternFailure == true ? Label() : 0;
            bool canRejectAtEntry = capturedEntryFailure != 0
                || recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null;
            if (clauses.Count == 1 && function.Clauses.Count == 1 && canRejectAtEntry)
            {
                if (recursiveClosure is null && TryEmitProvenConsTail(function, clauses[0]))
                {
                    return;
                }

                entryPatterns = EmitEntryShapeTests(function, clauses[0],
                    capturedEntryFailure == 0 ? functionInfo : capturedEntryFailure);
                entryGuard = EmitEntryGuardPrefix(clauses[0], clauses[0].Guard, functionInfo);
            }
            else if (clauses.Count == function.Clauses.Count && canRejectAtEntry)
            {
                sharedEntryTests = EmitSharedEntryTests(function, clauses, functionInfo);
            }

            if (entryPatterns is not null && entryGuard is null && clauses[0].Guard is not null
                && TryEmitEntryCallTuple(clauses[0] with { Guard = null }))
            {
                return;
            }

            var entryTupleLiterals = (entryPatterns is not null || sharedEntryTests is not null)
                && function.Arity < FirstReservedXRegister
                ? EmitSharedEntryTupleLiterals(clauses, functionInfo)
                : null;

            if (listFallback is { } selectedList)
            {
                sharedEntryTests = new (bool Shape, bool Arity, bool Tag)[function.Arity];
                sharedEntryTests[selectedList.Input] = (true, false, false);
            }

            var tupleFallback = recursiveClosure is null && sharedEntryTests is not null ? ReadEntryTupleFallback(clauses, sharedEntryTests) : null;
            int tupleFallbackLabel = 0;
            if (tupleFallback is { } tupleBranch)
            {
                tupleFallbackLabel = Label();
                if (HasTupleTag(tupleBranch.Pattern, out Atom tag))
                {
                    EmitDetailRecordTest(
                        tupleBranch.Argument,
                        tupleBranch.Pattern.Items.Count,
                        tag,
                        tupleFallbackLabel
                    );
                }
                else
                {
                    EmitTupleShapeTest(
                        XRegisterTag,
                        tupleBranch.Argument,
                        tupleBranch.Pattern.Items.Count,
                        tupleFallbackLabel
                    );
                }

                if (TryEmitTupleFallbackRecordUpdate(clauses[0], tupleBranch.Argument, tupleBranch.Pattern))
                {
                    Mark(tupleFallbackLabel);
                    owner.EmitConstant(tupleBranch.Value, 0);
                    owner.instructions.WriteByte(Return);

                    return;
                }

                sharedEntryTests![tupleBranch.Argument] = (true, true, true);
            }

            bool hasFailureEpilogue = capturedEntryFailure == 0 && tupleFallback is null && (recursiveClosure is not null || clauses[^1].Guard is not null
                || !HasUnconditionalParameters(clauses[^1]));
            bool[]? neededAcrossClauses = clauses.Count > 1 && !hasFailureEpilogue
                ? ReadNeededEntryArguments(clauses)
                : null;
            var usedParameters = capturedEntryFailure == 0 && clauses.Count == 1 && !hasFailureEpilogue
                ? ReadUsedEntryParameters(clauses[0])
                : null;

            EntryCaughtApply? caughtApply = recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null
                && clauses.Count == 1 && clauses[0].Guard is null && HasUnconditionalParameters(clauses[0])
                ? ReadEntryCaughtApply(clauses[0])
                : null;
            if (caughtApply is not null)
            {
                usedParameters = caughtApply.Survivors;
                hasFailureEpilogue = false;
            }

            int entryBindings = capturedEntryFailure == 0 && clauses.Count == 1 && !hasFailureEpilogue
                ? CountSafeEntryBindings(clauses[0])
                : 0;
            if (entryBindings == 0 && frameSize == 2 && recursiveClosure is null
                && function is
                {
                    Arity: 0, Clauses: [
                    {
                        Guard: null, Body: Expr.Sequence
                        {
                            Items: [
                        Expr.Match { Pattern: Pattern.Variable { }, Value: Expr.Call },
                        Expr.Match { Pattern: Pattern.Variable { }, Value: Expr.Case },
                        Expr.Match { Pattern: Pattern.Variable { }, Value: Expr.Call },
                        Expr.Match { Pattern: Pattern.Variable { }, Value: Expr.Case },
                        Expr.Call
                    ]
                        }
                    }]
                })
            {
                // OTP reverses allocated Y registers within each frame. This sequence
                // initializes the whole frame before its first saved call result.
                decoderSlotCount = frameSize;
            }
            var entryRegisters = new Dictionary<string, int>(StringComparer.Ordinal);
            HashSet<string>? entrySurvivors = null;
            int nextEntryRegister = function.Arity;
            if (entryBindings != 0 && clauses[0].Body is Expr.Sequence entryBody)
            {
                var continuation = new Expr.Sequence(entryBody.Items.Skip(entryBindings).ToArray());
                usedParameters = ReadUsedEntryParameters(new Clause(clauses[0].Patterns, null, continuation))
                    ?? usedParameters;
                if (function.Arity + entryBindings <= FirstReservedXRegister)
                {
                    var candidates = clauses[0].Patterns.Concat(entryBody.Items.Take(entryBindings)
                        .Cast<Expr.Match>().Select(match => match.Pattern)).ToArray();
                    entrySurvivors = ReadUsedEntryParameters(new Clause(candidates, null, continuation));
                }

                if (usedParameters is not null)
                {
                    for (int index = 0; index < function.Arity; index++)
                    {
                        string parameter = ((Pattern.Variable)clauses[0].Patterns[index]).Name;
                        if (!usedParameters.Contains(parameter))
                        {
                            entryRegisters[parameter] = index;
                        }
                    }
                }
            }

            EntryCall? firstCall = null;
            EntryCall? entryCasePrefix = null;
            Expr.Case? entryCallCase = null;
            Expr.Binary? entryCaseArithmetic = null;
            ForwardedCall? forwardedCall = null;
            Expr.Sequence? afterFirstCall = null;
            Pattern.Variable? firstResult = null;
            (Expr.Match Match, Pattern.Tuple Pattern, bool KnownShape, int ForwardedField)? firstTupleResult = null;
            bool saveFirstResult = false;
            if (entryBindings == 0 && clauses.Count == 1
                && (clauses[0].Guard is null || entryPatterns is not null && entryGuard is null)
                && recursiveClosure?.SelfName is null && recursiveClosure?.GuardFailure is null
                && HasUnconditionalParameters(clauses[0]))
            {
                var parameters = clauses[0].Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
                var sequence = clauses[0].Body as Expr.Sequence;
                Expr firstItem = sequence?.Items[0] ?? clauses[0].Body;
                var firstMatch = firstItem as Expr.Match;
                Expr caseExpression = firstMatch?.Value ?? firstItem;
                if (caseExpression is Expr.Case decision
                    && (firstMatch is null || sequence is not null && firstMatch.Pattern is Pattern.Variable))
                {
                    EntryCall? selectorCall = null;
                    EntryCall? prefix = null;
                    Expr.Binary? arithmetic = null;
                    Expr liveSelector = new Expr.Literal(Nil.Value);
                    if (decision.Value is Expr.Call selector)
                    {
                        selectorCall = ReadEntryCall(selector, parameters, allowConstructedArgument: recursiveClosure is null);
                        if (selectorCall is null && recursiveClosure is null
                            && ReadEntrySelector(selector, parameters, []) is
                            { Prefix: { } inner, Call: { SavedInputs: null or { Count: 0 } } outer })
                        {
                            prefix = inner;
                            selectorCall = outer;
                        }
                    }
                    else if (recursiveClosure is null && decision.Value is Expr.Binary binary
                        && ReadEntryCaseArithmetic(binary, parameters) is { } input)
                    {
                        arithmetic = binary;
                        selectorCall = input.Call;
                        liveSelector = input.Other;
                    }

                    Expr withoutSelector = decision with { Value = liveSelector };
                    Expr liveBody = sequence is null ? withoutSelector : new Expr.Sequence(
                        [firstMatch is null ? withoutSelector : firstMatch with { Value = withoutSelector },
                            .. sequence.Items.Skip(1)]);
                    if (selectorCall is not null
                        && ReadUsedEntryParameters(clauses[0] with { Body = liveBody }) is { } survivors)
                    {
                        usedParameters = survivors;
                        firstCall = selectorCall;
                        entryCasePrefix = prefix;
                        entryCallCase = decision;
                        entryCaseArithmetic = arithmetic;
                        hasFailureEpilogue = false;
                    }
                }

                var callSequence = ReadEntryTupleSequence(clauses[0].Body, parameters) ?? clauses[0].Body as Expr.Sequence;
                Expr? first = callSequence?.Items.FirstOrDefault();
                var candidate = first as Expr.Call;
                if (first is Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call value }
                    && !parameters.Contains(result.Name, StringComparer.Ordinal))
                {
                    firstResult = result;
                    candidate = value;
                }

                var tupleMatch = recursiveClosure is null && first is Expr.Match
                { Pattern: Pattern.Tuple { }, Value: Expr.Call } tupleBinding ? tupleBinding : null;
                if (tupleMatch?.Value is Expr.Call tupleCall)
                {
                    candidate = tupleCall;
                }

                if (callSequence is { Items.Count: >= 2 } && candidate is not null
                    && ReadEntryCall(candidate, parameters, allowConstructedArgument: recursiveClosure is null) is { } plannedCall)
                {
                    var continuation = new Expr.Sequence(callSequence.Items.Skip(1).ToArray());
                    var bindings = firstResult is null ? clauses[0].Patterns : [.. clauses[0].Patterns, firstResult];
                    if (tupleMatch is not null)
                    {
                        if (ReadEntryTupleBinding(
                            tupleMatch,
                            parameters,
                            continuation,
                            plannedCall
                        ) is { } tuplePlan)
                        {
                            usedParameters = tuplePlan.Survivors;
                            firstTupleResult = (tupleMatch, tuplePlan.Pattern, tuplePlan.KnownShape, tuplePlan.ForwardedField);
                            forwardedCall = tuplePlan.NextCall;
                            hasFailureEpilogue = false;
                            firstCall = plannedCall;
                            afterFirstCall = continuation;
                        }
                    }
                    else if (ReadUsedEntryParameters(new Clause(bindings, null, continuation)) is { } survivors)
                    {
                        usedParameters = survivors;
                        saveFirstResult = firstResult is not null && survivors.Contains(firstResult.Name);
                        if (saveFirstResult && firstResult is { } boundResult)
                        {
                            forwardedCall = ReadForwardedCall(continuation, boundResult.Name, parameters);
                            saveFirstResult = forwardedCall is null;
                        }

                        hasFailureEpilogue = false;
                        firstCall = plannedCall;
                        afterFirstCall = continuation;
                    }
                }
            }

            int sharedConsInput = -1;
            if (recursiveClosure is null && function.Arity + 2 < FirstReservedXRegister)
            {
                for (int index = 0; index < function.Arity; index++)
                {
                    if ((sharedEntryTests?[index].Shape == true || entryPatterns?[index] is Pattern.List)
                        && clauses.All(clause => clause.Patterns[index] is Pattern.List { Items.Count: 1, Tail: not null }))
                    {
                        sharedConsInput = index;
                        break;
                    }
                }
            }

            int live = function.Arity;
            int headRegister = -1;
            int tailRegister = -1;
            bool checkedConsHead = false;
            if (sharedConsInput >= 0)
            {
                bool needsHead = clauses.Any(clause => ((Pattern.List)clause.Patterns[sharedConsInput]).Items[0]
                    is not Pattern.Variable { Name: VariableScopeNames.Wildcard });
                bool needsTail = clauses.Any(clause => ((Pattern.List)clause.Patterns[sharedConsInput]).Tail
                    is not Pattern.Variable { Name: VariableScopeNames.Wildcard });
                if (needsHead)
                {
                    headRegister = live++;
                }

                if (needsTail)
                {
                    tailRegister = live++;
                }

                if (needsHead && needsTail)
                {
                    owner.EmitListParts(
                        sharedConsInput,
                        XRegisterTag,
                        headRegister,
                        XRegisterTag,
                        tailRegister
                    );
                }
                else if (needsHead || needsTail)
                {
                    owner.Op(needsHead ? GetHead : GetTail, XRegisterTag, sharedConsInput);
                    owner.Operand(XRegisterTag, needsHead ? headRegister : tailRegister);
                }

                if (headRegister >= 0
                    && EntryShape(((Pattern.List)clauses[0].Patterns[sharedConsInput]).Items[0]) is Pattern.Tuple tuple
                    && HasTupleTag(tuple, out Atom headTag)
                    && clauses.All(clause =>
                        EntryShape(((Pattern.List)clause.Patterns[sharedConsInput]).Items[0]) is Pattern.Tuple other
                        && other.Items.Count == tuple.Items.Count && HasTupleTag(other, out Atom tag) && headTag.Equals(tag)))
                {
                    EmitDetailRecordTest(
                        headRegister,
                        tuple.Items.Count,
                        headTag,
                        functionInfo
                    );
                    checkedConsHead = true;
                }
            }

            Pattern.Tuple? directConsHead = checkedConsHead && function.Clauses.Count == 1
                ? ReadDirectConsHead(clauses[0], sharedConsInput) : null;
            HashSet<string>? directUses = null;
            if (directConsHead is not null)
            {
                hasFailureEpilogue = false;
                var cons = (Pattern.List)clauses[0].Patterns[sharedConsInput];
                directUses = ReadUsedEntryParameters(clauses[0] with
                {
                    Patterns = [.. clauses[0].Patterns.OfType<Pattern.Variable>(),
                        .. directConsHead.Items.OfType<Pattern.Variable>(), cons.Tail!]
                });
            }

            EmitFrameAllocation(live, frameSize, heapWords: firstCall?.ConstructedArgument?.HeapWords ?? 0);
            int[] arguments = new int[function.Arity];
            int argumentBase = nextSlot;
            Expr.Case? foldedEntryCase = entryCallCase is not null && ReferenceEquals(clauses[0].Body, entryCallCase)
                ? FoldEntryCaseFallback(entryCallCase, clauses[0].Patterns) : null;
            Expr.Case? terminalAtomCase = entryCallCase is not null && ReferenceEquals(clauses[0].Body, entryCallCase)
                ? ReadTerminalAtomCase(entryCallCase, clauses[0].Patterns.OfType<Pattern.Variable>().Select(parameter => parameter.Name)) : null;
            bool reverseEntryArguments = caughtApply is not null || foldedEntryCase is not null || terminalAtomCase is not null
                || recursiveClosure is null && clauses.Count == 1 && HasUnconditionalParameters(clauses[0])
                    && (clauses[0].Body is Expr.Sequence { Items: [var firstDynamicItem, ..] } ? firstDynamicItem : clauses[0].Body)
                        is Expr.Call { DynamicRemote: true } dynamicCall
                    && ReadForwardedDynamicModule(dynamicCall, name => clauses[0].Patterns.OfType<Pattern.Variable>()
                        .Any(parameter => parameter.Name == name)) is not null;
            for (int offset = 0; offset < arguments.Length; offset++)
            {
                int index = reverseEntryArguments ? arguments.Length - 1 - offset : offset;
                bool needed = directConsHead is not null
                    ? index != sharedConsInput && (directUses is null
                        || directUses.Contains(((Pattern.Variable)clauses[0].Patterns[index]).Name))
                    : neededAcrossClauses?[index] ?? (usedParameters is null
                        || usedParameters.Contains(((Pattern.Variable)clauses[0].Patterns[index]).Name));
                arguments[index] = needed ? Slot() : -1;
            }

            int sharedHead = headRegister >= 0 && directConsHead is null ? Slot() : -1;
            int sharedTail = tailRegister >= 0 && (directUses is null
                || directUses.Contains(((Pattern.Variable)((Pattern.List)clauses[0].Patterns[sharedConsInput]).Tail!).Name))
                ? Slot() : -1;
            List<(int Field, string Name, int Slot)> directFields = [];
            if (directConsHead is not null)
            {
                // These slots are written from the checked tuple before the body can allocate or call.
                for (int field = 1; field < directConsHead.Items.Count; field++)
                {
                    var member = (Pattern.Variable)directConsHead.Items[field];
                    if (member.Name != VariableScopeNames.Wildcard && (directUses is null || directUses.Contains(member.Name)))
                    {
                        directFields.Add((field, member.Name, Slot()));
                    }
                }
            }

            if (entryBindings == 0)
            {
                // The entry try writes its token before evaluating any protected operands.
                bool initializesTryToken = caughtApply is not null
                    || recursiveClosure is null && clauses.Count == 1 && clauses[0].Guard is null
                        && HasUnconditionalParameters(clauses[0]) && clauses[0].Body is Expr.Try { After: null };
                EmitFrameInitialization(nextSlot - argumentBase + (initializesTryToken ? 1 : 0), frameSize);
            }
            if (sharedHead >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    headRegister,
                    YRegisterTag,
                    sharedHead
                );
            }

            if (sharedTail >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    tailRegister,
                    YRegisterTag,
                    sharedTail
                );
            }

            for (int offset = 0; offset < arguments.Length; offset++)
            {
                int index = reverseEntryArguments ? arguments.Length - 1 - offset : offset;
                if (arguments[index] >= 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        index,
                        YRegisterTag,
                        arguments[index]
                    );
                }
            }

            PrepareClosureSelf(arguments, functionInfo + 1);
            if (entryPatterns is not null && entryGuard is null && HasUnconditionalParameters(clauses[0]))
            {
                // Every rejection edge targets function_info before allocation.
                hasFailureEpilogue = false;
            }
            int clauseBase = nextSlot;
            var listFailures = new List<(int Label, int? Slot, Expr.Match Match)>();
            var tupleMatchFailures = new List<(int Label, BeamSourceLocation Location)>();
            int framedClauses = tupleFallback is null ? clauses.Count : 1;
            for (int clauseIndex = 0; clauseIndex < framedClauses; clauseIndex++)
            {
                Clause clause = clauses[clauseIndex];
                nextSlot = clauseBase;
                variables = new(StringComparer.Ordinal);
                checkedRecordBindings.Clear();
                int failure = hasFailureEpilogue || clauseIndex < framedClauses - 1 ? Label() : 0;
                for (int index = 0; index < arguments.Length; index++)
                {
                    if (index == sharedConsInput && directConsHead is not null)
                    {
                        foreach (var (field, name, slot) in directFields)
                        {
                            owner.Op(GetTupleElement, XRegisterTag, headRegister);
                            owner.Operand(BeamWire.UnsignedTag, field);
                            owner.Operand(YRegisterTag, slot);
                            variables.Add(name, slot);
                        }

                        if (sharedTail >= 0)
                        {
                            Pattern(((Pattern.List)clause.Patterns[index]).Tail!, sharedTail, failure);
                        }
                        continue;
                    }

                    if (arguments[index] < 0)
                    {
                        continue;
                    }

                    Pattern pattern = clause.Patterns[index];
                    if (index == sharedConsInput && pattern is Pattern.List { Items: [var head], Tail: { } tail })
                    {
                        Pattern(
                            head,
                            sharedHead,
                            failure,
                            checkedConsHead ? EntryShape(head) : null
                        );
                        Pattern(tail, sharedTail, failure);
                        continue;
                    }

                    Pattern? checkedShape = sharedEntryTests?[index].Shape == true ? EntryShape(pattern) : entryPatterns?[index];
                    Pattern(
                        pattern,
                        arguments[index],
                        failure,
                        checkedShape,
                        sharedEntryTests?[index].Tag ?? true,
                        sharedEntryTests?[index].Arity ?? true,
                        entryPatternScratch: recursiveClosure is null,
                        checkedTupleLiterals: entryTupleLiterals?[index]
                    );
                    RememberCheckedRecord(pattern);
                }
                if (recursiveClosure?.SelfName is { } self)
                {
                    variables.TryAdd(self, selfSlot);
                }
                Guard(entryPatterns is null ? clause.Guard : entryGuard, failure);
                Expr body = clause.Body;
                if (entryBindings != 0 && body is Expr.Sequence entrySequence)
                {
                    foreach (Expr.Match match in entrySequence.Items.Take(entryBindings).Cast<Expr.Match>())
                    {
                        string binding = ((Pattern.Variable)match.Pattern).Name;
                        bool survives = entrySurvivors is null || entrySurvivors.Contains(binding);
                        int destinationTag = survives ? YRegisterTag : XRegisterTag;
                        int destination = survives ? Slot() : nextEntryRegister++;
                        if (AttributeLiteral.TryNormalizeConstant(match.Value, out Term constant))
                        {
                            owner.EmitConstant(constant, destination, destinationTag);
                        }
                        else
                        {
                            var call = (Expr.Call)match.Value;
                            if (call.Arguments is [var key])
                            {
                                owner.Op(BifOne, LabelTag, 0);
                                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 1));
                                if (key is Expr.Variable variable && entryRegisters.TryGetValue(variable.Name, out int register))
                                {
                                    owner.Operand(XRegisterTag, register);
                                }
                                else
                                {
                                    GuardOperand(key, key is Expr.Variable saved ? variables[saved.Name] : null);
                                }
                            }
                            else
                            {
                                owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 0));
                            }

                            owner.Operand(destinationTag, destination);
                        }

                        if (survives)
                        {
                            variables.Add(binding, destination);
                        }
                        else
                        {
                            entryRegisters.Add(binding, destination);
                        }
                    }

                    EmitFrameInitialization(nextSlot - argumentBase, frameSize);
                    body = new Expr.Sequence(entrySequence.Items.Skip(entryBindings).ToArray());
                }

                if (entryCallCase is not null)
                {
                    if (entryCasePrefix is { } prefix)
                    {
                        EmitEntryCall(prefix);
                        EmitEntryCall(firstCall!, CallLocation(prefix.Call, location));
                    }
                    else
                    {
                        EmitEntryCall(firstCall!);
                    }

                    if (entryCaseArithmetic is { } arithmetic)
                    {
                        EmitEntryCaseArithmetic(arithmetic, firstCall!.Call);
                    }

                    if (body is Expr.Sequence sequence)
                    {
                        EmitWithContinuation(
                            sequence.Items[0],
                            sequence.Items.Skip(1),
                            null,
                            returnValue: false,
                            reuseCallSlots: true,
                            caseInputPrepared: true
                        );
                        if (EmitDecoderSequence(
                            sequence,
                            listFailures,
                            start: 1,
                            reuseCallSlots: true
                        ) is int sequenceFrame)
                        {
                            owner.EmitFrameReturn(sequenceFrame);
                        }
                    }
                    else if ((foldedEntryCase is not null
                            || !(entryCaseArithmetic is null && TryEmitTerminalIdentityCase(entryCallCase, firstCall!.Call))
                                && !TryEmitTerminalTaggedCase(entryCallCase))
                        && (terminalAtomCase is null || !TryCaseSelection(
                        terminalAtomCase,
                        input: null,
                        returnResult: true,
                        reuseBranchSlots: true,
                        branchContinuation: null,
                        deferredBooleanFailures: null,
                        reservedBooleanLabels: null,
                        allowUnmatchedAtoms: true,
                        terminalAtomTails: true
                    )))
                    {
                        EmitCase(
                            foldedEntryCase ?? entryCallCase,
                            null,
                            inputPrepared: true,
                            reuseBranchSlots: true,
                            returnResult: foldedEntryCase is not null,
                            tailFallback: foldedEntryCase is not null
                        );
                        if (foldedEntryCase is null)
                        {
                            owner.EmitFrameReturn(frameSize);
                        }
                    }
                }
                else if (firstCall is not null && afterFirstCall is not null)
                {
                    EmitEntryCall(firstCall);
                    body = afterFirstCall;
                    if (firstTupleResult is { } tuple)
                    {
                        EmitEntryTupleBinding(
                            tuple.Match,
                            tuple.Pattern,
                            tuple.KnownShape,
                            tuple.ForwardedField,
                            CallLocation(firstCall.Call, location)
                        );
                    }

                    if (saveFirstResult)
                    {
                        if (recursiveClosure is null
                            && TryEmitFirstResultTupleBinding(firstResult!, afterFirstCall) is { } remainder)
                        {
                            body = remainder;
                        }
                        else
                        {
                            variables.Add(firstResult!.Name, Save());
                        }
                    }
                    if (forwardedCall is not null)
                    {
                        if (EmitForwardedCall(forwardedCall, CallLocation(firstCall.Call, location)))
                        {
                            return;
                        }

                        body = forwardedCall.Continuation!;
                    }
                }

                // A clause body runs only after its patterns and guard have succeeded.
                if (entryCallCase is null && body is Expr.Receive { Clauses.Count: > 0 } receive)
                {
                    EmitReceive(receive, terminal: true);
                }
                else if (entryCallCase is null && !TryEmitDiscardedDynamicReturn(body)
                    && !(recursiveClosure is null && firstCall is null && entryBindings == 0
                        && TryEmitFramedTupleMatchTail(body, tupleMatchFailures))
                    && !TailLocal(body, reuseCallSlots: true))
                {
                    int? returnFrame = frameSize;
                    if (clauses.Count == 1 && clause.Guard is null && HasUnconditionalParameters(clause)
                        && body is Expr.Sequence sequence)
                    {
                        returnFrame = EmitDecoderSequence(sequence, listFailures, reuseCallSlots: true);
                    }
                    else if (caughtApply is not null)
                    {
                        EmitEntryCaughtApply(caughtApply);

                        return;
                    }
                    else
                    {
                        EmitReturnExpression(body, reuseCallSlots: true, terminalCase: true);
                    }
                    if (returnFrame is int remainingFrame)
                    {
                        owner.EmitFrameReturn(remainingFrame);
                    }
                }
                if (failure != 0)
                {
                    Mark(failure);
                }
            }

            checkedRecordBindings.Clear();
            if (!hasFailureEpilogue)
            {
                if (tupleFallback is { } fallback)
                {
                    Mark(tupleFallbackLabel);
                    owner.EmitConstant(fallback.Value, 0);
                    owner.instructions.WriteByte(Return);
                }

                if (capturedEntryFailure != 0)
                {
                    Mark(capturedEntryFailure);
                    owner.Op(TailLocalCall, BeamWire.UnsignedTag, function.Arity - recursiveClosure!.CaptureCount);
                    guardFailureOperands?.Add(recursiveClosure.GuardFailure!, checked((int)owner.instructions.Position));
                    owner.Operand(LabelTag, 0);
                }

                EmitDecoderFailures(listFailures);
                EmitRegisterTupleMatchFailures(tupleMatchFailures);

                return;
            }

            int failureArity = recursiveClosure is { GuardFailure: not null } guarded
                ? guarded.Arity - guarded.CaptureCount
                : arguments.Length;
            for (int index = 0; index < failureArity; index++)
            {
                Load(arguments[index], index);
            }
            if (recursiveClosure?.GuardFailure is { } failureStub)
            {
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, failureArity);
                guardFailureOperands?.Add(failureStub, checked((int)owner.instructions.Position));
                owner.Operand(LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, frameSize);
            }
            else
            {
                owner.Op(Deallocate, BeamWire.UnsignedTag, frameSize);
                JumpTo(functionInfo);
            }
            EmitDecoderFailures(listFailures);
            EmitRegisterTupleMatchFailures(tupleMatchFailures);

            if (listFallback is { } empty)
            {
                EmitEmptyListReturn(empty, listFallbackLabel, functionInfo);
            }
        }

        private int CountSafeEntryBindings(Clause clause)
        {
            if (clause.Body is not Expr.Sequence sequence)
            {
                return 0;
            }

            var bound = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name)
                .ToHashSet(StringComparer.Ordinal);
            int count = 0;
            foreach (Expr expression in sequence.Items.Take(sequence.Items.Count - 1))
            {
                if (expression is not Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable } match
                    || bound.Contains(variable.Name))
                {
                    break;
                }

                if (!AttributeLiteral.TryNormalizeConstant(match.Value, out _)
                    && !(match.Value is Expr.Call { DynamicRemote: false, Module: null or ErlangModuleNames.Module } call
                        && owner.ResolvesErlangBif(call)
                        && (call is { Function: AutoImportNames.Self or AutoImportNames.Node, Arguments: [] }
                            || call is { Function: AutoImportNames.Get, Arguments: [var key] }
                            && (AttributeLiteral.TryNormalizeConstant(key, out _)
                                || key is Expr.Variable input && bound.Contains(input.Name)))))
                {
                    break;
                }

                bound.Add(variable.Name);
                count++;
            }

            return count;
        }

        private static (int Argument, Pattern.Tuple Pattern, Term Value)? ReadEntryTupleFallback(
            IReadOnlyList<Clause> clauses, (bool Shape, bool Arity, bool Tag)[] sharedTests)
        {
            if (clauses is not [{ Guard: null } first, { Guard: null, Body: Expr.Literal result } second])
            {
                return null;
            }

            int argument = -1;
            Pattern.Tuple? tuple = null;
            for (int index = 0; index < first.Patterns.Count; index++)
            {
                if (sharedTests[index].Shape && first.Patterns[index] is Pattern.Literal { Value: Atom or Integer })
                {
                    continue;
                }

                if (second.Patterns[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return null;
                }

                if (argument < 0 && first.Patterns[index] is Pattern.Tuple candidate
                    && candidate.Items.Skip(HasTupleTag(candidate, out _) ? 1 : 0).All(item => item is Pattern.Variable))
                {
                    argument = index;
                    tuple = candidate;
                }
                else if (first.Patterns[index] is not Pattern.Variable { })
                {
                    return null;
                }
            }

            var bindings = first.Patterns.SelectMany(Semantics.Variables).ToArray();

            return tuple is not null && bindings.Distinct(StringComparer.Ordinal).Count() == bindings.Length
                ? (argument, tuple, result.Value) : null;
        }

        private static bool[]? ReadNeededEntryArguments(IReadOnlyList<Clause> clauses)
        {
            var needed = new bool[clauses[0].Patterns.Count];
            foreach (Clause clause in clauses)
            {
                var counts = clause.Patterns.SelectMany(Semantics.Variables)
                    .GroupBy(name => name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
                HashSet<string> referenced;
                try
                {
                    referenced = ClosureCaptures.ReadEntryReferences(clause, counts.Keys);
                }
                catch (NotSupportedException)
                {
                    return null;
                }

                for (int index = 0; index < needed.Length; index++)
                {
                    if (clause.Patterns[index] is not Pattern.Variable variable)
                    {
                        needed[index] = true;
                    }
                    else if (variable.Name != VariableScopeNames.Wildcard
                        && (counts.GetValueOrDefault(variable.Name) > 1 || referenced.Contains(variable.Name)))
                    {
                        needed[index] = true;
                    }
                }
            }

            return needed;
        }

        private static HashSet<string>? ReadUsedEntryParameters(Clause clause)
        {
            var parameters = clause.Patterns.Cast<Pattern.Variable>()
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard)
                .ToDictionary(parameter => parameter.Name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            try
            {
                var body = new Expr.Fun([new Clause([], null, clause.Body)]);

                return ClosureCaptures.Capture(body, parameters).Keys.ToHashSet(StringComparer.Ordinal);
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private bool TryEmitRegisterListClause(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items.Count: >= 2, Tail: null } list],
                    Body: Expr.Sequence body
                }] || list.Items.Count > FirstReservedXRegister - 2
                || list.Items.Any(item => item is not Pattern.Variable { Name: not VariableScopeNames.Wildcard }))
            {
                return false;
            }

            var parameters = list.Items.Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings(parameters))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            for (int index = 0; index < parameters.Length; index++)
            {
                int source = index == 0 ? 0 : 2;
                int destination = index == 0 ? 1 : index + 2;
                owner.EmitNonemptyList(source, failure);
                owner.EmitListParts(
                    source,
                    XRegisterTag,
                    destination,
                    XRegisterTag,
                    2
                );
            }
            owner.EmitNilTest(XRegisterTag, 2, failure);

            decoderSlotCount = frameSize;
            foreach (var parameter in parameters)
            {
                variables.Add(parameter.Name, Slot());
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, frameSize);
            owner.Operand(BeamWire.UnsignedTag, parameters.Length + 2);
            int uninitialized = frameSize - parameters.Length;
            if (uninitialized > 0)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, uninitialized);
                for (int index = 0; index < uninitialized; index++)
                {
                    owner.Operand(YRegisterTag, index);
                }
            }

            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                int source = index == 0 ? 1 : index + 2;
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    YRegisterTag,
                    variables[parameters[index].Name]
                );
            }

            if (!TryEmitLinearBindings(body, parameters))
            {
                Expression(body);
            }
            owner.EmitFrameReturn(frameSize);

            return true;
        }

        private sealed record OptionalDecode(
            Integer Tag, Atom Absent, DecodeCall Call);
        private sealed record DecodeCall(
            Expr.Call Decode,
            Expr.Call? Convert,
            Term Tags,
            int Entry
        );

        private bool TryEmitOptionalDecode(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable input, Value: Expr.Call
                            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] } read
                        },
                    .., var ignoredTail,
                    Expr.Match
                        {
                            Pattern: Pattern.Variable result, Value: Expr.Tuple
                            { Items: [Expr.Literal { Value: Atom record }, ..] } tuple
                        },
                    Expr.Variable returned
                ]
                    } sequence
                }]
                || !DistinctBindings(
                    first,
                    second,
                    input,
                    result
                )
                || left.Name != first.Name || right.Name != second.Name || returned.Name != result.Name
                || tuple.Items.Count < 2 || sequence.Items.Count < tuple.Items.Count + 3 || sequence.Items.Count > tuple.Items.Count + 4
                || !entries.TryGetValue((read.Function, 2), out int reader))
            {
                return false;
            }

            var bound = new HashSet<string>(StringComparer.Ordinal) { first.Name, second.Name, input.Name, result.Name };
            var fields = new List<OptionalDecode>();
            bool hasRequired = sequence.Items.Count == tuple.Items.Count + 4;
            bool leadingRequired = hasRequired && tuple.Items.Count == 4
                && sequence.Items[1] is Expr.Match { Pattern: Pattern.List { } };
            int requiredIndex = leadingRequired ? 1 : tuple.Items.Count - 2;
            DecodeCall? required = null;
            Expr.Match? requiredMatch = null;
            int offset = 0;
            string current = input.Name;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (hasRequired && index == requiredIndex)
                {
                    if (sequence.Items[index] is not Expr.Match
                        {
                            Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                            Value: Expr.Variable list
                        } match
                        || sequence.Items[index + 1] is not Expr.Match { Pattern: Pattern.Variable value, Value: var body }
                        || list.Name != current || tuple.Items[index] is not Expr.Variable returnedValue
                        || returnedValue.Name != value.Name || !DistinctBindings(head, rest, value)
                        || !bound.Add(head.Name) || !bound.Add(rest.Name) || !bound.Add(value.Name)
                        || ReadDecodeCall(body, head.Name) is not { } decoder)
                    {
                        return false;
                    }

                    required = decoder;
                    requiredMatch = match;
                    current = rest.Name;
                    offset = 1;
                    continue;
                }

                if (sequence.Items[index + offset] is not Expr.Match
                    { Pattern: Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable tail] }, Value: Expr.Case decision }
                    || tuple.Items[index] is not Expr.Variable member || member.Name != field.Name
                    || !DistinctBindings(field, tail) || !bound.Add(field.Name) || !bound.Add(tail.Name)
                    || ReadOptionalDecode(decision, current, bound) is not { } optional)
                {
                    return false;
                }

                fields.Add(optional);
                current = tail.Name;
            }

            if (hasRequired != (required is not null) || !IsIgnoredListTail(ignoredTail, current))
            {
                return false;
            }

            int count = tuple.Items.Count - 1;
            int slots = count == 1 ? 0 : count;
            int failure = hasRequired ? owner.nextLabel + fields.Count * 2 : 0;
            EmitGeneralFrame(2, slots: slots);
            owner.EmitCallLine(read, location);
            owner.EmitFieldCall(2, reader);
            var previous = CallLocation(read, location);
            if (leadingRequired && required is { } leadingCall)
            {
                EmitRequiredDecode(
                    leadingCall,
                    failure,
                    tailSlot: 2,
                    resultSlot: 1,
                    previous
                );
            }

            for (int index = 0; index < fields.Count; index++)
            {
                bool last = index == fields.Count - 1;
                if (last && !leadingRequired && required is { } requiredCall)
                {
                    EmitRequiredDecode(
                        requiredCall,
                        failure,
                        tailSlot: 1,
                        resultSlot: 0,
                        previous
                    );
                }

                int slot = last ? 0 : count - index - 1;
                EmitOptionalDecode(
                    fields[index],
                    slot,
                    leadingRequired ? 0 : slot - 1,
                    index == 0 && !leadingRequired,
                    index == fields.Count - 2,
                    last && hasRequired && !leadingRequired ? 1 : null,
                    previous
                );
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(record);
            for (int index = slots - 1; index > 0; index--)
            {
                int resultSlot = leadingRequired ? slots - index : hasRequired && index == 1 ? 0 : index;
                owner.Operand(YRegisterTag, resultSlot);
            }
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots);
            if (requiredMatch is { } matchFailure)
            {
                Mark(Label());
                var failureLocation = location;
                owner.EmitArithmeticLine(matchFailure.SourceLine, ref failureLocation);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;
        }

        private OptionalDecode? ReadOptionalDecode(Expr.Case decision, string current, HashSet<string> bound)
        {
            if (decision is not
                {
                    Value: Expr.Variable selector, Clauses: [
                    {
                        Guard: null, Patterns: [Pattern.List
                        {
                            Items: [Pattern.Tuple
                            { Items: [Pattern.Literal { Value: Integer tag }, Pattern.Variable bytes] }],
                            Tail: Pattern.Variable rest
                        }],
                        Body: Expr.Tuple { Items: [var body, Expr.Variable remaining] }
                    },
                    {
                        Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom absent }, Expr.Variable unchanged] }
                    }]
                }
                || selector.Name != current || unchanged.Name != current
                || !DistinctBindings(bytes, rest) || !bound.Add(bytes.Name) || !bound.Add(rest.Name)
                || remaining.Name != rest.Name || ReadDecodeCall(body, bytes.Name) is not { } call)
            {
                return null;
            }

            return new(tag, absent, call);
        }

        private DecodeCall? ReadDecodeCall(Expr body, string payload)
        {
            if (body is Expr.Block block)
            {
                body = block.Body;
            }
            var convert = ReadMappingConversion(body);
            if (convert is { Module: null } && entries.ContainsKey((convert.Function, 1)))
            {
                return null;
            }

            var candidate = convert is null ? body : convert.Arguments[0];
            if (candidate is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable bytes, var tags]
                } decode
                || bytes.Name != payload || !entries.TryGetValue((decode.Function, 2), out int entry)
                || !AttributeLiteral.TryNormalizeConstant(tags, out Term decoderTags))
            {
                return null;
            }

            return new(
                decode,
                convert,
                decoderTags,
                entry
            );
        }

        private void EmitDecodeCall(DecodeCall call, BeamSourceLocation previous)
        {
            owner.EmitConstant(call.Tags, 1);
            EmitDecodeInvocation(call, previous);
        }

        private void EmitDecodeInvocation(DecodeCall call, BeamSourceLocation previous)
        {
            owner.EmitCallLine(call.Decode, previous);
            owner.EmitFieldCall(2, call.Entry);
            if (call.Convert is { } convert)
            {
                owner.EmitCallLine(convert, CallLocation(call.Decode, previous));
                Call(convert.Module, convert.Function, 1);
            }
        }

        private void EmitRequiredDecode(
            DecodeCall call,
            int failure,
            int tailSlot,
            int resultSlot,
            BeamSourceLocation previous
        )
        {
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                tailSlot
            );
            EmitDecodeCall(call, previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                resultSlot
            );
        }

        private void EmitOptionalDecode(
            OptionalDecode field,
            int slot,
            int tailSlot,
            bool first,
            bool forwardInX,
            int? finalTailSlot,
            BeamSourceLocation previous
        )
        {
            int missing = Label();
            int done = Label();
            bool last = slot == 0;
            int sourceTag = (first || last) && finalTailSlot is null ? XRegisterTag : YRegisterTag;
            int source = sourceTag == XRegisterTag ? 0 : finalTailSlot ?? slot;
            if (first && !last)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
            }

            owner.Op(IsNonemptyList, LabelTag, missing);
            owner.Operand(sourceTag, source);
            owner.Op(last ? GetHead : GetList, sourceTag, source);
            owner.Operand(XRegisterTag, 0);
            if (!last)
            {
                owner.Operand(YRegisterTag, tailSlot);
            }
            owner.EmitTupleCheck(0, 2, missing);
            owner.EmitTupleElement(0, 1);
            owner.Op(ExactEqualTest, LabelTag, missing);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(field.Tag);
            owner.EmitTupleElement(1, 0);
            owner.EmitConstant(field.Call.Tags, 1);
            if (!last || finalTailSlot.HasValue)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(YRegisterTag, finalTailSlot ?? slot);
            }

            EmitDecodeInvocation(field.Call, previous);
            if (!last)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
                if (forwardInX)
                {
                    Load(tailSlot, 0);
                }
            }
            JumpTo(done);

            Mark(missing);
            if (!last)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    forwardInX ? XRegisterTag : YRegisterTag,
                    forwardInX ? 0 : tailSlot
                );
            }
            owner.EmitConstant(field.Absent, last ? 0 : slot, last ? XRegisterTag : YRegisterTag);
            Mark(done);
        }

        private bool TryEmitSingletonListForwarding(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: null }],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable item] } recur
                },
                { Guard: Expr.Call guard } identity,
                {
                    Guard: null, Patterns: [Pattern.Variable fallback],
                    Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable value] } finish
                }]
                || !DistinctBindings(head) || !DistinctBindings(fallback)
                || item.Name != head.Name || value.Name != fallback.Name
                || recur.Function != function.Name || !entries.TryGetValue((function.Name, 1), out int entry)
                || ReadIdentityGuard(identity) is not byte test
                || guard.Module is null && entries.ContainsKey((guard.Function, 1)))
            {
                return false;
            }

            int checkIdentity = Label();
            int callFallback = Label();
            owner.EmitNonemptyList(0, checkIdentity);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitNilTest(XRegisterTag, 2, checkIdentity);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);

            Mark(checkIdentity);
            owner.Op(test, LabelTag, callFallback);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            Mark(callFallback);
            owner.EmitCallLine(finish, location);
            owner.EmitTailRemote(finish.Module, finish.Function, framed: false);

            return true;
        }

        private bool HasByteOctetAccumulator(FunctionDefinition function, OctetStep step) =>
            HasOnlyLocalCalls(
                function,
                2,
                (_, call) => ReferenceEquals(call, step.Recurse)
                || AttributeLiteral.TryNormalizeConstant(call.Arguments[1], out Term constant) && constant is Nil
            );

        private bool HasOctetSign(FunctionDefinition function, OctetStep step, bool negative) =>
            HasOnlyLocalCalls(
                function,
                2,
                (caller, call) => ReferenceEquals(call, step.Recurse)
                || IsSignDispatch(caller, call, negative) && HasOnlyLocalCalls(caller, 1, IsGuardedIntegerCall)
            );

        private int ReadOctetNumberType(FunctionDefinition function, bool negative)
        {
            PairFacts sign = negative ? PairFacts.NonpositiveNumber : PairFacts.NonnegativeNumber;

            PairFacts facts = ReadJoinedNumericArgument(function, 0);
            if ((facts & sign) != 0)
            {
                return NumberTypeBits;
            }

            return !negative && (facts & PairFacts.NonnegativeIfNumeric) != 0 ? AnyTypeBits : 0;
        }

        private static bool IsSignDispatch(FunctionDefinition function, Expr.Call target, bool negative)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [Pattern.Variable input], Body: var body }]
                || input.Name == VariableScopeNames.Wildcard
                || target.Arguments[0] is not Expr.Variable passed || passed.Name != input.Name)
            {
                return false;
            }

            if (body is Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, ..] }
                && binding.Name != input.Name)
            {
                body = value;
            }

            return body is Expr.If
            {
                Clauses: [
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.GreaterOrEqual, Left: Expr.Variable tested,
                        Right: Expr.Literal { Value: Integer zero }
                    }, Body: var positive
                },
                { Guard: Expr.Literal { Value: Atom { Name: "true" } }, Body: var negativeBranch }]
            }
                && tested.Name == input.Name && zero.Value == 0
                && ReferenceEquals(target, negative ? negativeBranch : positive);
        }

        private bool IsGuardedIntegerCall(FunctionDefinition caller, Expr.Call target) =>
            target.Arguments is [Expr.Variable value]
            && caller.Clauses.Any(clause => clause.Guard is Expr.Call
            {
                Function: "is_integer", Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                Arguments: [Expr.Variable tested]
            } guard
                && (guard.Module is not null || !entries.ContainsKey(("is_integer", 1)))
                && tested.Name == value.Name
                && (ReferenceEquals(clause.Body, target)
                    || clause.Body is Expr.Call { DynamicRemote: false } outer
                        && outer.Arguments.Any(argument => ReferenceEquals(argument, target))));

        private bool HasOnlyLocalCalls(
            FunctionDefinition function, int arity, Func<FunctionDefinition, Expr.Call, bool> accepts
        )
        {
            if (owner.exportedFunctions.Contains((function.Name, arity)))
            {
                return false;
            }

            bool seeded = false;
            foreach (var caller in localFunctions.Values)
            {
                foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(caller.Clauses).SelectMany(LocalFunctionEntries.Walk))
                {
                    if (expression is Expr.FunctionReference reference && reference.Arity == arity && reference.Name == function.Name)
                    {
                        return false;
                    }
                    if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                        || call.Function != function.Name || call.Arguments.Count != arity)
                    {
                        continue;
                    }
                    if (!accepts(caller, call))
                    {
                        return false;
                    }
                    seeded = true;
                }
            }

            return seeded;
        }

        private bool HasNaturalBitPartTotal(FunctionDefinition function, BitPartCollection flow)
        {
            if (owner.exportedFunctions.Contains((function.Name, 3)))
            {
                return false;
            }

            bool hasCaller = false;
            foreach (var caller in localFunctions.Values)
            {
                var collection = ReadPartCollection(caller);
                foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(caller.Clauses).SelectMany(LocalFunctionEntries.Walk))
                {
                    if (expression is Expr.FunctionReference { Arity: 3 } reference && reference.Name == function.Name)
                    {
                        return false;
                    }
                    if (expression is not Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 3 } call
                        || call.Function != function.Name || ReferenceEquals(call, flow.Recur))
                    {
                        continue;
                    }
                    if (!ReferenceEquals(call, collection?.Bits))
                    {
                        return false;
                    }
                    hasCaller = true;
                }
            }

            return hasCaller;
        }

        private bool TryEmitListDecoderEntry(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Sequence sequence
                }]
                || sequence.Items is not [
                    Expr.Match
                {
                    Pattern: Pattern.Variable list, Value: Expr.Call
                    { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] } read
                },
                    Expr.Match
                {
                    Pattern: Pattern.List { Items: [Pattern.Variable { }], Tail: Pattern.Variable { } },
                    Value: Expr.Variable input
                }, _, _, ..]
                || !DistinctBindings(first, second, list) || left.Name != first.Name || right.Name != second.Name
                || input.Name != list.Name || !entries.ContainsKey((read.Function, 2)))
            {
                return false;
            }

            var suffix = sequence.Items.Skip(1).SelectMany(LocalFunctionEntries.Walk).ToArray();
            if (UsesDecoderBinding(suffix, first.Name) || UsesDecoderBinding(suffix, second.Name))
            {
                return false;
            }

            if (TryEmitTerminalRequiredFields(
                sequence.Items,
                first,
                second,
                list,
                read
            )
                || TryEmitRequiredAndTrailingOptional(
                    sequence.Items,
                    first,
                    second,
                    list,
                    read
                ))
            {
                return true;
            }

            decoderSlotCount = ReadDecoderSlotCount(sequence.Items);
            int lastLabel = owner.nextLabel + plannedLabels - 3;
            if (decoderSlotCount != 0 && plannedListFailures != 0)
            {
                // The failure blocks are emitted after all inline choices, in reverse match order.
                decoderListLabel = lastLabel;
                decoderRangeLabel = lastLabel - plannedListFailures;
            }
            peakSlot = Math.Max(peakSlot, decoderSlotCount);
            EmitGeneralFrame(function.Arity);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            int start = 1;
            if (TryEmitListDecodeStep(
                sequence.Items,
                start,
                failures,
                out int? returnFrame,
                list.Name
            ))
            {
                start += 2;
            }
            else
            {
                variables.Add(list.Name, Save());
            }
            if ((returnFrame ?? EmitDecoderSequence(sequence, failures, start)) is int remainingFrame)
            {
                owner.EmitFrameReturn(remainingFrame);
            }
            EmitDecoderFailures(failures);
            if (decoderListLabel.HasValue)
            {
                owner.nextLabel = lastLabel + 1;
            }

            return true;
        }

        private bool TryEmitTerminalRequiredFields(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (items is not [_, .., var ignored,
                Expr.Match
                {
                    Pattern: Pattern.Variable result, Value: Expr.Tuple
                    { Items: [Expr.Literal { Value: Atom record }, ..] } tuple
                },
                Expr.Variable returned]
                || tuple.Items.Count is not (3 or 4) || items.Count != tuple.Items.Count * 2 + 2
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    result
                ) || returned.Name != result.Name)
            {
                return false;
            }

            int slots = tuple.Items.Count - 1;
            var bound = new HashSet<string>(StringComparer.Ordinal) { first.Name, second.Name, list.Name, result.Name };
            var fields = new List<(Expr.Match Split, DecodeCall Call)>();
            TaggedBranchRange? range = null;
            DecodedEnum? enumeration = null;
            string current = list.Name;
            foreach (var member in tuple.Items.Skip(1))
            {
                int index = 1 + fields.Count * 2;
                if (items[index] is not Expr.Match
                    {
                        Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                        Value: Expr.Variable source
                    } split
                    || items[index + 1] is not Expr.Match { Pattern: Pattern.Variable value, Value: var body }
                    || !DistinctBindings(head, rest, value) || !bound.Add(head.Name) || !bound.Add(rest.Name)
                    || !bound.Add(value.Name) || source.Name != current
                    || member is not Expr.Variable returnedField || returnedField.Name != value.Name)
                {
                    return false;
                }

                if (index == 1 && body is Expr.Block
                    {
                        Body: Expr.Sequence
                        {
                            Items: [
                        Expr.Match { Pattern: Pattern.Variable number, Value: var input }, Expr.If test]
                        }
                    })
                {
                    if (!DistinctBindings(number) || !bound.Add(number.Name)
                        || ReadTaggedBranchRange(test, number.Name) is not { } checkedRange)
                    {
                        return false;
                    }

                    range = checkedRange;
                    body = input;
                }
                else if (index == 1 && slots == 2 && body is Expr.Case)
                {
                    if (ReadDecodedEnum(body) is not { } decoded || !bound.Add(decoded.Fallback))
                    {
                        return false;
                    }

                    enumeration = decoded;
                    body = decoded.Call;
                }
                if (ReadDecodeCall(body, head.Name) is not { } call || index == 1 && range is not null && call.Convert is not null)
                {
                    return false;
                }
                fields.Add((split, call));
                current = rest.Name;
            }
            if ((slots == 3 && range is null) || !IsIgnoredListTail(ignored, current))
            {
                return false;
            }

            int rangeFailure = owner.nextLabel;
            int firstFailure = rangeFailure + slots - 1 + (range is null ? 0 : 1)
                + (enumeration is null ? 0 : enumeration.Alternatives.Count + 2);
            if (range is not null)
            {
                Label();
            }

            EmitGeneralFrame(2, slots: slots);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            var previous = CallLocation(read, location);
            for (int index = 0; index < fields.Count; index++)
            {
                var (split, call) = fields[index];
                int? source = index == 0 ? null : index == 1 ? slots - 1 : 0;
                int sourceTag = source.HasValue ? YRegisterTag : XRegisterTag;
                int failure = firstFailure - index;
                bool last = index == slots - 1;
                failures.Add((failure, source, split));
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(sourceTag, source.GetValueOrDefault());
                owner.Op(last ? GetHead : GetList, sourceTag, source.GetValueOrDefault());
                owner.Operand(XRegisterTag, 0);
                if (!last)
                {
                    owner.Operand(YRegisterTag, index == 0 ? slots - 1 : 0);
                }
                owner.EmitConstant(call.Tags, 1);
                if (last)
                {
                    if (slots == 2)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            0,
                            YRegisterTag,
                            1
                        );
                    }
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, slots - 1);
                }
                else if (source is int dead)
                {
                    owner.InitStackSlot(dead);
                }

                EmitDecodeInvocation(call, previous);
                if (!last)
                {
                    int resultSlot = index == 0 ? slots - 2 : slots - 1;
                    if (index == 0 && enumeration is not null)
                    {
                        EmitDecodedEnum(enumeration, resultSlot);
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            resultSlot
                        );
                    }
                    if (index == 0 && range is not null)
                    {
                        EmitRangeTests(call.Decode, range, rangeFailure);
                    }
                }
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(record);
            for (int slot = 0; slot < slots - 1; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots - 1);
            owner.nextLabel = firstFailure + 1;
            if (range is not null)
            {
                Mark(rangeFailure);
                var callLocation = CallLocation(fields[0].Call.Decode, previous);
                EmitRangeExit(range, CallLocation(range.Exit, callLocation), slots);
            }
            EmitDecoderFailures(failures);

            return true;
        }

        private bool TryEmitRequiredAndTrailingOptional(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (items is not [_, ..,
                var ignored,
                Expr.Match
                {
                    Pattern: Pattern.Variable result, Value: Expr.Tuple
                    { Items: [Expr.Literal { Value: Atom record }, ..] } tuple
                },
                Expr.Variable returned]
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    result
                ) || returned.Name != result.Name)
            {
                return false;
            }

            int requiredCount = items.Count - tuple.Items.Count - 3;
            int optionalCount = tuple.Items.Count - 1 - requiredCount;
            if ((requiredCount, optionalCount) is not ((2, 1) or (3, >= 1 and <= 3)))
            {
                return false;
            }

            var bound = new HashSet<string>(StringComparer.Ordinal)
                { first.Name, second.Name, list.Name, result.Name };
            var fields = new List<(Expr.Match Split, DecodeCall Call)>();
            bool hasRange = requiredCount == 2;
            TaggedBranchRange? range = null;
            DecodedEnum? enumeration = null;
            string current = list.Name;
            foreach (var member in tuple.Items.Skip(1).Take(requiredCount))
            {
                int index = 1 + fields.Count * 2;
                if (items[index] is not Expr.Match
                    {
                        Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                        Value: Expr.Variable source
                    } split
                    || items[index + 1] is not Expr.Match { Pattern: Pattern.Variable value, Value: var body }
                    || !DistinctBindings(head, rest, value) || !bound.Add(head.Name) || !bound.Add(rest.Name)
                    || !bound.Add(value.Name) || source.Name != current
                    || member is not Expr.Variable returnedField || returnedField.Name != value.Name)
                {
                    return false;
                }

                if (hasRange && index == 1)
                {
                    if (body is not Expr.Block
                        {
                            Body: Expr.Sequence
                            {
                                Items: [
                            Expr.Match { Pattern: Pattern.Variable number, Value: var input }, Expr.If test]
                            }
                        }
                        || !DistinctBindings(number) || !bound.Add(number.Name)
                        || ReadTaggedBranchRange(test, number.Name) is not { } checkedRange)
                    {
                        return false;
                    }

                    range = checkedRange;
                    body = input;
                }
                else if (index == 1 && body is Expr.Case)
                {
                    if (ReadDecodedEnum(body) is not { } decoded || !bound.Add(decoded.Fallback))
                    {
                        return false;
                    }

                    enumeration = decoded;
                    body = decoded.Call;
                }
                if (ReadDecodeCall(body, head.Name) is not { } call || index == 1 && hasRange && call.Convert is not null)
                {
                    return false;
                }
                fields.Add((split, call));
                current = rest.Name;
            }
            var optionalFields = new List<OptionalDecode>();
            for (int index = 0; index < optionalCount; index++)
            {
                if (items[1 + requiredCount * 2 + index] is not Expr.Match
                    { Pattern: Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable tail] }, Value: Expr.Case decision }
                    || tuple.Items[1 + requiredCount + index] is not Expr.Variable member || member.Name != field.Name
                    || !DistinctBindings(field, tail) || !bound.Add(field.Name) || !bound.Add(tail.Name)
                    || ReadOptionalDecode(decision, current, bound) is not { } optional)
                {
                    return false;
                }

                optionalFields.Add(optional);
                current = tail.Name;
            }
            if (!IsIgnoredListTail(ignored, current))
            {
                return false;
            }

            int slots = tuple.Items.Count - 1;
            EmitGeneralFrame(2, slots: slots);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            int firstFailure = owner.nextLabel + optionalCount * 2 + requiredCount - 1 + (hasRange ? 1 : 0)
                + (enumeration is null ? 0 : enumeration.Alternatives.Count + 2);
            var previous = CallLocation(read, location);
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            int rangeFailure = firstFailure - fields.Count;
            (int? Source, int Tail, int Result)[] registers = hasRange
                ? [(null, 2, 1), (2, 0, 2)]
                : [(null, slots - 1, slots - 2),
                   (slots - 1, slots - 3, slots - 1),
                   (slots - 3, slots - 4, slots - 3)];
            for (int index = 0; index < fields.Count; index++)
            {
                var (split, call) = fields[index];
                var (source, rest, resultSlot) = registers[index];
                int failure = firstFailure - index;
                failures.Add((failure, source, split));
                int sourceTag = source.HasValue ? YRegisterTag : XRegisterTag;
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(sourceTag, source.GetValueOrDefault());
                owner.Op(GetList, sourceTag, source.GetValueOrDefault());
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, rest);
                owner.EmitConstant(call.Tags, 1);
                if (source is int dead)
                {
                    owner.InitStackSlot(dead);
                }
                EmitDecodeInvocation(call, previous);
                if (index == 0 && enumeration is not null)
                {
                    EmitDecodedEnum(enumeration, resultSlot);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        resultSlot
                    );
                }
                if (index == 0 && range is not null)
                {
                    EmitRangeTests(call.Decode, range, rangeFailure);
                }
            }

            for (int index = 0; index < optionalFields.Count; index++)
            {
                int slot = optionalFields.Count - index - 1;
                EmitOptionalDecode(
                    optionalFields[index],
                    slot,
                    slot - 1,
                    first: false,
                    forwardInX: slot == 1,
                    finalTailSlot: optionalFields.Count == 1 ? 0 : null,
                    previous
                );
            }
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(record);
            foreach (var register in registers)
            {
                owner.Operand(YRegisterTag, register.Result);
            }
            for (int slot = optionalFields.Count - 1; slot > 0; slot--)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots);
            owner.nextLabel = firstFailure + 1;
            if (range is not null)
            {
                Mark(rangeFailure);
                var callLocation = CallLocation(fields[0].Call.Decode, previous);
                EmitRangeExit(range, CallLocation(range.Exit, callLocation), slots);
            }
            EmitDecoderFailures(failures);

            return true;
        }

        private static int ReadDecoderSlotCount(IReadOnlyList<Expr> items)
        {
            if (items.Count < 8 || items[2] is not Expr.Match
                { Value: Expr.Block { Body: Expr.Call { Function: "binary_to_list" } } }
                || items[4] is not Expr.Match { Value: Expr.Case { Value: Expr.Call } })
            {
                return 0;
            }

            int index = 1;
            while (index + 1 < items.Count
                && items[index] is Expr.Match { Pattern: Pattern.List { Items.Count: 1, Tail: Pattern.Variable { } } }
                && items[index + 1] is Expr.Match { Pattern: Pattern.Variable { }, Value: Expr.Call or Expr.Block or Expr.Case })
            {
                index += 2;
            }

            return (index - 1) / 2;
        }

        private void EmitDecoderFailures(List<(int Label, int? Slot, Expr.Match Match)> listFailures)
        {
            ListFailureCount = listFailures.Count;
            foreach (var error in decoderRangeFailures.AsEnumerable().Reverse())
            {
                Mark(error.Label);
                EmitRangeExit(error.Range, error.Location, frameSize);
            }
            foreach (var error in listFailures.AsEnumerable().Reverse())
            {
                Mark(error.Label);
                var previous = location;
                owner.EmitArithmeticLine(error.Match.SourceLine, ref previous);
                owner.Op(BadMatch, error.Slot.HasValue ? YRegisterTag : XRegisterTag, error.Slot.GetValueOrDefault());
            }
        }

        private int? EmitDecoderSequence(
            Expr.Sequence sequence,
            List<(int Label, int? Slot, Expr.Match Match)> failures,
            int start = 0,
            bool reuseCallSlots = false
        )
        {
            for (int index = start; index < sequence.Items.Count - 1; index++)
            {
                if (index == sequence.Items.Count - 2)
                {
                    if (TryEmitBoundCallTuple(sequence.Items[index], sequence.Items[^1]))
                    {
                        return frameSize;
                    }
                    if (TryEmitCaseResultCall(sequence.Items[index], sequence.Items[^1]))
                    {
                        return null;
                    }
                }
                if (index == sequence.Items.Count - 2
                    && sequence.Items[index] is Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Tuple tuple }
                    && sequence.Items[^1] is Expr.Variable returned && result.Name == returned.Name
                    && !variables.ContainsKey(result.Name)
                    && tuple.Items.All(item => item is Expr.Variable variable && variables.ContainsKey(variable.Name)
                        || AttributeLiteral.TryNormalizeConstant(item, out _)))
                {
                    Expression(tuple);

                    return frameSize;
                }
                if (index + 2 < sequence.Items.Count && TryEmitCaseResultBinding(sequence, index))
                {
                    index++;
                    continue;
                }
                if (index + 2 < sequence.Items.Count
                    && TryEmitListDecodeStep(
                        sequence.Items,
                        index,
                        failures,
                        out int? returnFrame
                    ))
                {
                    if (returnFrame is int size)
                    {
                        return size;
                    }
                    index++;
                }
                else
                {
                    DiscardSequenceItem(
                        sequence.Items,
                        index,
                        null,
                        reuseCallSlots
                    );
                    RememberRecordAccesses(sequence.Items[index]);
                }
            }
            EmitReturnExpression(sequence.Items[^1], reuseCallSlots, terminalCase: true);

            return frameSize;
        }

        private bool TryEmitCaseResultBinding(Expr.Sequence sequence, int index)
        {
            if (sequence.Items[index] is not Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Case decision }
                || sequence.Items[index + 1] is not Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable first, Expr.Variable second]
                    } call
                }
                || result.Name != second.Name || output.Name == result.Name
                || output.Name == VariableScopeNames.Wildcard || variables.ContainsKey(output.Name)
                || variables.ContainsKey(result.Name) || deferredPairMembers.ContainsKey(result.Name)
                || deferredPairMembers.ContainsKey(output.Name) || deferredPairMembers.ContainsKey(first.Name)
                || !variables.TryGetValue(first.Name, out int firstSlot)
                || ClosureCaptures.ReadBranchExports([new Clause([], null, decision)],
                    variables.Keys.Concat(deferredPairMembers.Keys), [second, new Expr.Variable(output.Name)]).Length != 0
                || ReadUsedEntryParameters(new Clause([result], null,
                    sequence with { Items = sequence.Items.Skip(index + 2).ToArray() })) is not { Count: 0 })
            {
                return false;
            }

            if (!TryEmitTaggedCaseSecondArgument(decision))
            {
                EmitCase(
                    decision,
                    null,
                    reuseBranchSlots: true,
                    branchContinuation: sequence.Items.Skip(index + 1).ToArray()
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }
            Load(firstSlot);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);
            variables.Add(output.Name, Save());

            return true;
        }

        private bool TryEmitTaggedCaseSecondArgument(Expr.Case decision)
        {
            if (decision.Value is not Expr.Call { DynamicRemote: false, Arguments.Count: 0 } selector
                || decision.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom tag },
                        Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    }],
                    Body: Expr.Literal first
                },
                {
                    Guard: null, Patterns: [Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: var fallback
                }
                ]
                || fallback is not Expr.Literal && (fallback is not Expr.Call { DynamicRemote: false } fallbackCall
                    || !fallbackCall.Arguments.All(argument => AttributeLiteral.TryNormalizeConstant(argument, out _))))
            {
                return false;
            }

            int otherwise = Label();
            int done = Label();
            Expression(selector);
            owner.Op(IsTaggedTuple, LabelTag, otherwise);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.EmitConstant(first, 1);
            JumpTo(done);
            Mark(otherwise);
            if (fallback is Expr.Literal literal)
            {
                owner.EmitConstant(literal, 1);
            }
            else
            {
                Expression(fallback);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }
            Mark(done);

            return true;
        }

        private bool TryEmitCaseResultCall(Expr binding, Expr consumer)
        {
            if (binding is not Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Case decision }
                || result.Name == VariableScopeNames.Wildcard || variables.ContainsKey(result.Name)
                || deferredPairMembers.ContainsKey(result.Name)
                || consumer is not Expr.Call
                {
                    Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input, ..]
                } call
                || input.Name != result.Name
                || call.Module == ErlangModuleNames.Module && (call.Function != "load_nif" || call.Arguments.Count != 2)
                || !call.Arguments.Skip(1).All(argument => AttributeLiteral.TryNormalizeConstant(argument, out _))
                || ClosureCaptures.ReadBranchExports([new Clause([], null, decision)],
                    variables.Keys.Concat(deferredPairMembers.Keys), [input]).Length != 0)
            {
                return false;
            }

            var failures = new List<(int Label, BeamSourceLocation? Location)>();
            EmitCase(
                decision,
                null,
                reuseBranchSlots: true,
                deferredBooleanFailures: failures
            );
            for (int index = 1; index < call.Arguments.Count; index++)
            {
                owner.EmitConstant(call.Arguments[index], index);
            }
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, call.Arguments.Count));
            owner.Operand(BeamWire.UnsignedTag, frameSize);
            foreach (var (label, failureLocation) in failures)
            {
                EmitBooleanCaseFailure(label, failureLocation);
            }

            return true;
        }

        private bool TryEmitListDecodeStep(
            IReadOnlyList<Expr> items,
            int index,
            List<(int Label, int? Slot, Expr.Match Match)> failures,
            out int? returnFrame,
            string? preparedInput = null
        )
        {
            returnFrame = null;
            if (items[index] is not Expr.Match
                {
                    Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                    Value: Expr.Variable input
                } split
                || items[index + 1] is not Expr.Match { Pattern: Pattern.Variable result, Value: var value }
                || !DistinctBindings(head, tail, result)
                || variables.ContainsKey(head.Name) || variables.ContainsKey(tail.Name) || variables.ContainsKey(result.Name)
                || input.Name == head.Name || input.Name == tail.Name || input.Name == result.Name)
            {
                return false;
            }

            int? source = null;
            if (preparedInput is null)
            {
                if (!variables.TryGetValue(input.Name, out int slot))
                {
                    return false;
                }
                source = slot;
            }
            Expr.Call? conversion = null;
            Expr.Call? decoder = value as Expr.Call;
            if (value is Expr.Block
                {
                    Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false, Function: "binary_to_list", Arguments: [Expr.Call inner]
                    } convert
                }
                && (convert.Module is not null || !entries.ContainsKey(("binary_to_list", 1))))
            {
                conversion = convert;
                decoder = inner;
            }
            else if (value is Expr.Case { Value: Expr.Call select, Clauses.Count: > 0 } decision
                && decision.Clauses[^1].Patterns is [Pattern.Variable fallback]
                && fallback.Name != head.Name && fallback.Name != tail.Name && fallback.Name != input.Name)
            {
                decoder = select;
            }
            else if (value is Expr.Block
            {
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match { Pattern: Pattern.Variable number, Value: Expr.Call read }, Expr.If]
                }
            }
                && number.Name != head.Name && number.Name != tail.Name && number.Name != input.Name)
            {
                decoder = read;
            }

            if (decoder is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument, var tag] } call
                || argument.Name != head.Name || !entries.ContainsKey((call.Function, 2))
                || !AttributeLiteral.TryNormalizeConstant(tag, out Term constant))
            {
                return false;
            }

            var suffix = items.Skip(index + 2).SelectMany(LocalFunctionEntries.Walk).ToArray();
            bool inputUsed = UsesDecoderBinding(suffix, input.Name);
            if (UsesDecoderBinding(suffix, head.Name) || (preparedInput is not null && (input.Name != preparedInput || inputUsed)))
            {
                return false;
            }
            bool reuseSource = source.HasValue && !inputUsed && variables.Values.Count(slot => slot == source) == 1;
            Expr.Tuple? returnTuple = value is Expr.Case || conversion is null && value is Expr.Block
                ? null : ReadDecoderReturnTuple(
                    items,
                    index + 2,
                    result.Name,
                    tail.Name
                );
            int trim = returnTuple is not null && source == 0 && frameSize > 0
                && !returnTuple.Items.OfType<Expr.Variable>().Any(variable =>
                    variables.TryGetValue(variable.Name, out int slot) && slot == 0) ? 1 : 0;
            int? resultSlot = decoderSlotCount != 0 && reuseSource && returnTuple is null ? source : null;

            void EmitRead(Expr.Call read)
            {
                int failure = decoderListLabel ?? Label();
                decoderListLabel--;
                failures.Add((failure, source, split));
                int sourceTag = source.HasValue ? YRegisterTag : XRegisterTag;
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(sourceTag, source.GetValueOrDefault());
                owner.Op(returnTuple is null ? GetList : GetHead, sourceTag, source.GetValueOrDefault());
                owner.Operand(XRegisterTag, 0);
                if (returnTuple is null)
                {
                    int tailSlot = reuseSource && resultSlot is null ? source!.Value : Slot();
                    owner.Operand(YRegisterTag, tailSlot);
                    if (reuseSource)
                    {
                        variables.Remove(input.Name);
                    }
                    variables.Add(tail.Name, tailSlot);
                }
                owner.EmitConstant(constant, 1);
                if (resultSlot is int deadTail)
                {
                    owner.InitStackSlot(deadTail);
                }
                if (trim != 0)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, trim);
                    owner.Operand(BeamWire.UnsignedTag, frameSize - trim);
                }
                owner.EmitCallLine(read, location);
                Call(read.Module, read.Function, 2);
            }

            if (value is Expr.Case)
            {
                return TryBindDecodedEnum(
                    result.Name,
                    value,
                    EmitRead,
                    resultSlot
                );
            }
            if (conversion is null && value is Expr.Block)
            {
                return TryBindDecodedRange(
                    result.Name,
                    value,
                    EmitRead,
                    resultSlot
                );
            }

            EmitRead(call);
            if (conversion is not null)
            {
                owner.EmitCallLine(conversion, CallLocation(call, location));
                Call(conversion.Module, conversion.Function, 1);
            }
            if (returnTuple is null)
            {
                variables.Add(result.Name, SaveDecodedResult(resultSlot));
            }
            else
            {
                EmitDecoderReturnTuple(returnTuple, result.Name, trim);
                returnFrame = frameSize - trim;
            }

            return true;
        }

        private int SaveDecodedResult(int? slot)
        {
            if (slot is not int reused)
            {
                return Save();
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                reused
            );

            return reused;
        }

        private Expr.Tuple? ReadDecoderReturnTuple(
            IReadOnlyList<Expr> items,
            int start,
            string result,
            string tail
        )
        {
            if (IsIgnoredListTail(items[start], tail))
            {
                start++;
            }

            Expr.Tuple? tuple = null;
            if (start + 1 == items.Count && items[start] is Expr.Tuple direct)
            {
                tuple = direct;
            }
            else if (start + 2 == items.Count
                && items[start] is Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Tuple bound }
                && binding.Name != result && binding.Name != tail && !variables.ContainsKey(binding.Name)
                && items[start + 1] is Expr.Variable returned && returned.Name == binding.Name)
            {
                tuple = bound;
            }

            return tuple is not null
                && tuple.Items.Any(item => item is Expr.Variable variable && variable.Name == result)
                && tuple.Items.All(item => item is Expr.Variable variable
                    ? variable.Name == result || variables.ContainsKey(variable.Name)
                    : AttributeLiteral.TryNormalizeConstant(item, out _)) ? tuple : null;
        }

        private void EmitDecoderReturnTuple(Expr.Tuple tuple, string result, int trim)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            foreach (Expr item in tuple.Items)
            {
                if (item is Expr.Variable variable && variable.Name == result)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    GuardOperand(item, item is Expr.Variable binding ? variables[binding.Name] - trim : null);
                }
            }
        }

        private static bool UsesDecoderBinding(IEnumerable<Expr> suffix, string name) => suffix.Any(expression => expression switch
        {
            Expr.Variable variable => variable.Name == name,
            Expr.Match match => !CanBindExactPair(match.Pattern, name, name),
            Expr.Case decision => decision.Clauses.Any(clause => clause.Patterns.Any(pattern => !CanBindExactPair(pattern, name, name))),
            Expr.Block or Expr.Sequence or Expr.Call { DynamicRemote: false } or Expr.Tuple or Expr.If
                or Expr.Binary or Expr.List or Expr.Literal or Expr.Bits => false,
            _ => true
        });

        private bool TryEmitSingleOptionalField(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tag],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple
                            {
                                Items: [
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable field]
                            },
                            Value: Expr.Variable source
                        } initial,
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable bytes, Pattern.Variable size] },
                            Value: Expr.Case { Value: Expr.Variable selector } decision
                        },
                    Expr.Match { Pattern: Pattern.Variable list, Value: Expr.List { Items: [Expr.Variable item], Tail: null } },
                    Expr.Match { Pattern: Pattern.Variable length, Value: Expr.Variable count },
                    Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable finalTag, Expr.Variable finalList, Expr.Variable finalLength]
                        } finish
                ]
                    }
                }]
                || !DistinctBindings(
                    input,
                    tag,
                    field,
                    bytes,
                    size,
                    list,
                    length
                )
                || source.Name != input.Name || selector.Name != field.Name || item.Name != bytes.Name
                || count.Name != size.Name || finalTag.Name != tag.Name || finalList.Name != list.Name
                || finalLength.Name != length.Name || !entries.TryGetValue((finish.Function, 3), out int entry)
                || ReadDeferredOptional(decision) is not { Call: { Module: null, DynamicRemote: false } } optional
                || !entries.ContainsKey((optional.Call.Function, 2))
                || !AttributeLiteral.TryNormalizeConstant(optional.Call.Arguments[1], out _)
                || !ReturnsTuple(
                    decision,
                    2,
                    [],
                    exact: true
                ))
            {
                return false;
            }

            int failure = owner.nextLabel + (optional.Second is null ? 2 : 3);
            owner.EmitTupleCheck(0, 2, failure);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTupleElement(1, 0);
            EmitDeferredOptional(optional, -1, null);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleElement(0, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTupleElement(1, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 1);

            Mark(Label());
            var previous = location;
            owner.EmitArithmeticLine(initial.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private bool TryEmitDeferredTupleFields(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tag],
                    Body: Expr.Sequence sequence
                }
                || sequence.Items is not [Expr.Match
                { Pattern: Pattern.Tuple tuple, Value: Expr.Variable source } initial, ..]
                || !DistinctBindings(input, tag) || source.Name != input.Name || tuple.Items.Count < 2)
            {
                return false;
            }

            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable field)
                {
                    return false;
                }
                if (field.Name != VariableScopeNames.Wildcard
                    && (field.Name == input.Name || field.Name == tag.Name || !fields.TryAdd(field.Name, index)))
                {
                    return false;
                }
            }

            var rest = sequence.Items.Skip(1).ToArray();
            var projections = new List<int>();
            var optionalFields = new List<DeferredOptional?>();
            var bound = new HashSet<string>(StringComparer.Ordinal) { input.Name, tag.Name };
            foreach (Expr item in rest)
            {
                if (item is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] },
                        Value: var value
                    }
                    || !DistinctBindings(first, second)
                    || fields.ContainsKey(first.Name) || fields.ContainsKey(second.Name)
                    || !bound.Add(first.Name) || !bound.Add(second.Name)
                    || !ReturnsTuple(
                        value,
                        2,
                        [],
                        exact: true
                    ))
                {
                    break;
                }
                var optional = ReadDeferredOptional(value);
                if (DeferredField(optional?.Call ?? value, fields, bound) is not int fieldIndex)
                {
                    break;
                }
                projections.Add(fieldIndex);
                optionalFields.Add(optional);
            }
            int count = projections.Count;
            if (count < 2 || count == rest.Length)
            {
                return false;
            }
            // Keep pattern bindings outside the optimized prefix out of the deferred field namespace.
            var suffix = rest.Skip(count).SelectMany(LocalFunctionEntries.Walk).ToArray();
            if (suffix.Any(expression =>
                expression switch
                {
                    Expr.Variable variable => fields.ContainsKey(variable.Name),
                    Expr.Match { Pattern: Pattern.Variable variable } => fields.ContainsKey(variable.Name)
                        || (bound.Contains(variable.Name) && variable.Name != input.Name && variable.Name != tag.Name),
                    Expr.Call or Expr.List or Expr.Binary or Expr.Literal => false,
                    _ => true
                }))
            {
                return false;
            }

            bool inputNeeded = suffix.Any(expression => expression is Expr.Variable read && read.Name == input.Name
                || expression is Expr.Match { Pattern: Pattern.Variable target } && target.Name == input.Name);
            var tail = new Expr.Sequence(rest.Skip(count).ToArray());
            var pairNames = rest.Take(count).Cast<Expr.Match>()
                .SelectMany(match => ((Pattern.Tuple)match.Pattern).Items.Cast<Pattern.Variable>())
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var registerTail = ReadDeferredPairTail(
                tail,
                tag.Name,
                bound,
                pairNames
            );
            bool reuseForFinalOptional = count >= 3 && registerTail is not null && !inputNeeded
                && ((Expr.Match)rest[count - 2]).Value is Expr.Call
                && optionalFields[^1] is { Second: null };
            bool reuseInput = registerTail is not null && !inputNeeded
                && ((Expr.Match)rest[count - 1]).Value is Expr.Call
                && ((Expr.Match)rest[count - 2]).Value is Expr.Call;
            if (!inputNeeded && count == 2 && registerTail is { } twoFields
                && (reuseInput || optionalFields[1] is { Second: null }
                    && (((Expr.Match)rest[0]).Value is Expr.Call || optionalFields[0] is { Second: null })
                    || optionalFields[0] is null && ((Expr.Match)rest[0]).Value is Expr.Case
                    && ((Expr.Match)rest[1]).Value is Expr.Call)
                && ((Expr.Match)rest[0]).Pattern is Pattern.Tuple
                { Items: [Pattern.Variable firstBytes, Pattern.Variable firstLength] }
                && ((Expr.Match)rest[1]).Pattern is Pattern.Tuple
                { Items: [Pattern.Variable secondBytes, Pattern.Variable secondLength] }
                && twoFields.List.Items is [Expr.Variable left, Expr.Variable right]
                && left.Name == firstBytes.Name && right.Name == secondBytes.Name
                && twoFields.Operands is [var leftLength, var rightLength]
                && leftLength == firstLength.Name && rightLength == secondLength.Name)
            {
                EmitTwoDeferredFields(
                    initial,
                    tuple.Items.Count,
                    projections,
                    ((Expr.Match)rest[0]).Value,
                    optionalFields[0],
                    ((Expr.Match)rest[1]).Value,
                    optionalFields[1],
                    twoFields.Sum,
                    twoFields.Entry
                );

                return true;
            }

            reuseInput |= reuseForFinalOptional;
            bool reverseOptionalSlots = registerTail is not null && !inputNeeded && !reuseInput
                && optionalFields.Any(optional => optional is not null);

            int AssignPairSlot()
            {
                int slot = Slot();

                return reverseOptionalSlots ? count - slot : reuseInput ? count - 1 - slot : slot;
            }

            var decisions = rest.Take(count).Cast<Expr.Match>().Where((_, index) => optionalFields[index] is null)
                .Select(match => match.Value).OfType<Expr.Case>().ToArray();
            // Each enum emits one label per literal branch plus its join. The
            // error blocks follow those joins in reverse order, then badmatch.
            int failure = owner.nextLabel + decisions.Sum(decision => decision.Clauses.Count) + decisions.Length
                + optionalFields.Sum(optional => optional is null ? 0 : optional.Second is null ? 2 : 3);
            int enumFailure = failure - 1;
            owner.EmitTupleCheck(0, tuple.Items.Count, failure);
            // The two highest slots are immediately filled from the arguments, before any call.
            EmitGeneralFrame(function.Arity, reuseInput || reverseOptionalSlots ? 2 : 0);
            int tupleSlot = AssignPairSlot();
            int tagSlot = AssignPairSlot();
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                tagSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                tupleSlot
            );
            variables.Add(input.Name, tupleSlot);
            variables.Add(tag.Name, tagSlot);
            var enumErrors = new List<(int Label, Expr.Call Call, List<Atom> Tags)>();
            int? pendingPairSlot = null;
            for (int index = 0; index < count; index++)
            {
                var match = (Expr.Match)rest[index];
                var pair = (Pattern.Tuple)match.Pattern;
                owner.Op(GetTupleElement, index == 0 ? XRegisterTag : YRegisterTag, index == 0 ? 0 : tupleSlot);
                owner.Operand(BeamWire.UnsignedTag, projections[index]);
                bool last = index + 1 == count;
                bool optionalInX1 = pendingPairSlot.HasValue && optionalFields[index] is { Second: null };
                owner.Operand(XRegisterTag, pendingPairSlot.HasValue ? 1 : 0);
                if (pendingPairSlot is int pending)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        pending
                    );
                    if (!optionalInX1)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            1,
                            XRegisterTag,
                            0
                        );
                    }
                    pendingPairSlot = null;
                }
                int result = last && registerTail is not null ? -1
                    : reuseInput && index + 2 == count || last && !inputNeeded ? tupleSlot
                    : AssignPairSlot();
                if (optionalFields[index] is { } optional)
                {
                    bool savedFinalPair = last && reuseForFinalOptional;
                    EmitDeferredOptional(
                        optional,
                        result,
                        last && !inputNeeded && result == -1 && !savedFinalPair ? tupleSlot : null,
                        inputRegister: optionalInX1 ? 1 : 0
                    );
                }
                else if (match.Value is Expr.Call call)
                {
                    owner.EmitConstant(call.Arguments[1], 1);
                    owner.EmitCallLine(call, location);
                    Call(call.Module, call.Function, 2);
                    if (result >= 0)
                    {
                        if (!last && (((Expr.Match)rest[index + 1]).Value is Expr.Call
                            || reuseForFinalOptional && index + 2 == count
                            || reverseOptionalSlots && index + 2 < count && optionalFields[index + 1] is { Second: null }))
                        {
                            pendingPairSlot = result;
                        }
                        else
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                YRegisterTag,
                                result
                            );
                        }
                    }
                }
                else
                {
                    enumErrors.Add(EmitDeferredEnum((Expr.Case)match.Value, enumFailure--, result));
                }
                deferredPairMembers.Add(((Pattern.Variable)pair.Items[0]).Name, (result, 0));
                deferredPairMembers.Add(((Pattern.Variable)pair.Items[1]).Name, (result, 1));
            }

            owner.nextLabel = failure + 1;
            if (registerTail is { } plan)
            {
                var lengths = new Dictionary<string, PairFacts>(StringComparer.Ordinal);
                foreach (Expr.Match match in rest.Take(count))
                {
                    var pair = (Pattern.Tuple)match.Pattern;
                    lengths.Add(((Pattern.Variable)pair.Items[1]).Name, InferPairAdditionType(match.Value));
                }
                EmitDeferredPairTail(plan, tagSlot, lengths);
            }
            else if (!TailLocal(tail))
            {
                Expression(tail);
                owner.EmitFrameReturn(frameSize);
            }
            deferredPairMembers.Clear();
            foreach (var error in enumErrors.AsEnumerable().Reverse())
            {
                Mark(error.Label);
                EmitTaggedExit(
                    error.Call,
                    error.Tags,
                    location,
                    framed: true,
                    frameSlots: frameSize
                );
            }
            Mark(failure);
            var previous = location;
            owner.EmitArithmeticLine(initial.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private void EmitTwoDeferredFields(
            Expr.Match initial,
            int arity,
            IReadOnlyList<int> projections,
            Expr first,
            DeferredOptional? firstOptional,
            Expr second,
            DeferredOptional? optional,
            Expr.Binary sum,
            int entry
        )
        {
            var enumeration = firstOptional is null ? first as Expr.Case : null;
            int failure = owner.nextLabel + (optional is null ? 0 : 2) + (firstOptional is null ? 0 : 2)
                + (enumeration is null ? 0 : enumeration.Clauses.Count + 1);
            int slots = optional is null && enumeration is null ? 2 : 3;
            int tagSlot = slots - 1;
            int secondSlot = slots - 2;
            owner.EmitTupleCheck(0, arity, failure);
            EmitGeneralFrame(2, initializedByMoves: 2, slots: slots);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                tagSlot
            );
            owner.EmitTuplePart(0, projections[0], 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, projections[1]);
            owner.Operand(YRegisterTag, secondSlot);
            (int Label, Expr.Call Call, List<Atom> Tags)? enumError = null;
            if (enumeration is not null)
            {
                enumError = EmitDeferredEnum(
                    enumeration,
                    failure - 1,
                    0,
                    inputRegister: 1
                );
            }
            else if (firstOptional is not null)
            {
                EmitDeferredOptional(
                    firstOptional,
                    0,
                    null,
                    inputRegister: 1
                );
            }
            else
            {
                var call = (Expr.Call)first;
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.EmitConstant(call.Arguments[1], 1);
                owner.EmitCallLine(call, location);
                Call(call.Module, call.Function, 2);
            }
            if (optional is not null)
            {
                if (firstOptional is null)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        0
                    );
                }
                EmitDeferredOptional(
                    optional,
                    -1,
                    secondSlot,
                    inputSlot: secondSlot
                );
            }
            else
            {
                var call = (Expr.Call)second;
                if (enumeration is null)
                {
                    owner.Op(Swap, YRegisterTag, secondSlot);
                    owner.Operand(XRegisterTag, 0);
                }
                owner.EmitConstant(call.Arguments[1], 1);
                if (enumeration is not null)
                {
                    Load(secondSlot, 0);
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        1
                    );
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                }
                owner.EmitCallLine(call, first is Expr.Call previousCall ? CallLocation(previousCall, location) : location);
                Call(call.Module, call.Function, 2);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitDeferredPairMember((0, 0), 1);
            EmitDeferredPairMember((0, 1), 2);
            EmitDeferredPairMember((-1, 0), 3);
            EmitDeferredPairMember((-1, 1), 0);
            owner.Op(PutList, XRegisterTag, 3);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 3);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 1);
            var previous = location;
            owner.EmitArithmeticLine(sum.SourceLine, ref previous);
            EmitPairAddition(
                owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2),
                3,
                2,
                0,
                InferPairAdditionType(first),
                InferPairAdditionType(second)
            );
            int trim = enumeration is null ? 0 : 1;
            Load(tagSlot - trim, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, slots - trim);

            if (enumError is { } error)
            {
                Mark(Label());
                EmitTaggedExit(
                    error.Call,
                    error.Tags,
                    location,
                    framed: true,
                    live: 2,
                    source: 1,
                    frameSlots: slots
                );
            }
            Mark(Label());
            previous = location;
            owner.EmitArithmeticLine(initial.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);
        }

        private (Expr.List List, Expr.Binary Sum, Expr.Call Call, int Entry, List<string> Operands)?
            ReadDeferredPairTail(
                Expr.Sequence tail,
                string tag,
                HashSet<string> bound,
                HashSet<string> pairNames
            )
        {
            if (tail.Items is not [
                    Expr.Match { Pattern: Pattern.Variable bytes, Value: Expr.List list },
                    Expr.Match { Pattern: Pattern.Variable length, Value: Expr.Binary sum },
                    Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable first, Expr.Variable second, Expr.Variable third]
                } call]
                || !DistinctBindings(bytes, length) || bound.Contains(bytes.Name) || bound.Contains(length.Name)
                || first.Name != tag || second.Name != bytes.Name || third.Name != length.Name
                || !entries.TryGetValue((call.Function, 3), out int entry)
                || list.Tail is not null || list.Items.Count == 0
                || !list.Items.All(item => item is Expr.Variable member && pairNames.Contains(member.Name)))
            {
                return null;
            }

            var operands = new List<string>();
            Expr left = sum;
            while (left is Expr.Binary { Operator: ErlangOperators.Plus } addition
                && addition.SourceLine == sum.SourceLine
                && addition.Right is Expr.Variable right
                && pairNames.Contains(right.Name))
            {
                operands.Add(right.Name);
                left = addition.Left;
            }
            if (left is not Expr.Variable initial || !pairNames.Contains(initial.Name))
            {
                return null;
            }

            operands.Add(initial.Name);
            operands.Reverse();

            return (list, sum, call, entry, operands);
        }

        private void EmitDeferredPairTail(
            (Expr.List List, Expr.Binary Sum, Expr.Call Call, int Entry, List<string> Operands) plan,
            int tagSlot,
            Dictionary<string, PairFacts> lengths
        )
        {
            var first = deferredPairMembers[plan.Operands[0]];
            var second = deferredPairMembers[plan.Operands[1]];
            bool scheduled = TrySchedulePairList(
                plan.List,
                first,
                second,
                out int firstRegister,
                out int secondRegister
            );
            if (!scheduled)
            {
                TryEmitDeferredPairList(plan.List, destination: 1);
                EmitDeferredPairMember(first, 2);
            }
            var previous = location;
            owner.EmitArithmeticLine(plan.Sum.SourceLine, ref previous);
            int add = owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2);
            int start = 1;
            PairFacts accumulator = lengths.GetValueOrDefault(plan.Operands[0]);
            if (scheduled)
            {
                PairFacts right = lengths.GetValueOrDefault(plan.Operands[1]);
                EmitPairAddition(
                    add,
                    firstRegister + 1,
                    firstRegister,
                    secondRegister,
                    accumulator,
                    right
                );
                accumulator = (accumulator & right & PairFacts.Length) != 0 ? PairFacts.Length : PairFacts.NumericLength;
                start = 2;
            }
            for (int index = start; index < plan.Operands.Count; index++)
            {
                var member = deferredPairMembers[plan.Operands[index]];
                int destination = scheduled && index + 1 == plan.Operands.Count && member.Slot == -1 ? 0 : 3;
                EmitDeferredPairMember(member, destination);
                PairFacts right = lengths.GetValueOrDefault(plan.Operands[index]);
                EmitPairAddition(
                    add,
                    destination == 0 ? 3 : 4,
                    2,
                    destination,
                    accumulator,
                    right
                );
                accumulator = (accumulator & right & PairFacts.Length) != 0 ? PairFacts.Length : PairFacts.NumericLength;
            }
            Load(tagSlot, 0);
            // OTP omits a call-site line for local tail calls.
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, plan.Entry);
            owner.Operand(BeamWire.UnsignedTag, frameSize);
        }

        private bool TrySchedulePairList(
            Expr.List list,
            (int Slot, int Index) first,
            (int Slot, int Index) second,
            out int firstRegister,
            out int secondRegister
        )
        {
            firstRegister = 0;
            secondRegister = 0;
            var members = list.Items.Cast<Expr.Variable>().Select(variable => deferredPairMembers[variable.Name]).ToArray();
            if (members.Length < 3 || members.Length + 2 >= FirstReservedXRegister
                || members[0] != (first.Slot, 0) || members[1] != (second.Slot, 0)
                || first.Index != 1 || second.Index != 1 || members[^1].Slot != -1
                || members.Take(members.Length - 1).Any(member => member.Slot == -1))
            {
                return false;
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, members.Length * 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            int register = 1;
            var heads = new int[members.Length];
            for (int index = members.Length - 2; index >= 0; index--)
            {
                heads[index] = register++;
                EmitDeferredPairMember(members[index], heads[index]);
                if (index == 1)
                {
                    secondRegister = register++;
                    EmitDeferredPairMember(second, secondRegister);
                }
                else if (index == 0)
                {
                    firstRegister = register++;
                    EmitDeferredPairMember(first, firstRegister);
                }
            }
            EmitDeferredPairMember(members[^1], register);
            owner.Op(PutList, XRegisterTag, register);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, register);
            for (int index = members.Length - 2; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, heads[index]);
                owner.Operand(XRegisterTag, register);
                owner.Operand(XRegisterTag, 1);
                register = 1;
            }

            return true;
        }

        private void EmitPairAddition(
            int import,
            int live,
            int left,
            int right,
            PairFacts leftType,
            PairFacts rightType
        )
        {
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, import);
            EmitPairAdditionOperand(left, leftType);
            EmitPairAdditionOperand(right, rightType);
            owner.Operand(XRegisterTag, 2);
        }

        private void EmitPairAdditionOperand(int register, PairFacts type)
        {
            if ((type & PairFacts.Length) != 0)
            {
                owner.EmitTypedRegister(register, IntegerTypeBits, lower: 0);
            }
            else if ((type & PairFacts.NumericLength) != 0)
            {
                owner.EmitTypedRegister(register, NumberTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, register);
            }
        }

        private bool inferNumericPairs;

        private PairFacts InferPairAdditionType(Expr expression)
        {
            PairFacts joined = ReadJoinedNumericPairFacts(expression);
            if ((joined & PairFacts.NumericLength) != 0 && (joined & PairFacts.Length) == 0)
            {
                return PairFacts.NumericLength;
            }

            return (InferPairFacts(expression) & PairFacts.Length) != 0
                ? PairFacts.Length : joined & PairFacts.NumericLength;
        }

        private PairFacts InferPairFacts(Expr expression)
        {
            const int maxContexts = 512;
            var contexts = new Dictionary<(string Function, string Arguments), PairFacts>();
            bool changed;
            PairFacts result;
            do
            {
                var previous = contexts.ToArray();
                result = ReadPairFacts(expression, new(StringComparer.Ordinal), contexts);
                changed = false;
                foreach (var (key, facts) in previous)
                {
                    var arguments = key.Arguments.Select(value => (PairFacts)value).ToArray();
                    var function = localFunctions[(key.Function, arguments.Length)];
                    PairFacts inferred = facts & ReadPairBranches(
                        function.Clauses,
                        arguments,
                        new(StringComparer.Ordinal),
                        contexts
                    );
                    contexts[key] = inferred;
                    changed |= inferred != facts;
                }
                if (contexts.Count > maxContexts)
                {
                    return PairFacts.Unknown;
                }
                changed |= contexts.Count != previous.Length;
            }
            while (changed);

            return result;
        }

        private bool observePairCalls;
        private bool observeListArithmeticCalls;

        private PairFacts PairAnalysisTop => PairFacts.NoReturn
            | (observeListArithmeticCalls ? PairFacts.BitValue | PairFacts.BitList : PairFacts.Unknown)
            | (inferNumericPairs ? PairFacts.Numeric | PairFacts.NumericLength
                | PairFacts.NonnegativeNumber | PairFacts.NonpositiveNumber | PairFacts.NonnegativeIfNumeric | PairFacts.Atom | PairFacts.List : PairFacts.Unknown);

        private long? NaturalArgumentLowerBound(FunctionDefinition function, int index)
        {
            var facts = ReadLocalArgumentFacts(function, listArithmetic: false);
            PairFacts common = facts?[index] ?? PairFacts.Unknown;

            return (common & PairFacts.Positive) == PairFacts.Positive ? 1
                : (common & PairFacts.Natural) == PairFacts.Natural ? 0 : null;
        }


        private PairFacts[]? ReadLocalArgumentFacts(FunctionDefinition function, bool listArithmetic)
        {
            if (listArithmetic && owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return null;
            }

            var top = listArithmetic ? PairFacts.NoReturn | PairFacts.BitValue | PairFacts.BitList : PairFacts.NoReturn;
            const int maxContexts = 512;
            const int maxListContexts = 2048;
            var contexts = new Dictionary<(string Function, string Arguments), PairFacts>();
            var roots = owner.exportedFunctions.Concat(localFunctions.Values
                .SelectMany(candidate => LocalFunctionEntries.ClauseExpressions(candidate.Clauses))
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.FunctionReference>()
                .Select(reference => (reference.Name, reference.Arity)));
            if (listArithmetic && owner.OnLoadFunction is { } onLoad)
            {
                roots = roots.Append((onLoad, 0));
            }

            var ancestors = listArithmetic ? ReadLocalCallAncestors(function) : null;
            foreach (var root in roots)
            {
                if (localFunctions.ContainsKey(root) && (ancestors is null || ancestors.Contains(root)))
                {
                    contexts.TryAdd((root.Name, new string((char)PairFacts.Unknown, root.Arity)), top);
                }
            }

            observePairCalls = true;
            observeListArithmeticCalls = listArithmetic;
            try
            {
                bool changed;
                do
                {
                    if (contexts.Count > (listArithmetic ? maxListContexts : maxContexts))
                    {
                        return null;
                    }

                    var previous = contexts.ToArray();
                    changed = false;
                    foreach (var (key, facts) in previous)
                    {
                        var arguments = key.Arguments.Select(value => (PairFacts)value).ToArray();
                        var candidate = localFunctions[(key.Function, arguments.Length)];
                        PairFacts inferred = facts & ReadPairBranches(
                            candidate.Clauses,
                            arguments,
                            new(StringComparer.Ordinal),
                            contexts
                        );
                        contexts[key] = inferred;
                        changed |= inferred != facts;
                    }
                    changed |= contexts.Count != previous.Length;
                }
                while (changed);
            }
            finally
            {
                observePairCalls = false;
                observeListArithmeticCalls = false;
            }

            var calls = contexts.Keys.Where(key => key.Function == function.Name && key.Arguments.Length == function.Arity).ToArray();

            if (calls.Length == 0)
            {
                return null;
            }

            return Enumerable.Range(0, function.Arity).Select(index =>
                calls.Aggregate(top, (current, key) => current & (PairFacts)key.Arguments[index])).ToArray();
        }

        private HashSet<(string Name, int Arity)> ReadLocalCallAncestors(FunctionDefinition target)
        {
            var callers = new Dictionary<(string Name, int Arity), HashSet<(string Name, int Arity)>>();
            foreach (var (signature, function) in localFunctions)
            {
                foreach (var call in LocalFunctionEntries.ClauseExpressions(function.Clauses)
                    .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Call>())
                {
                    if (call is not { Module: null, DynamicRemote: false })
                    {
                        continue;
                    }

                    var callee = (call.Function, call.Arguments.Count);
                    if (!callers.TryGetValue(callee, out var sources))
                    {
                        sources = [];
                        callers.Add(callee, sources);
                    }

                    sources.Add(signature);
                }
            }

            var ancestors = new HashSet<(string Name, int Arity)>();
            var pending = new Stack<(string Name, int Arity)>();
            pending.Push((target.Name, target.Arity));
            while (pending.TryPop(out var callee))
            {
                if (!ancestors.Add(callee) || !callers.TryGetValue(callee, out var sources))
                {
                    continue;
                }

                foreach (var caller in sources)
                {
                    pending.Push(caller);
                }
            }

            return ancestors;
        }

        private PairFacts ReadPairFacts(
            Expr expression,
            Dictionary<string, PairFacts> bindings,
            Dictionary<(string Function, string Arguments), PairFacts> contexts
        )
        {
            PairFacts facts = ReadPairFactsCore(expression, bindings, contexts);
            if (inferNumericPairs)
            {
                if ((facts & PairFacts.Integer) != 0)
                {
                    facts |= PairFacts.Numeric;
                }
                if ((facts & PairFacts.Natural) == PairFacts.Natural)
                {
                    facts |= PairFacts.NonnegativeNumber;
                }
                if ((facts & PairFacts.NonnegativeNumber) != 0)
                {
                    facts |= PairFacts.NonnegativeIfNumeric;
                }
                if ((facts & PairFacts.Length) != 0)
                {
                    facts |= PairFacts.NumericLength;
                }
            }

            return facts;
        }

        private PairFacts ReadPairFactsCore(
            Expr expression,
            Dictionary<string, PairFacts> bindings,
            Dictionary<(string Function, string Arguments), PairFacts> contexts
        )
        {
            switch (expression)
            {
                case Expr.Literal { Value: Atom } when inferNumericPairs:
                    return PairFacts.Atom;
                case Expr.Literal { Value: Integer value }:
                    if (observeListArithmeticCalls && (value.Value.IsZero || value.Value.IsOne))
                    {
                        return value.Value.IsZero ? PairFacts.BitValue : PairFacts.BitValue | PairFacts.Positive;
                    }
                    if (value.Value > 0)
                    {
                        return PairFacts.Positive;
                    }

                    if (!inferNumericPairs)
                    {
                        return value.Value >= 0 ? PairFacts.Natural : PairFacts.Integer;
                    }

                    return value.Value.IsZero ? PairFacts.Natural | PairFacts.NonpositiveNumber
                        : PairFacts.Integer | PairFacts.NonpositiveNumber;
                case Expr.Literal { Value: FloatTerm } when inferNumericPairs:
                    return PairFacts.Numeric;
                case Expr.Literal { Value: Nil }:
                    return observeListArithmeticCalls ? PairFacts.BitList : PairFacts.ProperList;
                case Expr.Variable variable:
                    return bindings.GetValueOrDefault(variable.Name);
                case Expr.List list:
                    bool allBits = observeListArithmeticCalls;
                    if (observePairCalls)
                    {
                        foreach (var item in list.Items)
                        {
                            var itemFacts = ReadPairFacts(item, bindings, contexts);
                            allBits &= (itemFacts & PairFacts.BitValue) == PairFacts.BitValue;
                        }
                    }

                    PairFacts tailFacts = list.Tail is null ? PairFacts.BitList
                        : ReadPairFacts(list.Tail, bindings, contexts);
                    PairFacts listFacts = allBits && (tailFacts & PairFacts.BitList) == PairFacts.BitList
                        ? PairFacts.BitList : tailFacts & PairFacts.ProperList;

                    return observePairCalls && list.Items.Count != 0 ? listFacts | PairFacts.NonemptyList : listFacts;
                case Expr.Tuple { Items: [_, var size] }:
                    if (observePairCalls)
                    {
                        ReadPairFacts(((Expr.Tuple)expression).Items[0], bindings, contexts);
                    }
                    PairFacts sizeFacts = ReadPairFacts(size, bindings, contexts);
                    if ((sizeFacts & PairFacts.Positive) == PairFacts.Positive)
                    {
                        return PairFacts.PositiveLength;
                    }

                    return (sizeFacts & PairFacts.Natural) == PairFacts.Natural
                        ? PairFacts.Length : (sizeFacts & PairFacts.Numeric) != 0 ? PairFacts.NumericLength : PairFacts.Unknown;
                case Expr.Binary binary when observeListArithmeticCalls && IsRegisterArithmeticOperator(binary.Operator):
                    var leftFacts = ReadPairFacts(binary.Left, bindings, contexts);
                    var rightFacts = ReadPairFacts(binary.Right, bindings, contexts);

                    return IsIntegerRegisterArithmetic(binary.Operator)
                        || binary.Operator != ErlangOperators.Divide
                            && (leftFacts & rightFacts & PairFacts.Integer) != 0
                        ? PairFacts.Integer : PairFacts.Unknown;
                case Expr.Binary { Operator: ErlangOperators.Plus } addition:
                    PairFacts left = ReadPairFacts(addition.Left, bindings, contexts);
                    PairFacts right = ReadPairFacts(addition.Right, bindings, contexts);
                    if ((left & right & PairFacts.Natural) == PairFacts.Natural
                        && ((left | right) & PairFacts.Positive) == PairFacts.Positive)
                    {
                        return PairFacts.Positive;
                    }

                    PairFacts integerSum = left & right & PairFacts.Natural;
                    PairFacts sign = left & right & (PairFacts.NonnegativeNumber | PairFacts.NonpositiveNumber);

                    return inferNumericPairs ? integerSum | PairFacts.Numeric | sign : integerSum;
                case Expr.Block block:
                    return ReadPairFacts(block.Body, bindings, contexts);
                case Expr.Match match when observePairCalls || inferNumericPairs:
                    return ReadPairFacts(match.Value, bindings, contexts);
                case Expr.Binary { Operator: ErlangOperators.ShiftRight, Right: Expr.Literal { Value: Integer } } shift
                    when observePairCalls:
                    PairFacts shifted = ReadPairFacts(shift.Left, bindings, contexts);
                    PairFacts shiftSign = shifted & (PairFacts.NonnegativeNumber | PairFacts.NonpositiveNumber);
                    if ((shifted & PairFacts.NonnegativeIfNumeric) != 0)
                    {
                        shiftSign |= PairFacts.NonnegativeNumber;
                    }

                    if (inferNumericPairs && shiftSign != PairFacts.Unknown)
                    {
                        return PairFacts.Integer | shiftSign | (shifted & PairFacts.Natural);
                    }

                    return shifted & PairFacts.Natural;
                case Expr.Sequence sequence:
                    var scope = new Dictionary<string, PairFacts>(bindings, StringComparer.Ordinal);
                    foreach (Expr item in sequence.Items.SkipLast(1))
                    {
                        if (item is Expr.Match binding)
                        {
                            BindPairFacts(binding.Pattern, ReadPairFacts(binding.Value, scope, contexts), scope);
                        }
                        else if (observePairCalls)
                        {
                            ReadPairFacts(item, scope, contexts);
                        }
                    }

                    return ReadPairFacts(sequence.Items[^1], scope, contexts);
                case Expr.Catch caught when collectNumericPairCalls:
                    ReadPairFacts(caught.Operand, new(bindings, StringComparer.Ordinal), contexts);

                    return PairFacts.Unknown;
                case Expr.Try tried when collectNumericPairCalls:
                    var protectedFacts = ReadPairFacts(tried.Body, new(bindings, StringComparer.Ordinal), contexts);
                    ReadPairBranches(tried.Clauses, [protectedFacts], bindings, contexts);
                    ReadPairBranches(tried.Catches, [], bindings, contexts);
                    if (tried.After is { } cleanup)
                    {
                        ReadPairFacts(cleanup, new(bindings, StringComparer.Ordinal), contexts);
                    }

                    return PairFacts.Unknown;
                case Expr.Case decision:
                    PairFacts selector = ReadPairFacts(decision.Value, bindings, contexts);

                    return ReadPairBranches(
                        decision.Clauses,
                        [selector],
                        bindings,
                        contexts
                    );
                case Expr.If decision:
                    return ReadPairBranches(
                        decision.Clauses,
                        [],
                        bindings,
                        contexts
                    );
                case Expr.Call { Module: null, DynamicRemote: false } call
                    when localFunctions.ContainsKey((call.Function, call.Arguments.Count)):
                    var arguments = call.Arguments.Select(argument => ReadPairFacts(argument, bindings, contexts)).ToArray();
                    if (activeNumericPairFlow is { } flow)
                    {
                        var signature = (call.Function, arguments.Length);
                        if (collectNumericPairCalls)
                        {
                            flow.Observe(signature, arguments);
                        }

                        return !owner.IsDeclaredNative(signature) && flow.Entries.TryGetValue(signature, out var entry)
                            ? entry.Result : PairFacts.Unknown;
                    }

                    var key = (call.Function, new string(arguments.Select(fact => (char)fact).ToArray()));
                    contexts.TryAdd(key, PairAnalysisTop);
                    if (owner.IsDeclaredNative((call.Function, call.Arguments.Count)))
                    {
                        return PairFacts.Unknown;
                    }

                    if (observePairCalls && call.Arguments.Count == 2
                        && ReadSignedOctetAccumulator(localFunctions[(call.Function, 2)]) is not null)
                    {
                        return contexts[key] | PairFacts.NonemptyList;
                    }

                    return contexts[key];
                case Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments.Count: 1 } call
                    when owner.ResolvesErlangBif(call):
                    if (observePairCalls)
                    {
                        PairFacts argument = ReadPairFacts(call.Arguments[0], bindings, contexts);
                        if (call.Function == "length" && (argument & PairFacts.NonemptyList) != 0)
                        {
                            return PairFacts.Positive;
                        }
                    }

                    return call.Function switch
                    {
                        "length" or "byte_size" => PairFacts.Natural,
                        AutoImportNames.ListToAtom or AutoImportNames.ListToExistingAtom
                            or AutoImportNames.BinaryToAtom or AutoImportNames.BinaryToExistingAtom when inferNumericPairs
                            => PairFacts.Atom,
                        "exit" => PairAnalysisTop,
                        _ => PairFacts.Unknown
                    };
                case Expr.Call
                    {
                        Function: AutoImportNames.BinaryToAtom or AutoImportNames.BinaryToExistingAtom,
                        DynamicRemote: false, Arguments.Count: 2
                    } conversion when inferNumericPairs && owner.ResolvesErlangBif(conversion):
                    foreach (Expr argument in conversion.Arguments)
                    {
                        ReadPairFacts(argument, bindings, contexts);
                    }

                    return PairFacts.Atom;
                case Expr.Call { Module: "lists", Function: "reverse", DynamicRemote: false, Arguments: [var value] }:
                    if (observePairCalls)
                    {
                        ReadPairFacts(value, bindings, contexts);
                    }

                    return PairFacts.ProperList;
                case Expr.Call
                {
                    Module: "lists", Function: "dropwhile", DynamicRemote: false,
                    Arguments: [var predicate, var list]
                }:
                    if (observePairCalls)
                    {
                        ReadPairFacts(predicate, bindings, contexts);
                    }

                    return ReadPairFacts(list, bindings, contexts) & PairFacts.ProperList;
                case Expr.Call call when observePairCalls:
                    foreach (var argument in call.Arguments)
                    {
                        ReadPairFacts(argument, bindings, contexts);
                    }

                    return PairFacts.Unknown;
                default:
                    if (observePairCalls)
                    {
                        foreach (var nested in LocalFunctionEntries.Walk(expression).OfType<Expr.Call>())
                        {
                            if (nested is { Module: null, DynamicRemote: false }
                                && localFunctions.ContainsKey((nested.Function, nested.Arguments.Count)))
                            {
                                if (activeNumericPairFlow is { } fallbackFlow)
                                {
                                    fallbackFlow.Observe((nested.Function, nested.Arguments.Count), new PairFacts[nested.Arguments.Count]);
                                }
                                else
                                {
                                    contexts.TryAdd((nested.Function, new string((char)PairFacts.Unknown, nested.Arguments.Count)),
                                        PairAnalysisTop);
                                }
                            }
                        }
                    }

                    return PairFacts.Unknown;
            }
        }

        private PairFacts ReadPairBranches(
            IReadOnlyList<Clause> clauses,
            IReadOnlyList<PairFacts> arguments,
            Dictionary<string, PairFacts> bindings,
            Dictionary<(string Function, string Arguments), PairFacts> contexts
        )
        {
            PairFacts facts = PairAnalysisTop;
            foreach (Clause clause in clauses)
            {
                var scope = new Dictionary<string, PairFacts>(bindings, StringComparer.Ordinal);
                for (int index = 0; index < arguments.Count; index++)
                {
                    BindPairFacts(clause.Patterns[index], arguments[index], scope);
                }
                if (observePairCalls && clause.Guard is { } guard)
                {
                    ReadPairFacts(guard, scope, contexts);
                    if (inferNumericPairs)
                    {
                        RefineNumericComparison(guard, scope, succeeded: true);
                        RefineEntryTypeGuard(guard, scope);
                    }
                }
                facts &= ReadPairFacts(clause.Body, scope, contexts);
                if (inferNumericPairs && clause.Guard is not null)
                {
                    if (arguments.Count == 0)
                    {
                        bindings = new(bindings, StringComparer.Ordinal);
                        RefineNumericComparison(clause.Guard, bindings, succeeded: false);
                    }
                    else
                    {
                        arguments = RefineFailedNumericClause(clause, bindings, arguments);
                    }
                }
            }

            return facts;
        }

        private void BindPairFacts(Pattern pattern, PairFacts facts, Dictionary<string, PairFacts> bindings)
        {
            if (pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable)
            {
                bindings.TryAdd(variable.Name, facts);
            }
            else if (observeListArithmeticCalls && pattern is Pattern.List list)
            {
                PairFacts head = (facts & PairFacts.BitList) == PairFacts.BitList
                    ? PairFacts.BitValue : PairFacts.Unknown;
                foreach (var item in list.Items)
                {
                    BindPairFacts(item, head, bindings);
                }

                if (list.Tail is { } tail)
                {
                    BindPairFacts(tail, facts & PairFacts.BitList, bindings);
                }
            }
            else if (pattern is Pattern.Tuple tuple)
            {
                for (int index = 0; index < tuple.Items.Count; index++)
                {
                    PairFacts member = PairFacts.Unknown;
                    if (tuple.Items.Count == 2 && index == 1)
                    {
                        member = (facts & PairFacts.Length) != 0
                            ? (facts & PairFacts.PositiveLength) == PairFacts.PositiveLength ? PairFacts.Positive : PairFacts.Natural
                            : (facts & PairFacts.NumericLength) != 0 ? PairFacts.Numeric : PairFacts.Unknown;
                    }
                    BindPairFacts(tuple.Items[index], member, bindings);
                }
            }
            else if (pattern is BitPattern bits)
            {
                foreach (var segment in bits.Segments)
                {
                    PairFacts value = segment.Specification.Type == BitSegmentTypes.Integer
                        ? PairFacts.Integer : PairFacts.Unknown;
                    BindPairFacts(segment.Value, value, bindings);
                }
            }
        }

        private (int Label, Expr.Call Call, List<Atom> Tags) EmitDeferredEnum(
            Expr.Case decision,
            int failure,
            int result,
            int inputRegister = 0
        )
        {
            var branches = decision.Clauses.SkipLast(1)
                .Select(clause => (Key: ((Pattern.Literal)clause.Patterns[0]).Value, Call: (Expr.Call)clause.Body))
                .OrderByDescending(branch => branch.Key, Comparer<Term>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)))
                .Select(branch => (branch.Key, branch.Call, Label: Label())).ToArray();
            int done = Label();
            owner.Op(SelectValue, XRegisterTag, inputRegister);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            for (int index = 0; index < branches.Length; index++)
            {
                var branch = branches[index];
                Mark(branch.Label);
                var constants = new List<(int Destination, Term Value)>();
                for (int argument = 0; argument < branch.Call.Arguments.Count; argument++)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(branch.Call.Arguments[argument], out Term value))
                    {
                        throw new NotSupportedException(Unsupported);
                    }
                    constants.Add((argument, value));
                }
                owner.EmitIndependentCallMoves(constants);
                owner.EmitCallLine(branch.Call, location);
                Call(branch.Call.Module, branch.Call.Function, branch.Call.Arguments.Count);
                if (result >= 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        result
                    );
                }
                if (index + 1 < branches.Length)
                {
                    JumpTo(done);
                }
            }
            Mark(done);
            Clause fallback = decision.Clauses[^1];
            var error = ReadTaggedExit(fallback.Body, ((Pattern.Variable)fallback.Patterns[0]).Name)
                ?? throw new NotSupportedException(Unsupported);

            return (failure, error.Call, error.Tags);
        }

        private sealed record DeferredOptional(
            Atom First,
            Atom? Second,
            Term Default,
            Expr.Call Call
        );

        private static DeferredOptional? ReadDeferredOptional(Expr value)
        {
            if (value is not Expr.Case { Value: Expr.Variable selector, Clauses: var clauses }
                || clauses.Count is not (2 or 3)
                || clauses[0] is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }] }
                || clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call { Arguments: [Expr.Variable field, _] } call
                }
                || field.Name != selector.Name
                || !AttributeLiteral.TryNormalizeConstant(clauses[0].Body, out Term defaultPair))
            {
                return null;
            }

            if (clauses.Count == 2)
            {
                return new(
                    first,
                    null,
                    defaultPair,
                    call
                );
            }

            return clauses[1] is
            {
                Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                Guard: Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual, Left: Expr.Variable checkedField,
                    Right: Expr.Literal { Value: Atom second }
                }, Body: var alternative
            }
                && checkedField.Name == selector.Name && first != second
                && AttributeLiteral.TryNormalizeConstant(alternative, out Term otherPair) && defaultPair.Equals(otherPair)
                ? new(
                    first,
                    second,
                    defaultPair,
                    call
                ) : null;
        }

        private void EmitDeferredOptional(
            DeferredOptional optional,
            int result,
            int? deadTupleSlot,
            int? inputSlot = null,
            int inputRegister = 0
        )
        {
            int absent = optional.Second is null ? 0 : Label();
            int encode = Label();
            int done = Label();
            if (optional.Second is { } second)
            {
                owner.Op(SelectValue, XRegisterTag, 0);
                owner.Operand(LabelTag, encode);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 4);
                foreach (Atom tag in new[] { optional.First, second }.OrderBy(tag => tag,
                    Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))))
                {
                    owner.LiteralOperand(tag);
                    owner.Operand(LabelTag, absent);
                }
                Mark(absent);
            }
            else
            {
                owner.Op(ExactEqualTest, LabelTag, encode);
                owner.Operand(inputSlot.HasValue ? YRegisterTag : XRegisterTag, inputSlot ?? inputRegister);
                owner.LiteralOperand(optional.First);
            }
            owner.EmitConstant(optional.Default, Math.Max(result, 0), result < 0 ? XRegisterTag : YRegisterTag);
            JumpTo(done);

            Mark(encode);
            var call = optional.Call;
            if (inputRegister != 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    inputRegister,
                    XRegisterTag,
                    0
                );
            }
            owner.EmitConstant(call.Arguments[1], 1);
            if (inputSlot is int input)
            {
                Load(input, 0);
            }
            if (deadTupleSlot is int slot)
            {
                owner.InitStackSlot(slot);
            }
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);
            if (result >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    result
                );
            }
            Mark(done);
        }

        private int? DeferredField(Expr value, Dictionary<string, int> fields, HashSet<string> bound)
        {
            if (value is Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable field, var literal] }
                && AttributeLiteral.TryNormalizeConstant(literal, out _))
            {
                return fields.TryGetValue(field.Name, out int fieldIndex) ? fieldIndex : null;
            }
            if (value is Expr.Case { Value: Expr.Variable selector } decision
                && decision.Clauses.Count >= 3
                && decision.Clauses[^1] is { Guard: null, Patterns: [Pattern.Variable key] } fallback
                && !fields.ContainsKey(key.Name) && !bound.Contains(key.Name)
                && ReadTaggedExit(fallback.Body, key.Name) is not null
                && decision.Clauses.SkipLast(1).All(branch => branch is
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom }],
                    Body: Expr.Call { Module: null, DynamicRemote: false } call
                }
                    && call.Arguments.All(argument => AttributeLiteral.TryNormalizeConstant(argument, out _)))
                && decision.Clauses.SkipLast(1).Select(branch => ((Pattern.Literal)branch.Patterns[0]).Value)
                    .Distinct().Count() == decision.Clauses.Count - 1)
            {
                return fields.TryGetValue(selector.Name, out int fieldIndex) ? fieldIndex : null;
            }

            return null;
        }

        private void EmitGeneralFrame(int arity, int initializedByMoves = 0, int? slots = null)
        {
            int size = slots ?? frameSize;
            EmitFrameAllocation(arity, size);
            EmitFrameInitialization(0, size - initializedByMoves);
        }

        private void EmitFrameAllocation(int arity, int size, int heapWords = 0)
        {
            owner.Op(heapWords == 0 ? Allocate : AllocateHeap, BeamWire.UnsignedTag, size);
            if (heapWords != 0)
            {
                owner.Operand(BeamWire.UnsignedTag, heapWords);
            }
            owner.Operand(BeamWire.UnsignedTag, arity);
        }

        private void EmitFrameInitialization(int first, int end, int? leading = null)
        {
            int count = Math.Max(0, end - first) + (leading.HasValue ? 1 : 0);
            if (count == 0)
            {
                return;
            }

            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count);
            if (leading is int slot)
            {
                owner.Operand(YRegisterTag, slot);
            }

            for (int index = first; index < end; index++)
            {
                owner.Operand(YRegisterTag, index);
            }
        }

        private bool TryEmitLiteralEncoder(FunctionDefinition function)
        {
            if (function.Arity != 2 || function.Clauses.Count < 3
                || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable key, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                } fallback
                || ReadTaggedExit(fallback.Body, key.Name) is not { } error)
            {
                return false;
            }

            var branches = new List<(Atom Key, Term Payload, Integer Length, int Entry)>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }, Pattern.Variable tag],
                        Body: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable forwarded, var payload, Expr.Literal { Value: Integer length }]
                        } call
                    }
                    || forwarded.Name != tag.Name || tag.Name == VariableScopeNames.Wildcard
                    || !keys.Add(atom.Name) || !entries.TryGetValue((call.Function, 3), out int entry)
                    || !AttributeLiteral.TryNormalizeConstant(payload, out Term constant))
                {
                    return false;
                }
                branches.Add((atom, constant, length, entry));
            }

            var ordered = branches.OrderByDescending(branch => branch.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)))
                .Select(branch => (Branch: branch, Label: Label())).ToArray();
            int failure = Label();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, ordered.Length * 2);
            foreach (var item in ordered.Reverse())
            {
                owner.LiteralOperand(item.Branch.Key);
                owner.Operand(LabelTag, item.Label);
            }
            foreach (var item in ordered)
            {
                Mark(item.Label);
                owner.EmitConstant(item.Branch.Length, 2);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.EmitConstant(item.Branch.Payload, 1);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(LabelTag, item.Branch.Entry);
            }
            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                location,
                framed: false
            );

            return true;
        }

        private bool TryEmitProjectedChoice(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable saved],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable bytes, Pattern.Variable size] },
                            Value: Expr.Case decision
                        },
                        Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable forwarded, Expr.Variable data, Expr.Variable length]
                        } tail]
                    }
                }
                || !DistinctBindings(
                    input,
                    saved,
                    bytes,
                    size
                )
                || forwarded.Name != saved.Name || data.Name != bytes.Name || length.Name != size.Name
                || !IsElementOf(decision.Value, 1, input.Name)
                || !entries.TryGetValue((tail.Function, 3), out int tailEntry)
                || decision.Clauses.Count < 3
                || decision.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable key] } fallback
                || !DistinctBindings(input, saved, key)
                || ReadTaggedExit(fallback.Body, key.Name) is not { } error)
            {
                return false;
            }

            var branches = new List<(Atom Key, Expr.Call Call, Term Constant, int Entry)>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Clause branch in decision.Clauses.SkipLast(1))
            {
                if (branch is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [var projected, var literal] } call
                    }
                    || !keys.Add(atom.Name) || !IsElementOf(projected, 2, input.Name)
                    || !AttributeLiteral.TryNormalizeConstant(literal, out Term constant)
                    || !entries.TryGetValue((call.Function, 2), out int entry)
                    || !ReturnsTuple(
                        call,
                        2,
                        [],
                        exact: true
                    ))
                {
                    return false;
                }
                branches.Add((atom, call, constant, entry));
            }

            var ordered = branches.OrderByDescending(branch => branch.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)))
                .Select(branch => (Branch: branch, Label: Label())).ToArray();
            int done = Label();
            int failure = Label();
            bool emptyTag = AlwaysCalledWithEmptySecondArgument(function);
            int frameSlots = emptyTag ? 0 : 1;
            int selector = emptyTag ? 1 : 2;
            int live = selector + 1;
            int element = owner.Import(ErlangModuleNames.Module, "element", 2);
            owner.EmitCallLine((Expr.Call)decision.Value, location);
            EmitChoiceElement(element, 1, selector);
            owner.Op(Allocate, BeamWire.UnsignedTag, frameSlots);
            owner.Operand(BeamWire.UnsignedTag, live);
            if (!emptyTag)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }
            owner.Op(SelectValue, XRegisterTag, selector);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, ordered.Length * 2);
            foreach (var item in ordered.Reverse())
            {
                owner.LiteralOperand(item.Branch.Key);
                owner.Operand(LabelTag, item.Label);
            }
            for (int index = 0; index < ordered.Length; index++)
            {
                var item = ordered[index];
                Mark(item.Label);
                owner.EmitCallLine(item.Branch.Call, location);
                EmitChoiceElement(element, 2, 0);
                owner.EmitConstant(item.Branch.Constant, 1);
                owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, item.Branch.Entry);
                if (index + 1 < ordered.Length)
                {
                    JumpTo(done);
                }
            }

            Mark(done);
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 2);
            if (emptyTag)
            {
                owner.EmitConstant(Term.List(), 0);
            }
            else
            {
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
            }
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, tailEntry);
            owner.Operand(BeamWire.UnsignedTag, frameSlots);
            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                location,
                framed: true,
                live: live,
                source: selector,
                frameSlots: frameSlots
            );

            return true;
        }

        private bool AlwaysCalledWithEmptySecondArgument(FunctionDefinition function)
        {
            if (owner.exportedFunctions.Contains((function.Name, function.Arity)))
            {
                return false;
            }

            bool called = false;
            var expressions = localFunctions.Values
                .SelectMany(definition => LocalFunctionEntries.ClauseExpressions(definition.Clauses))
                .SelectMany(LocalFunctionEntries.Walk);
            foreach (Expr expression in expressions)
            {
                if (expression is Expr.FunctionReference reference
                    && reference.Name == function.Name && reference.Arity == function.Arity)
                {
                    return false;
                }

                if (expression is Expr.Call { Module: null, DynamicRemote: false, Arguments: [_, var tag] } call
                    && call.Function == function.Name)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(tag, out Term value) || value is not Nil)
                    {
                        return false;
                    }
                    called = true;
                }
            }

            return called;
        }

        private bool IsElementOf(Expr expression, int index, string variable) =>
            expression is Expr.Call
            {
                Function: "element", Module: null or ErlangModuleNames.Module,
                DynamicRemote: false, Arguments: [Expr.Literal { Value: Integer position }, Expr.Variable tuple]
            } call
            && position.Value == index && tuple.Name == variable
            && (call.Module is not null || !entries.ContainsKey(("element", 2)));

        private void EmitChoiceElement(int import, int index, int destination)
        {
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, import);
            owner.Operand(BeamWire.IntegerTag, index);
            if (index == 1)
            {
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.EmitTypedRegister(0, TupleTypeBits);
            }
            owner.Operand(XRegisterTag, destination);
        }

        private (byte Opcode, int Register)? ReadRegisterTypeGuard(Clause clause, bool allowList = false)
        {
            if (!HasUnconditionalParameters(clause)
                || clause.Guard is not Expr.Call
                {
                    Function: "is_atom" or "is_integer" or "is_list", DynamicRemote: false,
                    Module: null or ErlangModuleNames.Module, Arguments: [Expr.Variable tested]
                } guard
                || !allowList && guard.Function == "is_list"
                || guard.Module is null && entries.ContainsKey((guard.Function, 1)))
            {
                return null;
            }

            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable binding && binding.Name == tested.Name)
                {
                    return (TypeGuardOpcode(guard), index);
                }
            }

            return null;
        }

        private bool TryEmitGuardedConstantReturns(FunctionDefinition function)
        {
            var branches = new List<(byte Opcode, int Register, Term Value)>();
            foreach (Clause clause in function.Clauses)
            {
                Expr result = clause.Body;
                if (result is Expr.Sequence sequence && sequence.Items.SkipLast(1).All(item => item is Expr.Literal))
                {
                    result = sequence.Items[^1];
                }

                if (ReadRegisterTypeGuard(clause) is not { } guard || result is not Expr.Literal literal)
                {
                    return false;
                }

                branches.Add((guard.Opcode, guard.Register, literal.Value));
            }

            int functionInfo = owner.nextLabel - 2;
            for (int index = 0; index < branches.Count; index++)
            {
                var (opcode, register, value) = branches[index];
                bool last = index == branches.Count - 1;
                int failure = last ? functionInfo : Label();
                owner.Op(opcode, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.EmitConstant(value, 0);
                owner.instructions.WriteByte(Return);
                if (!last)
                {
                    Mark(failure);
                }
            }

            return true;
        }

        private bool TryEmitRepeatedArgumentReturns(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Variable left, Pattern.Variable right] } first,
                { Guard: null } fallback
                ]
                || left.Name == VariableScopeNames.Wildcard || left.Name != right.Name
                || !HasUnconditionalParameters(fallback)
                || !AttributeLiteral.TryNormalizeConstant(first.Body, out Term equal)
                || !AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term unequal))
            {
                return false;
            }

            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(equal, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(unequal, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitLiteralConstantReturn(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause]
                || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result))
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var tests = new List<(int Register, Term Value)>();
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                switch (clause.Patterns[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when names.Add(variable.Name):
                        break;
                    case Pattern.Literal { Value: Atom or Integer or Nil } literal:
                        tests.Add((index, literal.Value));
                        break;
                    default:
                        return false;
                }
            }

            if (tests.Count == 0)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            foreach (var (register, value) in tests)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(value);
            }

            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitNilArgumentReturn(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Variable result } clause])
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            int nilRegister = -1;
            int returnRegister = -1;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                switch (clause.Patterns[index])
                {
                    case Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil } when nilRegister < 0:
                        nilRegister = index;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when names.Add(variable.Name):
                        if (variable.Name == result.Name)
                        {
                            returnRegister = index;
                        }
                        break;
                    default:
                        return false;
                }
            }

            if (nilRegister < 0 || returnRegister < 0)
            {
                return false;
            }

            owner.EmitNilTest(XRegisterTag, nilRegister, owner.nextLabel - 2);
            if (returnRegister != 0)
            {
                owner.MoveToX0(XRegisterTag, returnRegister);
            }
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitLiteralPatternTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom }],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match { Pattern: Pattern.Variable binding, Value: var value },
                    Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call
                ]
                    }
                } clause]
                || binding.Name == VariableScopeNames.Wildcard
                || !AttributeLiteral.TryNormalizeConstant(value, out Term constant))
            {
                return false;
            }

            var bindings = new Dictionary<string, Term>(StringComparer.Ordinal) { [binding.Name] = constant };
            var arguments = new List<Expr>(call.Arguments.Count);
            foreach (Expr argument in call.Arguments)
            {
                if (!AttributeLiteral.TryNormalizeConstant(argument, out Term literal, bindings))
                {
                    return false;
                }

                arguments.Add(new Expr.Literal(literal));
            }

            Expr.Call tail = call with { Arguments = arguments };
            owner.EmitLiteralTest(clause, owner.nextLabel - 2);
            owner.EmitCall(
                clause with { Body = tail },
                location,
                tail,
                entries
            );

            return true;
        }

        private bool TryEmitGuardedRemoteTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause]
                || ReadRegisterTypeGuard(clause, allowList: true) is not { Opcode: IsAtom or IsList } guard
                || clause.Body is not Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false
                } tail
                || tail.Arguments.Any(argument => argument is not Expr.Variable
                    && !AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            owner.Op(guard.Opcode, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, guard.Register);
            owner.EmitCall(
                clause,
                location,
                tail,
                entries
            );

            return true;
        }

        private bool TryEmitGuardedIntegerForward(FunctionDefinition function)
        {
            if (function.Arity != 2 || function.Clauses is not [var success, var fallback]
                || success is not
                {
                    Patterns: [Pattern.Variable value, Pattern.Variable saved],
                    Guard: Expr.Call
                    {
                        Function: "is_integer", Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false, Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable forwarded, Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable input]
                        } convert]
                    } tail
                }
                || fallback is not { Guard: null, Patterns: [Pattern.Variable rejected, Pattern.Variable { }] }
                || !HasUnconditionalParameters(fallback) || !DistinctBindings(value, saved)
                || tested.Name != value.Name || input.Name != value.Name || forwarded.Name != saved.Name
                || guard.Module is null && entries.ContainsKey(("is_integer", 1))
                || ReadTaggedExit(fallback.Body, rejected.Name) is not { } error
                || !entries.TryGetValue((convert.Function, 1), out int convertEntry)
                || !entries.TryGetValue((tail.Function, 2), out int tailEntry))
            {
                return false;
            }

            int failure = Label();
            owner.Op(IsInteger, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(convert, location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, convertEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, tailEntry);
            owner.Operand(BeamWire.UnsignedTag, 1);

            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                location,
                framed: false
            );

            return true;
        }

        private bool TryEmitMeasuredEncoder(FunctionDefinition function)
        {
            if (function.Arity != 2 || function.Clauses is not [var first, var second]
                || ReadMeasuredBranch(first, "is_binary", "byte_size") is not { } binary
                || ReadMeasuredBranch(second, "is_list", "length") is not { } list)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int fallback = Label();
            EmitMeasuredTail(
                binary.Measure,
                binary.Entry,
                IsBinary,
                MatchableTypeBits,
                fallback
            );
            Mark(fallback);
            const int listTypeBits = (1 << 2) | (1 << 7); // BEAM_TYPE_CONS | BEAM_TYPE_NIL
            EmitMeasuredTail(
                list.Measure,
                list.Entry,
                IsList,
                listTypeBits,
                functionInfo
            );

            return true;
        }

        private (Expr.Call Measure, int Entry)? ReadMeasuredBranch(Clause clause, string guardName, string measureName)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Variable value, Pattern.Variable tag],
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments:
                        [Expr.Variable first, Expr.Variable second, Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Arguments: [Expr.Variable measured]
                        } measure]
                    } call
                }
                || !DistinctBindings(value, tag) || first.Name != tag.Name || second.Name != value.Name
                || tested.Name != value.Name || measured.Name != value.Name
                || guard.Function != guardName || measure.Function != measureName
                || guard.Module is null && entries.ContainsKey((guardName, 1))
                || measure.Module is null && entries.ContainsKey((measureName, 1))
                || !entries.TryGetValue((call.Function, 3), out int entry))
            {
                return null;
            }

            return (measure, entry);
        }

        private void EmitMeasuredTail(
            Expr.Call measure,
            int entry,
            byte test,
            int typeBits,
            int failure
        )
        {
            owner.Op(test, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(measure, location);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, measure.Function, 1));
            owner.EmitTypedRegister(0, typeBits, unit: test == IsBinary ? BitSyntaxDefaults.BinaryUnit : null);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
        }

        private bool TryEmitSignedDecode(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || !HasUnconditionalParameters(clause)
                || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable bytes, Value: Expr.Call
                            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable a, Expr.Variable b] } read
                        },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable length, Value: Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "byte_size",
                                Arguments: [Expr.Variable measured]
                            } measure
                        },
                            Expr.Match
                        {
                            Pattern: BitPattern
                            {
                                Segments:
                                [
                                    {
                                        Value: Pattern.Variable number, Specification:
                                        {
                                            Type: BitSegmentTypes.Integer,
                                            Unit: BitSyntaxDefaults.BinaryUnit, Signed: true, Endian: BitByteOrders.Big,
                                            Size: Expr.Variable width
                                        }
                                    }]
                            }, Value: Expr.Variable matched
                        } match,
                            Expr.Variable result
                        ]
                    }
                }
                || a.Name != first.Name || b.Name != second.Name
                || measured.Name != bytes.Name || matched.Name != bytes.Name
                || width.Name != length.Name || number.Name != result.Name
                || !DistinctBindings(
                    first,
                    second,
                    bytes,
                    length,
                    number
                )
                || measure.Module is null && entries.ContainsKey(("byte_size", 1))
                || !entries.TryGetValue((read.Function, 2), out int target))
            {
                return false;
            }

            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(read, location);
            owner.EmitFieldCall(2, target);
            owner.EmitCallLine(measure, CallLocation(read, location));
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "byte_size", 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);

            // Successful byte_size proves the input is a bitstring.
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Op(GetBinaryInteger, LabelTag, failure);
            owner.EmitTypedRegister(2, MatchableTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 4); // signed, big-endian
            owner.Operand(XRegisterTag, 1);
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 2);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, owner.Intern("ensure_exactly"));
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitFrameReturn(0);

            Mark(failure);
            var previous = CallLocation(measure, location);
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private bool TryForwardingDispatch(FunctionDefinition function, out bool fallbackEmitted)
        {
            fallbackEmitted = false;
            if (function.Arity != 2 || function.Clauses.Count < 3
                || function.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second] }
                || first.Name == second.Name)
            {
                return false;
            }

            var destinations = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable argument],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input] } call
                    }
                    || argument.Name == VariableScopeNames.Wildcard || argument.Name != input.Name
                    || !entries.TryGetValue((call.Function, 1), out int entry)
                    || !destinations.TryAdd(key.Name, entry))
                {
                    return false;
                }
            }

            var branches = destinations.Reverse().Select(pair => (pair.Key, pair.Value, Label: Label())).ToArray();
            int fallback = Label();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.Operand(AtomTag, owner.Intern(branch.Key));
                owner.Operand(LabelTag, branch.Label);
            }
            foreach (var branch in branches)
            {
                Mark(branch.Label);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, branch.Value);
            }
            Mark(fallback);
            fallbackEmitted = TryEmitDispatchExit(function.Clauses[^1].Body, first.Name);

            return true;
        }

        private bool TryEmitDispatchExit(Expr body, string key)
        {
            if (ReadTaggedExit(body, key) is not { } error)
            {
                return false;
            }
            EmitTaggedExit(
                error.Call,
                error.Tags,
                location,
                framed: false
            );

            return true;
        }

        private (Expr.Call Call, List<Atom> Tags)? ReadTaggedExit(Expr body, string key)
        {
            if (body is not Expr.Call
                {
                    Function: "exit", DynamicRemote: false,
                    Module: null or ErlangModuleNames.Module, Arguments: [var reason]
                } call
                || call.Module is null && entries.ContainsKey(("exit", 1)))
            {
                return null;
            }

            var tags = new List<Atom>();
            while (reason is Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, var inner] })
            {
                tags.Add(tag);
                reason = inner;
            }
            if (tags.Count == 0 || reason is not Expr.Variable variable || variable.Name != key)
            {
                return null;
            }

            return (call, tags);
        }

        private void EmitTaggedExit(
            Expr.Call call,
            List<Atom> tags,
            BeamSourceLocation previous,
            bool framed,
            int live = 1,
            int source = 0,
            int frameSlots = 0
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, tags.Count * 3);
            owner.Operand(BeamWire.UnsignedTag, live);
            foreach (Atom tag in tags.AsEnumerable().Reverse())
            {
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.LiteralOperand(tag);
                owner.Operand(XRegisterTag, source);
                source = 0;
            }
            owner.EmitCallLine(call, previous);
            owner.Op(framed ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "exit", 1));
            if (framed)
            {
                owner.Operand(BeamWire.UnsignedTag, frameSlots);
            }
        }

        private bool TryEmitFixedDecode(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || !HasUnconditionalParameters(clause)
                || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Sequence
                    {
                        Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable value, Value: Expr.Call
                        { Module: null, DynamicRemote: false, Arguments: [Expr.Variable a, Expr.Variable b] } read
                    },
                        Expr.Case { Value: Expr.Variable input } choice
                    ]
                    }
                }
                || a.Name != first.Name || b.Name != second.Name || value.Name != input.Name
                || !DistinctBindings(first, second, value)
                || !entries.TryGetValue((read.Function, 2), out int target)
                || choice.Clauses.Count is not (2 or 3)
                || choice.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }] } fallback
                || ReadTaggedExit(fallback.Body, value.Name) is not { } error
                || ReadFixedBranch(choice.Clauses[0]) is not { } primary)
            {
                return false;
            }

            bool empty = choice.Clauses.Count == 2 && primary.Pattern.Segments.Count == 0;
            int expected = 0;
            Atom? other = null;
            if (!empty)
            {
                if (choice.Clauses.Count != 3
                    || primary.Pattern.Segments is not [{ Value: Pattern.Literal { Value: Integer literal } } firstByte]
                    || !IsByte(firstByte.Specification) || literal.Value < byte.MinValue || literal.Value > byte.MaxValue
                    || ReadFixedBranch(choice.Clauses[1]) is not { } secondary
                    || secondary.Pattern.Segments is not [{ Value: Pattern.Variable { Name: VariableScopeNames.Wildcard } } otherByte]
                    || !IsByte(otherByte.Specification))
                {
                    return false;
                }
                expected = (int)literal.Value;
                other = secondary.Result;
            }

            int alternative = empty ? 0 : Label();
            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(read, location);
            owner.EmitFieldCall(2, target);
            if (empty)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(new BitString([]));
            }
            else
            {
                EmitSingleByteDispatch(expected, alternative, failure);
                EmitExactBinaryTail(alternative);
            }
            owner.EmitConstant(primary.Result, 0);
            owner.EmitFrameReturn(0);
            if (other is not null)
            {
                Mark(alternative);
                EmitExactBinaryTail(failure);
                owner.EmitConstant(other, 0);
                owner.EmitFrameReturn(0);
            }
            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                CallLocation(read, location),
                framed: true
            );

            return true;
        }

        private static (BitPattern Pattern, Atom Result)? ReadFixedBranch(Clause clause) =>
            clause is { Guard: null, Patterns: [BitPattern bits], Body: Expr.Literal { Value: Atom result } }
                ? (bits, result)
                : null;

        private void EmitSingleByteDispatch(int expected, int alternative, int failure)
        {
            const int byteWidth = BitSyntaxDefaults.BinaryUnit;
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 9);
            owner.Operand(AtomTag, owner.Intern("ensure_at_least"));
            owner.Operand(BeamWire.UnsignedTag, byteWidth);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
            owner.Operand(AtomTag, owner.Intern("integer"));
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(BeamWire.UnsignedTag, byteWidth);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
            owner.Operand(XRegisterTag, 2);
            owner.Op(ExactEqualTest, LabelTag, alternative);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.Operand(BeamWire.IntegerTag, expected);
        }

        private void EmitExactBinaryTail(int failure)
        {
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, owner.Intern("ensure_exactly"));
            owner.Operand(BeamWire.UnsignedTag, 0);
        }

        private bool TailLocal(Expr body, bool reuseCallSlots = false, bool caseTail = false)
        {
            if (reuseCallSlots && body is Expr.Case { Value: Expr.Call selector } decision
                && ReadTerminalAtomCase(decision, variables.Keys) is { } terminal)
            {
                Expression(selector);

                return TryCaseSelection(
                    terminal,
                    input: null,
                    returnResult: true,
                    reuseBranchSlots: true,
                    branchContinuation: null,
                    deferredBooleanFailures: null,
                    reservedBooleanLabels: null,
                    allowUnmatchedAtoms: true,
                    terminalAtomTails: true
                );
            }

            if (body is Expr.Try { After: null } protectedBody
                && TryEmitDirectExceptionMatch(protectedBody, reservedToken: null, terminal: true))
            {
                return true;
            }

            Expr last = body is Expr.Sequence sequence ? sequence.Items[^1] : body;
            if (last is Expr.Call thrown && ReadStackFreeThrow(thrown, requireCallerHandler: true) is { } raised)
            {
                if (body is Expr.Sequence throwPrefix)
                {
                    DiscardSequencePrefix(throwPrefix.Items, null, reuseCallSlots);
                }

                EmitStackFreeThrow(raised, tail: true);

                return true;
            }

            if (last is Expr.Binary { Operator: ErlangOperators.Append or ErlangOperators.SubtractList } append
                && (append.Operator == ErlangOperators.SubtractList || AppendPrefix(append.Left) is null))
            {
                if (body is Expr.Sequence appendPrefix)
                {
                    DiscardSequencePrefix(appendPrefix.Items, null, reuseCallSlots);
                }
                EmitAppend(append, tail: true);

                return true;
            }
            if (last is Expr.Call { DynamicRemote: true, Arguments: [_, _, Expr.List { Tail: null }] } dynamic)
            {
                if (body is Expr.Sequence dynamicPrefix)
                {
                    DiscardSequencePrefix(dynamicPrefix.Items, null, reuseCallSlots);
                }
                EmitDynamicRemote(dynamic, tail: true);

                return true;
            }
            if (last is Expr.Apply selfCall && IsClosureSelfCall(selfCall))
            {
                if (body is Expr.Sequence selfPrefix)
                {
                    DiscardSequencePrefix(selfPrefix.Items, null, reuseCallSlots);
                }
                EmitClosureSelfCall(selfCall, tail: true);

                return true;
            }
            if (last is not Expr.Call { Module: null, DynamicRemote: false } call
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int entry))
            {
                return false;
            }

            if (body is Expr.Sequence prefix)
            {
                DiscardSequencePrefix(prefix.Items, null, reuseCallSlots);
            }
            if (caseTail)
            {
                LoadCaseTailArguments(call.Arguments);
            }
            else
            {
                LoadCallArguments(call.Arguments, null, orderSimpleOperands: true);
            }
            // OTP does not emit a call-site line for a local tail call.
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, frameSize);

            return true;
        }

        private void EmitAppend(Expr.Binary append, bool tail)
        {
            int argumentBase = nextSlot;
            var previous = LoadCallArguments([append.Left, append.Right], null,
                callSite: new BeamSourceLocation(location.File, append.SourceLine));
            if (previous.File != location.File)
            {
                previous = location;
            }
            owner.EmitArithmeticLine(append.SourceLine, ref previous);
            owner.Op(tail ? TailExternalCallWithFrame : ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, append.Operator, 2));
            if (tail)
            {
                owner.Operand(BeamWire.UnsignedTag, frameSize);
            }
            nextSlot = Math.Max(argumentBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);
        }

        private bool TryEmitDiscardedDynamicReturn(Expr body)
        {
            if (body is not Expr.Sequence
                {
                    Items: [Expr.Call { DynamicRemote: true } call, var result]
                }
                || ReadForwardedDynamicModule(call, name => variables.ContainsKey(name)
                    && !deferredPairMembers.ContainsKey(name)) is null)
            {
                return false;
            }

            var tail = ReadEntryCaseExternalTail(result);
            if (result is not Expr.Literal && (tail is null || !tail.Value.Call.Arguments.All(
                argument => AttributeLiteral.TryNormalizeConstant(argument, out _))))
            {
                return false;
            }

            EmitDynamicRemote(call, tail: false, trimFrame: true);
            if (tail is { } external)
            {
                var argumentLocation = LoadCallArguments(external.Call.Arguments, null, orderSimpleOperands: true);
                owner.EmitCallLine(external.Call, argumentLocation);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, external.Call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(external.Module, external.Call.Function, external.Call.Arguments.Count));
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else
            {
                owner.EmitConstant(result, 0);
                owner.EmitFrameReturn(0);
            }

            return true;
        }

        private void EmitDynamicRemote(Expr.Call call, bool tail, bool trimFrame = false)
        {
            int argumentBase = nextSlot;
            var arguments = ((Expr.List)call.Arguments[2]).Items;
            var previous = location;
            Expr module = call.Arguments[0];
            Expr function = call.Arguments[1];
            if (ReadForwardedDynamicModule(call, name => variables.ContainsKey(name)
                && !deferredPairMembers.ContainsKey(name)) is { } moduleCall)
            {
                Expression(moduleCall);
                previous = CallLocation(moduleCall, previous);
                foreach (int register in OrderForwardedDynamicMoves(arguments))
                {
                    if (register == arguments.Count)
                    {
                        if (register != 0)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                XRegisterTag,
                                register
                            );
                        }
                    }
                    else
                    {
                        Expr operand = register < arguments.Count ? arguments[register] : function;
                        if (operand is Expr.Variable variable)
                        {
                            Load(variables[variable.Name], register);
                        }
                        else
                        {
                            owner.EmitConstant(operand, register);
                        }
                    }
                }
            }
            else
            {
                Expr[] evaluationOrder = [module, function, .. arguments];
                int?[] slots = evaluationOrder.Select(expression => GuardSlot(expression, null)).ToArray();
                for (int register = 0; register < slots.Length; register++)
                {
                    int index = (register + 2) % slots.Length;
                    if (slots[index] is int slot)
                    {
                        Load(slot, register);
                    }
                    else
                    {
                        owner.EmitConstant(evaluationOrder[index], register);
                    }
                }
            }
            if (trimFrame && frameSize > 0)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frameSize);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            owner.EmitCallLine(call, previous);
            owner.Op(tail ? TailDynamicApply : DynamicApply, BeamWire.UnsignedTag, arguments.Count);
            if (tail)
            {
                owner.Operand(BeamWire.UnsignedTag, frameSize);
            }
            ReleaseOperandSlots(argumentBase);
        }

        private int[] OrderForwardedDynamicMoves(IReadOnlyList<Expr> arguments)
        {
            int[] order = IndependentCallMoveOrder(arguments.Count + 2);
            for (int start = 0; start < order.Length;)
            {
                if (!IsStackMove(order[start]))
                {
                    start++;
                    continue;
                }

                int end = start + 1;
                while (end < order.Length && IsStackMove(order[end]))
                {
                    end++;
                }

                // beam_block sorts each run by its Y source, leaving X0 and other moves in place.
                int[] sorted = order[start..end].Reverse()
                    .OrderBy(register => variables[((Expr.Variable)arguments[register]).Name]).ToArray();
                sorted.CopyTo(order, start);
                start = end;
            }

            return order;

            bool IsStackMove(int register) => register > 0 && register < arguments.Count
                && arguments[register] is Expr.Variable;
        }

        private FunctionDefinition FoldLeadingDynamicModule(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Body: Expr.Sequence sequence } clause] || !HasUnconditionalParameters(clause)
                || sequence.Items is not [
                    Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                    Value: Expr.Call { DynamicRemote: false, Arguments.Count: 0 } producer
                },
                    Expr.Call { DynamicRemote: true, Arguments: [Expr.Variable module, var member, Expr.List { Tail: null } args] } apply,
                    ..
                ] || module.Name != binding.Name)
            {
                return function;
            }

            var parameters = clause.Patterns.OfType<Pattern.Variable>().Select(parameter => parameter.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (parameters.Contains(binding.Name))
            {
                return function;
            }

            var forwarded = apply with { Arguments = [producer, member, args] };
            var rest = sequence.Items.Skip(2).ToArray();
            if (ReadForwardedDynamicModule(forwarded, parameters.Contains) is null
                || rest.Length != 0 && ReadUsedEntryParameters(new Clause([binding], null,
                    sequence with { Items = rest })) is not { Count: 0 })
            {
                return function;
            }

            return function with
            {
                Clauses = [clause with
            {
                Body = rest.Length == 0 ? forwarded : sequence with { Items = [forwarded, .. rest] }
            }]
            };
        }

        private Expr.Call? ReadForwardedDynamicModule(Expr.Call call, Func<string, bool> isBound)
        {
            return call.Arguments is [Expr.Call { DynamicRemote: false } module, var function, Expr.List { Tail: null } arguments]
                && TryResolveOrdinaryCall(module, out _)
                && IsIndependentOperand(function) && arguments.Items.All(IsIndependentOperand)
                ? module : null;

            bool IsIndependentOperand(Expr operand) => operand is Expr.Variable variable
                ? isBound(variable.Name)
                : AttributeLiteral.TryNormalizeConstant(operand, out _);
        }

        private BeamSourceLocation LoadCallArguments(
            IReadOnlyList<Expr> expressions,
            int? failure,
            bool orderSimpleOperands = false,
            BeamSourceLocation? callSite = null,
            (int Index, EntryCall Call)? entryOperand = null
        )
        {
            if (expressions.Any(expression => !ReferenceEquals(FoldLiteralMatch(expression), expression)))
            {
                expressions = expressions.Select(FoldLiteralMatch).ToArray();
            }

            if (failure is null && expressions is [var argument])
            {
                Expression(argument);
                // Direct calls and the constructors below leave the nested call's line active.
                Expr? producer = argument is Expr.List
                {
                    Tail: null, Items: [.., Expr.Tuple
                    { Items: [Expr.Literal { Value: Atom }, var member] }]
                } ? member : argument;
                if (producer is Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module, DynamicRemote: false, SourceLine: > 0
                    } call)
                {
                    return CallLocation(call, location);
                }

                return location;
            }

            if (failure is null && entryOperand is null
                && TryLoadSingleComputedArgument(expressions, out BeamSourceLocation argumentLocation, callSite))
            {
                return argumentLocation;
            }

            if (failure is null && entryOperand is null && TryLoadRecordFieldArguments(expressions))
            {
                return location;
            }

            // No argument evaluation can clobber X0 after the last computed operand.
            // Earlier computed operands still need slots across subsequent evaluation.
            int registerArgument = -1;
            if (failure is null && owner.receiveScratchRegisters is null)
            {
                for (int index = expressions.Count - 1; index >= 0; index--)
                {
                    Expr expression = expressions[index];
                    if (expression is Expr.Variable variable && variables.ContainsKey(variable.Name)
                        || AttributeLiteral.TryNormalizeConstant(expression, out _))
                    {
                        continue;
                    }

                    registerArgument = index;
                    break;
                }
            }

            var incomingRecords = checkedRecordBindings;
            var slots = new int?[expressions.Count];
            for (int index = 0; index < expressions.Count; index++)
            {
                if (entryOperand is { } operand && operand.Index == index)
                {
                    EmitEntryCall(operand.Call);
                    slots[index] = Save();
                }
                else if (index == registerArgument)
                {
                    EmitFinalCallOperand(expressions[index], index);
                }
                else
                {
                    slots[index] = GuardSlot(expressions[index], failure);
                }

                if (failure is null)
                {
                    RememberRecordAccesses(expressions[index]);
                }
            }

            checkedRecordBindings = incomingRecords;
            IEnumerable<int> order = failure is null && (linearBindings || orderSimpleOperands && expressions.All(argument =>
                argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _)))
                ? IndependentCallMoveOrder(slots.Length)
                : Enumerable.Range(0, slots.Length);
            foreach (int index in order)
            {
                if (index == registerArgument)
                {
                    continue;
                }

                if (slots[index] is int slot)
                {
                    Load(slot, index);
                }
                else
                {
                    owner.EmitConstant(expressions[index], index);
                }
            }

            return location;
        }

        private bool TryLoadSingleComputedArgument(IReadOnlyList<Expr> expressions, out BeamSourceLocation argumentLocation,
            BeamSourceLocation? callSite = null)
        {
            argumentLocation = location;
            int computed = FindSingleComputedOperand(expressions, out var operands);
            if (computed < 0)
            {
                return false;
            }

            BeamSourceLocation? lengthLocation = expressions[computed] is Expr.Call { Function: AutoImportNames.Length } length
                ? TryEmitLengthArgument(length, computed) : null;
            if (lengthLocation is null)
            {
                int previousLineCount = owner.lineInstructions;
                EmitFinalCallOperand(expressions[computed], computed);
                argumentLocation = location;
                if (expressions[computed] is Expr.Call
                    { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false, SourceLine: > 0 } call
                    && owner.lineInstructions != previousLineCount
                    && owner.lastEmittedLine == CallLocation(call, location)
                    && owner.lastEmittedLine == callSite)
                {
                    argumentLocation = owner.lastEmittedLine;
                }
            }

            IEnumerable<int> order = linearBindings || lengthLocation.HasValue
                ? IndependentCallMoveOrder(expressions.Count)
                : Enumerable.Range(0, expressions.Count);
            foreach (int index in order)
            {
                if (index == computed)
                {
                    continue;
                }

                var (slot, constant) = operands[index];
                if (slot is int register)
                {
                    Load(register, index);
                }
                else
                {
                    owner.EmitConstant(constant!, index);
                }
            }

            argumentLocation = lengthLocation ?? argumentLocation;

            return true;
        }

        private BeamSourceLocation? TryEmitLengthArgument(Expr.Call call, int destination)
        {
            call = owner.ResolveImportedCall(call, entries);
            if (!IsGuardBif(call, AutoImportNames.Length) || !owner.ResolvesErlangBif(call))
            {
                return null;
            }

            int operandBase = nextSlot;
            Expr operand = call.Arguments[0];
            int? slot = GuardSlot(operand, null);
            owner.EmitCallLine(call, location);
            EmitSizeInstruction(call.Function, 0, ReceiveGuardLiveRegisters);
            GuardOperand(operand, slot);
            owner.Operand(XRegisterTag, destination);
            ReleaseOperandSlots(operandBase);

            return CallLocation(call, location);
        }

        private int FindSingleComputedOperand(
            IReadOnlyList<Expr> expressions,
            out (int? Slot, Term? Constant)[] operands)
        {
            int computed = -1;
            operands = new (int? Slot, Term? Constant)[expressions.Count];
            for (int index = 0; index < expressions.Count; index++)
            {
                Expr expression = expressions[index];
                if (expression is Expr.Variable variable && variables.TryGetValue(variable.Name, out int slot))
                {
                    operands[index] = (slot, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    operands[index] = (null, constant);
                }
                else
                {
                    if (computed >= 0)
                    {
                        return -1;
                    }

                    computed = index;
                }
            }

            return computed;
        }

        private bool TryEmitTupleWithRegisterMember(Expr.Tuple tuple)
        {
            int computed = FindSingleComputedOperand(tuple.Items, out var operands);
            if (computed < 0)
            {
                return false;
            }

            Expression(tuple.Items[computed]);
            EmitTupleWithX0(operands);

            return true;
        }

        private bool TryEmitEntryCallComparison(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || ReadEntryComparison(clause.Body, out bool branchOnEqual) is not { } comparison)
            {
                return false;
            }

            bool callOnLeft = comparison.Left is Expr.Call;
            Expr computed = callOnLeft ? comparison.Left : comparison.Right;
            Expr operand = callOnLeft ? comparison.Right : comparison.Left;
            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (computed is not Expr.Call call || ReadEntryCall(call, parameters) is not { } plan)
            {
                return false;
            }

            int source = operand is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                ? Array.IndexOf(parameters, variable.Name)
                : -1;
            if (source < 0 && !AttributeLiteral.TryNormalizeConstant(operand, out _))
            {
                return false;
            }

            int slots = source >= 0 ? 1 : 0;
            EmitFrameAllocation(parameters.Length, slots);
            if (source >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    YRegisterTag,
                    0
                );
            }

            EmitEntryCall(plan);
            if (branchOnEqual)
            {
                int unequal = Label();
                owner.Op(ExactEqualTest, LabelTag, unequal);
                owner.Operand(XRegisterTag, 0);
                GuardOperand(operand, 0);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), 0);
                owner.EmitFrameReturn(slots);
                Mark(unequal);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.True), 0);
                owner.EmitFrameReturn(slots);

                return true;
            }

            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, comparison.Operator, 2));
            if (callOnLeft)
            {
                owner.Operand(XRegisterTag, 0);
                GuardOperand(operand, source >= 0 ? 0 : null);
            }
            else
            {
                GuardOperand(operand, source >= 0 ? 0 : null);
                owner.Operand(XRegisterTag, 0);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots);

            return true;
        }

        private static Expr.Binary? ReadEntryComparison(Expr expression, out bool branchOnEqual)
        {
            branchOnEqual = false;
            if (expression is Expr.Binary { Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual } binary)
            {
                return binary;
            }

            if (expression is not Expr.Case
                {
                    Value: Expr.Call call,
                    Clauses:
                    [
                    {
                        Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } expected], Guard: null,
                        Body: Expr.Literal { Value: Atom match }
                    },
                    {
                        Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null,
                        Body: Expr.Literal { Value: Atom fallback }
                    }
                    ]
                })
            {
                return null;
            }

            // The caller verifies that the pattern variable was bound on entry.
            branchOnEqual = (match.Name, fallback.Name) == (ErlangBooleanAtoms.False, ErlangBooleanAtoms.True);
            if (!branchOnEqual && (match.Name, fallback.Name) != (ErlangBooleanAtoms.True, ErlangBooleanAtoms.False))
            {
                return null;
            }

            return new Expr.Binary(ErlangOperators.ExactEqual, call, new Expr.Variable(expected.Name));
        }

        private bool TryEmitEntryCallTuple(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call { DynamicRemote: false } call
                    },
                        Expr.Tuple tuple
                    ]
                }
                || !HasSimpleDecodeArguments(call))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(result.Name, StringComparer.Ordinal) || ReadEntryCall(call, parameters) is not { } plan)
            {
                return false;
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            var copies = new List<(int Source, int Slot)>();
            for (int source = parameters.Length - 1; source >= 0; source--)
            {
                string name = parameters[source];
                if (name != VariableScopeNames.Wildcard
                    && tuple.Items.Any(item => item is Expr.Variable variable && variable.Name == name))
                {
                    int slot = saved.Count;
                    saved.Add(name, slot);
                    copies.Add((source, slot));
                }
            }

            if (!TryReadBoundTupleOperands(
                tuple,
                result.Name,
                saved,
                out var operands
            ))
            {
                return false;
            }

            EmitFrameAllocation(parameters.Length, saved.Count);
            foreach (var (source, slot) in copies)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(plan);
            EmitTupleWithX0(operands);
            owner.EmitFrameReturn(saved.Count);

            return true;
        }

        private bool TryEmitEntryCallTupleTail(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: Expr.Tuple tuple
                    },
                        Expr.Call { Module: null, DynamicRemote: false } tail
                    ]
                }
                || !entries.TryGetValue((tail.Function, tail.Arguments.Count), out int entry))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var calls = tuple.Items.OfType<Expr.Call>().ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || calls is not [var call] || ReadEntryCall(call, parameters) is not { } plan)
            {
                return false;
            }

            int destination = -1;
            var stackMoves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            var copies = new List<int>();
            var tailNames = tail.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (Expr.Variable member in tuple.Items.OfType<Expr.Variable>())
            {
                if (!tailNames.Contains(member.Name))
                {
                    SaveParameter(member.Name);
                }
            }

            for (int index = tail.Arguments.Count - 1; index >= 0; index--)
            {
                Expr argument = tail.Arguments[index];
                if (argument is Expr.Variable variable && variable.Name == binding.Name && destination < 0)
                {
                    destination = index;
                }
                else if (argument is Expr.Variable saved && SaveParameter(saved.Name))
                {
                    stackMoves.Add((index, bindings[saved.Name]));
                }
                else if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    constants.Add((index, constant));
                }
                else
                {
                    return false;
                }
            }

            const string result = "\0tuple-tail";
            var prepared = tuple with
            {
                Items = tuple.Items.Select(item => ReferenceEquals(item, call) ? new Expr.Variable(result) : item).ToArray()
            };
            if (destination < 0 || !TryReadBoundTupleOperands(
                prepared,
                result,
                bindings,
                out var operands
            ))
            {
                return false;
            }

            EmitFrameAllocation(parameters.Length, copies.Count);
            for (int slot = 0; slot < copies.Count; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    copies[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(plan);
            EmitTupleWithX0(operands, destination);
            owner.EmitOrderedTailMoves([], constants, stackMoves);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, tail.Arguments.Count);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, copies.Count);

            return true;

            bool SaveParameter(string name)
            {
                int source = name == VariableScopeNames.Wildcard ? -1 : Array.IndexOf(parameters, name);
                if (source < 0)
                {
                    return false;
                }

                if (bindings.TryAdd(name, copies.Count))
                {
                    copies.Add(source);
                }

                return true;
            }
        }

        private bool TryEmitEntryTupleAndCallResult(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                    Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call read },
                    Expr.Call { DynamicRemote: false, Arguments: [Expr.Tuple tuple, Expr.Variable forwarded] } finish
                ]
                }
                || result.Name == VariableScopeNames.Wildcard || forwarded.Name != result.Name)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(result.Name, StringComparer.Ordinal) || ReadEntryCall(read, parameters) is not { } plan)
            {
                return false;
            }

            int? local = finish.Module is null && entries.TryGetValue((finish.Function, 2), out int entry) ? entry : null;
            string? module = local.HasValue ? null : finish.Module ?? owner.ResolveExternalModule(finish.Function, 2);
            if (!local.HasValue && (module is null or ErlangModuleNames.Module))
            {
                return false;
            }

            var members = tuple.Items.OfType<Expr.Variable>().Select(member => member.Name).ToHashSet(StringComparer.Ordinal);
            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                if (parameters[index] != VariableScopeNames.Wildcard && members.Contains(parameters[index]))
                {
                    saved.Add(parameters[index], saved.Count);
                }
            }

            if (!TryReadBoundTupleOperands(
                tuple,
                result.Name,
                saved,
                out var operands,
                requireResult: false
            )
                || operands.Any(operand => operand is (null, null)))
            {
                return false;
            }

            EmitFrameAllocation(parameters.Length, saved.Count);
            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                if (saved.TryGetValue(parameters[index], out int slot))
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        index,
                        YRegisterTag,
                        slot
                    );
                }
            }

            EmitEntryCall(plan);
            EmitTupleWithX0(operands, forwardX0: true);
            if (local is int localEntry)
            {
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, localEntry);
            }
            else
            {
                owner.EmitCallLine(finish, CallLocation(read, location));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module!, finish.Function, 2));
            }

            owner.Operand(BeamWire.UnsignedTag, saved.Count);

            return true;
        }

        private Expr.Sequence? ReadEntryTupleSequence(Expr body, string[] parameters)
        {
            IReadOnlyList<Expr> sequence = body is Expr.Sequence block ? block.Items : [body];
            Expr? first = sequence.FirstOrDefault();
            Expr? selector = first is Expr.Match match ? match.Value : first;
            Expr? value = selector is Expr.Case decision ? decision.Value : selector;
            if (value is not Expr.Tuple tuple)
            {
                return null;
            }

            var prefix = new List<Expr>();
            var items = new Expr[tuple.Items.Count];
            for (int index = 0; index < items.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Call call && ReadEntryCall(call, parameters) is not null)
                {
                    // Source variables cannot contain a NUL character.
                    string temporary = $"\0tuple{index}";
                    prefix.Add(new Expr.Match(new Pattern.Variable(temporary), call));
                    items[index] = new Expr.Variable(temporary);
                }
                else if (item is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard
                    && parameters.Contains(variable.Name, StringComparer.Ordinal)
                    || AttributeLiteral.TryNormalizeConstant(item, out _))
                {
                    items[index] = item;
                }
                else
                {
                    return null;
                }
            }

            if (prefix.Count < 2)
            {
                return null;
            }

            Expr rewritten = new Expr.Tuple(items);
            if (selector is Expr.Case @case)
            {
                rewritten = @case with { Value = rewritten };
            }
            if (first is Expr.Match binding)
            {
                rewritten = binding with { Value = rewritten };
            }

            prefix.Add(rewritten);
            prefix.AddRange(sequence.Skip(1));

            return new Expr.Sequence(prefix);
        }

        private sealed record EntryCall(
            Expr.Call Call,
            string? Module,
            List<(int Destination, int Source)> Moves,
            List<(int Destination, Term Value)> Constants,
            List<(int Destination, int Source)>? SavedInputs = null,
            EntryConstructedArgument? ConstructedArgument = null,
            bool ConstructAfterMoves = false,
            (Expr.FunctionReference Reference, int Entry, int Destination, bool KeepsResult)? FunctionArgument = null,
            CallArgumentConstructions? RegisterConstructions = null
        );

        private sealed record EntryConstructedArgument(int Register, bool IsTuple, (int? Source, Term? Constant)[] Operands)
        {
            internal int HeapWords => IsTuple ? Operands.Length + 1 : 2;
        }

        private static EntryConstructedArgument? ReadEntryConstructedArgument(Expr expression, int destination, string[] parameters)
        {
            if (expression is Expr.List { Items: [Expr.Variable headVariable], Tail: Expr.Variable tailVariable }
                && headVariable.Name != VariableScopeNames.Wildcard && tailVariable.Name != VariableScopeNames.Wildcard
                && Array.IndexOf(parameters, headVariable.Name) is int headRegister && headRegister >= 0
                && Array.IndexOf(parameters, tailVariable.Name) is int tailRegister && tailRegister >= 0)
            {
                return new(destination, IsTuple: false, [(headRegister, null), (tailRegister, null)]);
            }

            if (expression is Expr.List { Items.Count: > 0 } list
                && list.Items[0] is Expr.Variable head && head.Name != VariableScopeNames.Wildcard
                && Array.IndexOf(parameters, head.Name) is int source && source >= 0
                && AttributeLiteral.TryNormalizeConstant(new Expr.List(list.Items.Skip(1).ToArray(), list.Tail), out Term tail))
            {
                return new(destination, IsTuple: false, [(source, null), (null, tail)]);
            }

            if (expression is not Expr.Tuple tuple || !tuple.Items.Any(item => item is Expr.Variable))
            {
                return null;
            }

            var operands = new (int? Source, Term? Constant)[tuple.Items.Count];
            for (int index = 0; index < operands.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard
                    && Array.IndexOf(parameters, variable.Name) is int register && register >= 0)
                {
                    operands[index] = (register, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    operands[index] = (null, constant);
                }
                else
                {
                    return null;
                }
            }

            return new(destination, IsTuple: true, operands);
        }

        private EntryCall? ReadEntryCall(
            Expr.Call call,
            string[] parameters,
            string[]? savedParameters = null,
            bool allowTermToBinary = false,
            bool allowConstructedArgument = false,
            bool allowFunctionArgument = false,
            bool allowControlTransfer = false,
            int preparedOperand = -1
        )
        {
            string? module;
            int preparedArgument = preparedOperand;
            EntryConstructedArgument? constructed = null;
            (Expr.FunctionReference Reference, int Entry, int Destination, bool KeepsResult)? functionArgument = null;
            if (allowFunctionArgument && preparedArgument < 0)
            {
                for (int index = 0; index < call.Arguments.Count; index++)
                {
                    if (call.Arguments[index] is Expr.FunctionReference reference
                        && entries.TryGetValue((reference.Name, reference.Arity), out int entry))
                    {
                        bool keepsResult = parameters.Length == 1 && call.Arguments.OfType<Expr.Variable>()
                            .Any(argument => argument.Name == parameters[0]);
                        functionArgument = (reference, entry, index, keepsResult);
                        preparedArgument = index;
                        break;
                    }
                }
            }

            if (allowConstructedArgument && preparedArgument < 0)
            {
                for (int index = 0; index < call.Arguments.Count; index++)
                {
                    constructed = ReadEntryConstructedArgument(call.Arguments[index], index, parameters);
                    if (constructed is not null)
                    {
                        preparedArgument = index;
                        break;
                    }
                }
            }

            if (allowTermToBinary && IsTermToBinaryCall(call) && HasSimpleDecodeArguments(call))
            {
                module = ErlangModuleNames.Module;
            }
            else if (!TryResolveOrdinaryCall(
                call,
                out module,
                preparedArgument,
                allowControlTransfer
            ))
            {
                return null;
            }
            int arity = call.Arguments.Count;
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            List<(int Destination, int Source)>? saved = null;
            for (int destination = 0; destination < arity; destination++)
            {
                if (destination == preparedArgument)
                {
                    continue;
                }
                if (call.Arguments[destination] is Expr.Variable variable)
                {
                    int source = Array.IndexOf(parameters, variable.Name);
                    if (variable.Name == VariableScopeNames.Wildcard)
                    {
                        return null;
                    }

                    if (source < 0)
                    {
                        int savedSource = savedParameters is null ? -1 : Array.IndexOf(savedParameters, variable.Name);
                        if (savedSource < 0)
                        {
                            return null;
                        }

                        (saved ??= []).Add((destination, savedSource));
                    }
                    else if (source != destination)
                    {
                        moves.Add((destination, source));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(call.Arguments[destination], out Term constant))
                {
                    constants.Add((destination, constant));
                }
            }

            if (constructed is not null && moves.Any(move => move.Source == preparedArgument))
            {
                int scratch = Math.Max(parameters.Length, arity);
                if (scratch >= FirstReservedXRegister)
                {
                    return null;
                }
                constructed = constructed with { Register = scratch };
                moves.Add((preparedArgument, scratch));
            }

            return new EntryCall(
                call,
                module,
                moves,
                constants,
                saved,
                constructed,
                FunctionArgument: functionArgument
            );
        }

        private void EmitEntryCall(EntryCall entry, BeamSourceLocation? previous = null)
        {
            EmitEntryCallArguments(entry);
            owner.EmitCallLine(entry.Call, previous ?? location);
            Call(entry.Module, entry.Call.Function, entry.Call.Arguments.Count);
        }

        private void EmitEntryCallArguments(EntryCall entry)
        {
            if (entry.ConstructAfterMoves)
            {
                owner.EmitOrderedTailMoves(entry.Moves, entry.Constants);
            }

            if (entry.ConstructedArgument is { } argument)
            {
                EmitEntryConstruction(argument);
            }

            if (!entry.ConstructAfterMoves)
            {
                owner.EmitOrderedTailMoves(entry.Moves, entry.Constants);
            }
        }

        private void EmitEntryConstruction(EntryConstructedArgument argument)
        {
            if (argument.IsTuple)
            {
                owner.EmitTupleHeader(XRegisterTag, argument.Operands.Length, argument.Register);
            }
            else
            {
                owner.instructions.WriteByte(PutList);
            }

            foreach (var (source, constant) in argument.Operands)
            {
                if (source is int register)
                {
                    owner.Operand(XRegisterTag, register);
                }
                else
                {
                    owner.LiteralOperand(constant!);
                }
            }

            if (!argument.IsTuple)
            {
                owner.Operand(XRegisterTag, argument.Register);
            }
        }

        private bool TryResolveOrdinaryCall(
            Expr.Call call,
            out string? module,
            int preparedArgument = -1,
            bool allowControlTransfer = false
        )
        {
            return TryResolveOrdinaryCallTarget(call, out module, allowControlTransfer)
                && call.Arguments.Where((_, index) => index != preparedArgument)
                    .All(argument => argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _));
        }

        private bool TryResolveOrdinaryCallTarget(Expr.Call call, out string? module, bool allowControlTransfer = false)
        {
            int arity = call.Arguments.Count;
            bool local = call.Module is null && entries.ContainsKey((call.Function, arity));
            module = local ? null : call.Module ?? owner.ResolveExternalModule(call.Function, arity);

            return !call.DynamicRemote && (local || module is not null
                    && (module != ErlangModuleNames.Module
                        || allowControlTransfer && IsControlTransfer(call.Function, arity)
                        || (call.Function, arity) is (SourceLoadNifNames.Function, 2)
                        || (call.Function, arity) is (AutoImportNames.Put or AutoImportNames.Monitor, 2) or ("function_exported", 3)
                            or (AutoImportNames.Statistics, 1)
                            or (AutoImportNames.TupleToList or AutoImportNames.ListToTuple
                                or AutoImportNames.AtomToList or AutoImportNames.ListToAtom
                                or AutoImportNames.IntegerToList or AutoImportNames.ListToInteger or AutoImportNames.IolistSize
                                or AutoImportNames.ListToBinary or AutoImportNames.IolistToBinary
                                or AutoImportNames.BinaryToList, 1)
                            or (AutoImportNames.IntegerToList or AutoImportNames.ListToInteger, 2)
                            or (ErlangOperators.Append, 2)
                            or (AutoImportNames.BinaryToList, 3)));
        }

        private static bool IsControlTransfer(string function, int arity) =>
            (function, arity) is (AutoImportNames.Halt, 0 or 1 or 2)
                or (AutoImportNames.Exit or AutoImportNames.Throw, 1)
                or (AutoImportNames.Error, 1 or 2 or 3);

        private sealed record ForwardedCall(
            Expr.Call Call,
            string? Module,
            string Input,
            string? Result,
            Expr.Sequence? Continuation,
            string? ReusedInput = null,
            int ConstructedArgument = -1,
            (string? Name, Term? Constant)[]? ReturnedTuple = null
        );

        private ForwardedCall? ReadForwardedCall(Expr.Sequence sequence, string input, string[] parameters)
        {
            Expr first = sequence.Items[0];
            var call = first as Expr.Call;
            Pattern.Variable? result = null;
            (string? Name, Term? Constant)[]? returnedTuple = null;
            if (sequence.Items is [Expr.Tuple tupleResult] && tupleResult.Items.OfType<Expr.Call>().ToArray() is [var nested])
            {
                returnedTuple = new (string?, Term?)[tupleResult.Items.Count];
                for (int index = 0; index < returnedTuple.Length; index++)
                {
                    Expr item = tupleResult.Items[index];
                    if (ReferenceEquals(item, nested))
                    {
                        continue;
                    }

                    if (item is Expr.Variable variable && variable.Name != input && IsBound(variable))
                    {
                        returnedTuple[index] = (variable.Name, null);
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                    {
                        returnedTuple[index] = (null, constant);
                    }
                    else
                    {
                        return null;
                    }
                }

                call = nested;
            }

            if (sequence.Items.Count > 1 && first is Expr.Match { Pattern: Pattern.Variable target, Value: Expr.Call value }
                && target.Name != input && !parameters.Contains(target.Name, StringComparer.Ordinal))
            {
                call = value;
                result = target;
            }

            if (call is null)
            {
                return null;
            }

            bool consumesResult = call.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == input);
            int constructedArgument = -1;
            if (sequence.Items.Count == 1)
            {
                for (int index = 0; index < call.Arguments.Count && index < FirstReservedXRegister; index++)
                {
                    Expr argument = call.Arguments[index];
                    bool listTail = index != 0 && argument is Expr.List { Items.Count: > 0, Tail: Expr.Variable tail } list
                        && tail.Name == input && list.Items.All(item => item is Expr.Variable head && IsBound(head));
                    bool tupleTail = (index != 0 || !consumesResult) && argument is Expr.Tuple tuple
                        && tuple.Items.OfType<Expr.Variable>().Any(field => field.Name == input)
                        && tuple.Items.All(field => field is Expr.Variable variable && IsBound(variable)
                            || AttributeLiteral.TryNormalizeConstant(field, out _));
                    if (listTail || tupleTail)
                    {
                        constructedArgument = index;
                        break;
                    }
                }
            }

            if (!TryResolveOrdinaryCall(call, out string? module, constructedArgument)
                || call.Arguments.OfType<Expr.Variable>().Any(argument => !IsBound(argument)))
            {
                return null;
            }

            if (sequence.Items.Count == 1)
            {
                return consumesResult || constructedArgument >= 0
                    ? new ForwardedCall(
                        call,
                        module,
                        input,
                        null,
                        null,
                        ConstructedArgument: constructedArgument,
                        ReturnedTuple: returnedTuple
                    )
                    : null;
            }

            var continuation = new Expr.Sequence(sequence.Items.Skip(1).ToArray());
            var bindings = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToList();
            bindings.Add(new Pattern.Variable(input));
            if (result is not null)
            {
                bindings.Add(result);
            }

            if (ReadUsedEntryParameters(new Clause(bindings, null, continuation)) is not { } used)
            {
                return null;
            }

            string? output = result is not null && used.Contains(result.Name) ? result.Name : null;
            if (consumesResult && !used.Contains(input))
            {
                return new ForwardedCall(
                    call,
                    module,
                    input,
                    output,
                    continuation
                );
            }

            if (!consumesResult && used.Contains(input)
                && call.Arguments is [Expr.Variable firstArgument, ..]
                && !used.Contains(firstArgument.Name)
                && !call.Arguments.Skip(1).OfType<Expr.Variable>().Any(argument => argument.Name == firstArgument.Name))
            {
                return new ForwardedCall(
                    call,
                    module,
                    input,
                    output,
                    continuation,
                    firstArgument.Name
                );
            }

            return null;

            bool IsBound(Expr.Variable variable) =>
                variable.Name != VariableScopeNames.Wildcard
                && (variable.Name == input || parameters.Contains(variable.Name, StringComparer.Ordinal));
        }

        private bool EmitForwardedCall(ForwardedCall plan, BeamSourceLocation previousCall)
        {
            int arity = plan.Call.Arguments.Count;
            var registers = new List<(int Destination, int Source)>();
            var stack = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            int firstArgument = 0;
            if (plan.ReusedInput is { } reused)
            {
                int slot = variables[reused];
                owner.Op(Swap, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                variables.Remove(reused);
                variables.Add(plan.Input, slot);
                firstArgument = 1;
            }

            Expr? constructed = plan.ConstructedArgument >= 0 ? plan.Call.Arguments[plan.ConstructedArgument] : null;
            if (constructed is Expr.Tuple tupleArgument
                && TryReadBoundTupleOperands(
                    tupleArgument,
                    plan.Input,
                    variables,
                    out var tupleOperands
                ))
            {
                EmitTupleWithX0(tupleOperands, plan.ConstructedArgument);
            }
            else if (constructed is Expr.List list)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, list.Items.Count * ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                int tailRegister = 0;
                for (int index = list.Items.Count - 1; index >= 0; index--)
                {
                    string head = ((Expr.Variable)list.Items[index]).Name;
                    if (head == plan.Input)
                    {
                        owner.Op(PutList, XRegisterTag, 0);
                    }
                    else
                    {
                        owner.Op(PutList, YRegisterTag, variables[head]);
                    }

                    owner.Operand(XRegisterTag, tailRegister);
                    owner.Operand(XRegisterTag, plan.ConstructedArgument);
                    tailRegister = plan.ConstructedArgument;
                }
            }

            for (int index = firstArgument; index < arity; index++)
            {
                if (plan.Call.Arguments[index] is Expr.Variable variable)
                {
                    if (variable.Name != plan.Input)
                    {
                        stack.Add((index, variables[variable.Name]));
                    }
                    else if (index != 0)
                    {
                        registers.Add((index, 0));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(plan.Call.Arguments[index], out Term constant))
                {
                    constants.Add((index, constant));
                }
            }

            owner.EmitOrderedTailMoves(registers, constants, stack);
            if (plan.ReturnedTuple is { } returned)
            {
                owner.EmitCallLine(plan.Call, previousCall);
                Call(plan.Module, plan.Call.Function, arity);
                EmitTupleWithX0(returned.Select(operand =>
                    (operand.Name is null ? (int?)null : variables[operand.Name], operand.Constant)).ToArray());
                owner.EmitFrameReturn(frameSize);

                return true;
            }

            if (plan.Continuation is null)
            {
                if (plan.Module is null)
                {
                    owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, arity);
                    owner.Operand(LabelTag, entries[(plan.Call.Function, arity)]);
                }
                else
                {
                    owner.EmitCallLine(plan.Call, previousCall);
                    owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, arity);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(plan.Module, plan.Call.Function, arity));
                }

                owner.Operand(BeamWire.UnsignedTag, frameSize);

                return true;
            }

            owner.EmitCallLine(plan.Call, previousCall);
            Call(plan.Module, plan.Call.Function, arity);
            if (plan.Result is { } result)
            {
                if (plan.Continuation.Items is [Expr.Tuple tuple]
                    && TryReadBoundTupleOperands(
                        tuple,
                        result,
                        variables,
                        out var operands
                    ))
                {
                    EmitTupleWithX0(operands);
                    owner.EmitFrameReturn(frameSize);

                    return true;
                }

                if (plan.Continuation.Items is [Expr.Case decision]
                    && TryEmitPreparedTupleCase(decision, x0Binding: result, returnResult: true))
                {
                    return true;
                }

                variables.Add(result, Save());
            }

            return false;
        }

        private bool TryEmitBoundCallTuple(Expr binding, Expr continuation)
        {
            if (binding is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                    Value: Expr.Call call
                }
                || variables.ContainsKey(result.Name)
                || !HasSimpleDecodeArguments(call)
                || continuation is not Expr.Tuple tuple)
            {
                return false;
            }

            if (!TryReadBoundTupleOperands(
                tuple,
                result.Name,
                variables,
                out var operands
            ))
            {
                return false;
            }

            Expression(call);
            EmitTupleWithX0(operands);

            return true;
        }

        private Expr.Sequence? TryEmitFirstResultTupleBinding(Pattern.Variable firstResult, Expr.Sequence continuation)
        {
            if (owner.receiveScratchRegisters is not null
                || continuation.Items is not
                    [Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, .., Expr.Case]
                || binding.Name == firstResult.Name || binding.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(binding.Name) || deferredPairMembers.ContainsKey(binding.Name))
            {
                return null;
            }

            var tuple = value is Expr.RecordConstruction record ? RecordExpressions.Create(record) : value as Expr.Tuple;
            if (tuple is null || !TryReadBoundTupleOperands(
                tuple,
                firstResult.Name,
                variables,
                out var operands
            ))
            {
                return null;
            }

            var rest = continuation with { Items = continuation.Items.Skip(1).ToArray() };
            var tracked = variables.Keys.Select(name => (Pattern)new Pattern.Variable(name)).Append(firstResult).Append(binding).ToArray();
            if (ReadUsedEntryParameters(new Clause(tracked, null, rest)) is not { } used
                || !used.Contains(binding.Name))
            {
                return null;
            }

            foreach (var (name, slot) in variables.OrderByDescending(item => item.Value))
            {
                if (used.Contains(name) || variables.Any(alias => alias.Key != name && alias.Value == slot)
                    || deferredPairMembers.Values.Any(member => member.Slot == slot))
                {
                    continue;
                }

                EmitTupleWithX0(operands, slot, destinationTag: YRegisterTag);
                variables.Remove(name);
                variables.Add(binding.Name, slot);
                if (used.Contains(firstResult.Name))
                {
                    variables.Add(firstResult.Name, Save());
                }

                return rest;
            }

            return null;
        }

        private static bool TryReadBoundTupleOperands(
            Expr.Tuple tuple,
            string? result,
            Dictionary<string, int> bindings,
            out (int? Slot, Term? Constant)[] operands,
            bool requireResult = true
        )
        {
            operands = new (int? Slot, Term? Constant)[tuple.Items.Count];
            bool usesResult = false;
            for (int index = 0; index < operands.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Variable variable && variable.Name == result)
                {
                    usesResult = true;
                }
                else if (item is Expr.Variable saved && bindings.TryGetValue(saved.Name, out int slot))
                {
                    operands[index] = (slot, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    operands[index] = (null, constant);
                }
                else
                {
                    return false;
                }
            }

            return usesResult || !requireResult;
        }

        private void EmitTupleWithX0(
            (int? Slot, Term? Constant)[] operands,
            int destination = 0,
            bool forwardX0 = false,
            bool usesX0 = true,
            int destinationTag = XRegisterTag
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, operands.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(usesX0 ? 1 : 0, ReceiveGuardLiveRegisters));
            if (forwardX0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }

            owner.Op(PutTuple, destinationTag, destination);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, operands.Length);
            for (int index = 0; index < operands.Length; index++)
            {
                if (operands[index] is (null, null))
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else if (operands[index].Slot is int register)
                {
                    owner.Operand(YRegisterTag, register);
                }
                else
                {
                    owner.LiteralOperand(operands[index].Constant!);
                }
            }
        }

        private void Guard(Expr? guard, int failure, string? selector = null)
        {
            if (guard is null or Expr.Literal { Value: Atom { Name: "true" } })
            {
                return;
            }
            if (guard is Expr.GuardAlternatives alternatives)
            {
                int accepted = Label();
                foreach (Expr item in alternatives.Items)
                {
                    int next = Label();
                    Guard(item, next, selector);
                    JumpTo(accepted);
                    Mark(next);
                }
                JumpTo(failure);
                Mark(accepted);

                return;
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
            {
                Guard(conjunction.Left, failure, selector);
                Guard(conjunction.Right, failure, selector);

                return;
            }
            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } disjunction
                && (owner.receiveScratchRegisters is not null || IsTotalTypeGuard(disjunction.Left)))
            {
                int right = Label();
                int accepted = Label();
                Guard(disjunction.Left, right, selector);
                JumpTo(accepted);
                Mark(right);
                Guard(disjunction.Right, failure, selector);
                Mark(accepted);

                return;
            }
            if (guard is Expr.Unary { Operator: ErlangOperators.Not } negation)
            {
                Expression(negation.Operand, failure);
                owner.EmitAtomTest(0, "false", failure);

                return;
            }
            if (guard is Expr.Call arityCall && IsFunctionArityCall(arityCall)
                && arityCall.Arguments[1] is Expr.Literal { Value: Integer arity }
                && arity.Value >= 0 && arity.Value <= byte.MaxValue)
            {
                int? slot = GuardSlot(arityCall.Arguments[0], failure);
                owner.Op(IsFunctionWithArity, LabelTag, failure);
                GuardOperand(arityCall.Arguments[0], slot);
                owner.LiteralOperand(arity);

                return;
            }
            if (ReadTupleArityGuard(guard) is { } arityGuard)
            {
                Expression(arityGuard.Value, failure);
                EmitTupleArityGuard(0, arityGuard.Arity, failure);

                return;
            }
            if (guard is Expr.Binary comparison && TryGuardComparison(comparison, failure, selector))
            {
                return;
            }
            if (owner.ReadRecordTest(guard) is { } record)
            {
                Expression(record.Value, failure);
                owner.EmitRecordTest(
                    failure,
                    XRegisterTag,
                    0,
                    record.Name,
                    record.Arity
                );

                return;
            }
            if (guard is Expr.Call range && IsIntegerRangeCall(range)
                && range.Arguments is [var value, Expr.Literal { Value: Integer lower }, Expr.Literal { Value: Integer upper }]
                && lower.Value >= long.MinValue && upper.Value <= long.MaxValue && lower.Value <= upper.Value)
            {
                Expression(value, failure);
                EmitIntegerRangeTests(failure, (long)lower.Value, (long)upper.Value);

                return;
            }
            if (guard is Expr.Call { DynamicRemote: false, Arguments: [var argument] } test
                && test.Module is null or ErlangModuleNames.Module
                && (test.Module is not null || !entries.ContainsKey((test.Function, 1))))
            {
                byte opcode = TypeGuardOpcode(test);
                if (opcode != 0)
                {
                    int? slot = GuardSlot(argument, failure, selector);
                    owner.Op(opcode, LabelTag, failure);
                    GuardOperand(argument, slot, selector);

                    return;
                }
            }
            Expression(guard, failure);
            owner.EmitAtomTest(0, "true", failure);
        }

        private bool IsTotalTypeGuard(Expr guard) => guard switch
        {
            Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse } binary =>
                IsTotalTypeGuard(binary.Left) && IsTotalTypeGuard(binary.Right),
            Expr.Call
            {
                DynamicRemote: false,
                Module: null or ErlangModuleNames.Module,
                Arguments: [Expr.Variable or Expr.Literal]
            } call => owner.ResolvesErlangBif(call) && TypeGuardOpcode(call) != 0,
            _ => false
        };

        private byte TypeGuardOpcode(Expr.Call test) => test.Function switch
        {
            "is_atom" => IsAtom,
            "is_binary" => IsBinary,
            "is_integer" => IsInteger,
            "is_list" => IsList,
            "is_tuple" => IsTuple,
            AutoImportNames.IsPid => IsPid,
            AutoImportNames.IsPort => IsPort,
            AutoImportNames.IsBitstring when owner.ResolvesErlangBif(test) => IsBitString,
            AutoImportNames.IsReference when owner.ResolvesErlangBif(test) => IsReference,
            RecordPredicateNames.IsRecord when owner.ResolvesErlangBif(test) => IsAnyNativeRecord,
            AutoImportNames.IsFloat => IsFloat,
            AutoImportNames.IsMap => IsMap,
            AutoImportNames.IsBoolean when owner.receiveScratchRegisters is not null => IsBoolean,
            AutoImportNames.IsNumber => IsNumber,
            AutoImportNames.IsFunction => IsFunction,
            _ => 0
        };

        private Expr? EmitEntryGuardPrefix(
            Clause clause,
            Expr? guard,
            int failure,
            Func<string, (long Lower, long Upper)?>? integerBounds = null
        )
        {
            if (guard is Expr.GuardAlternatives compoundAlternatives
                && TryEmitEntryTupleGuardAlternatives(clause, compoundAlternatives, failure))
            {
                return null;
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
            {
                Expr? left = EmitEntryGuardPrefix(
                    clause,
                    conjunction.Left,
                    failure,
                    integerBounds
                );

                return left is null
                    ? EmitEntryGuardPrefix(
                        clause,
                        conjunction.Right,
                        failure,
                        integerBounds
                    )
                    : conjunction with { Left = left };
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } alternatives
                && ReadIntegerGuardKey(alternatives.Left) is { } firstKey
                && ReadIntegerGuardKey(alternatives.Right) is { } secondKey
                && firstKey.Name == secondKey.Name && firstKey.Key.Value != secondKey.Key.Value
                && HasUnconditionalParameters(clause))
            {
                int register = Array.FindIndex(clause.Patterns.ToArray(),
                    pattern => pattern is Pattern.Variable parameter && parameter.Name == firstKey.Name);
                if (register >= 0)
                {
                    int success = Label();
                    owner.EmitMembershipSelect(register, failure,
                        new[] { firstKey.Key, secondKey.Key }.OrderBy(key => key.Value).Select(key => ((Term)key, success)));
                    Mark(success);

                    return null;
                }
            }

            if (guard is not null && ReadTupleArityGuard(guard) is { Value: Expr.Variable tuple, Arity: var arity }
                && HasUnconditionalParameters(clause))
            {
                int register = Array.FindIndex(clause.Patterns.ToArray(),
                    pattern => pattern is Pattern.Variable parameter && parameter.Name == tuple.Name);
                if (register >= 0)
                {
                    EmitTupleArityGuard(register, arity, failure);

                    return null;
                }
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } typeAlternatives
                && ReadEntryTypeGuard(clause, typeAlternatives.Left) is { } firstType
                && ReadEntryTypeGuard(clause, typeAlternatives.Right) is { } secondType)
            {
                int otherwise = Label();
                int success = Label();
                owner.Op(firstType.Opcode, LabelTag, otherwise);
                owner.Operand(XRegisterTag, firstType.Register);
                JumpTo(success);
                Mark(otherwise);
                owner.Op(secondType.Opcode, LabelTag, failure);
                owner.Operand(XRegisterTag, secondType.Register);
                Mark(success);

                return null;
            }

            if (guard is Expr.Binary comparison && ReadEntryComparisonOpcode(clause, comparison) is byte opcode)
            {
                bool reverse = comparison.Operator is ErlangOperators.Greater or ErlangOperators.LessOrEqual;
                owner.Op(opcode, LabelTag, failure);
                EmitComparisonOperand(reverse ? comparison.Right : comparison.Left);
                EmitComparisonOperand(reverse ? comparison.Left : comparison.Right);

                return null;

                void EmitComparisonOperand(Expr operand)
                {
                    if ((opcode is IsLess or IsGreaterOrEqual) && operand is Expr.Variable variable
                        && integerBounds?.Invoke(variable.Name) is { } bounds && bounds.Lower < bounds.Upper)
                    {
                        int register = ArgumentRegister(clause, variable.Name);
                        owner.EmitTypedRegister(
                            register,
                            IntegerTypeBits,
                            bounds.Lower,
                            bounds.Upper
                        );
                    }
                    else
                    {
                        EmitDirectOperand(clause, operand);
                    }
                }
            }
            if (ReadEntryFunctionArityGuard(clause, guard) is { } function)
            {
                owner.Op(IsFunctionWithArity, LabelTag, failure);
                owner.Operand(XRegisterTag, function.Register);
                owner.LiteralOperand(function.Arity);

                return null;
            }

            if (ReadEntryTypeGuard(clause, guard) is not { } test)
            {
                return guard;
            }

            owner.Op(test.Opcode, LabelTag, failure);
            owner.Operand(XRegisterTag, test.Register);

            return null;

        }

        private static (string Name, Integer Key)? ReadIntegerGuardKey(Expr guard) => guard switch
        {
            Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable variable, Right: Expr.Literal { Value: Integer key }
            } => (variable.Name, key),
            Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Literal { Value: Integer key }, Right: Expr.Variable variable
            } => (variable.Name, key),
            _ => null
        };

        private static byte? ReadEntryComparisonOpcode(Clause clause, Expr.Binary comparison)
        {
            byte opcode = GuardComparisonOpcode(comparison);

            return opcode != 0 && HasUnconditionalParameters(clause)
                && IsEntryOperand(comparison.Left) && IsEntryOperand(comparison.Right)
                ? opcode : null;

            bool IsEntryOperand(Expr operand) => operand is Expr.Variable variable
                ? variable.Name != VariableScopeNames.Wildcard
                    && clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == variable.Name)
                : AttributeLiteral.TryNormalizeConstant(operand, out _);
        }

        private (int Register, Integer Arity)? ReadEntryFunctionArityGuard(Clause clause, Expr? guard)
        {
            if (guard is not Expr.Call { Arguments: [Expr.Variable function, Expr.Literal { Value: Integer arity }] } call
                || function.Name == VariableScopeNames.Wildcard || !IsFunctionArityCall(call) || !owner.ResolvesErlangBif(call)
                || arity.Value < 0 || arity.Value > byte.MaxValue)
            {
                return null;
            }

            for (int register = 0; register < clause.Patterns.Count; register++)
            {
                if (clause.Patterns[register] is Pattern.Variable parameter && parameter.Name == function.Name)
                {
                    return (register, arity);
                }
            }

            return null;
        }

        private (byte Opcode, int Register)? ReadEntryTypeGuard(Clause clause, Expr? guard)
        {
            if (guard is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Arguments: [Expr.Variable tested]
                } call
                || !owner.ResolvesErlangBif(call))
            {
                return null;
            }

            byte opcode = TypeGuardOpcode(call);
            if (opcode == 0)
            {
                return null;
            }

            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                Pattern.Variable? parameter = clause.Patterns[index] switch
                {
                    Pattern.Variable variable => variable,
                    AliasPattern { Left: Pattern.Variable variable, Right: Pattern.Tuple { } } => variable,
                    AliasPattern { Left: Pattern.Tuple { }, Right: Pattern.Variable variable } => variable,
                    _ => null
                };
                if (parameter?.Name == tested.Name)
                {
                    return (opcode, index);
                }
            }

            return null;
        }

        private bool TryGuardComparison(Expr.Binary comparison, int failure, string? selector)
        {
            byte opcode = GuardComparisonOpcode(comparison);
            if (opcode == 0)
            {
                return false;
            }

            if (selector is null && owner.receiveScratchRegisters is null
                && TryEmitRegisterGuardArithmetic(comparison, opcode, failure))
            {
                return true;
            }

            int? left = GuardSlot(comparison.Left, failure, selector);
            int? right = GuardSlot(comparison.Right, failure, selector);
            owner.Op(opcode, LabelTag, failure);
            if (comparison.Operator is ErlangOperators.Greater or ErlangOperators.LessOrEqual)
            {
                GuardOperand(comparison.Right, right, selector);
                GuardOperand(comparison.Left, left, selector);
            }
            else
            {
                GuardOperand(comparison.Left, left, selector);
                GuardOperand(comparison.Right, right, selector);
            }

            return true;
        }

        private bool TryEmitRegisterGuardArithmetic(Expr.Binary comparison, byte opcode, int failure)
        {
            var (left, right) = EntryComparisonOperands(comparison);
            bool arithmeticOnLeft = left is Expr.Binary;
            Expr other = arithmeticOnLeft ? right : left;
            if ((arithmeticOnLeft ? left : right) is not Expr.Binary arithmetic
                || !IsRegisterArithmeticOperator(arithmetic.Operator)
                || !IsBoundOperand(arithmetic.Left) || !IsBoundOperand(arithmetic.Right) || !IsBoundOperand(other))
            {
                return false;
            }

            int? otherSlot = GuardSlot(other, failure);
            EmitArithmetic(arithmetic, failure);
            owner.Op(opcode, LabelTag, failure);
            if (arithmeticOnLeft)
            {
                owner.Operand(XRegisterTag, 0);
                GuardOperand(other, otherSlot);
            }
            else
            {
                GuardOperand(other, otherSlot);
                owner.Operand(XRegisterTag, 0);
            }

            return true;

            bool IsBoundOperand(Expr operand) => operand is Expr.Variable variable
                ? variables.ContainsKey(variable.Name)
                : AttributeLiteral.TryNormalizeConstant(operand, out _);
        }

        private static byte GuardComparisonOpcode(Expr.Binary comparison) => comparison.Operator switch
        {
            ErlangOperators.ExactEqual => ExactEqualTest,
            ErlangOperators.ExactNotEqual => ExactNotEqualTest,
            ErlangOperators.NumericEqual => HasAtomOperand(comparison) ? ExactEqualTest : NumericEqualTest,
            ErlangOperators.NumericNotEqual => HasAtomOperand(comparison) ? ExactNotEqualTest : NumericNotEqualTest,
            ErlangOperators.Less or ErlangOperators.Greater => IsLess,
            ErlangOperators.LessOrEqual or ErlangOperators.GreaterOrEqual => IsGreaterOrEqual,
            _ => 0
        };

        private int? GuardSlot(Expr expression, int? failure, string? selector = null) =>
            IsGuardSelector(expression, selector) || AttributeLiteral.TryNormalizeConstant(expression, out _)
                ? null
                : ValueSlot(expression, failure);

        private static bool IsGuardSelector(Expr expression, string? selector) =>
            selector is not null && expression is Expr.Variable variable && variable.Name == selector;

        private void GuardOperand(Expr expression, int? slot, string? selector = null)
        {
            if (slot is int register)
            {
                owner.Operand(YRegisterTag, register);
            }
            else if (IsGuardSelector(expression, selector))
            {
                owner.Operand(XRegisterTag, 0);
            }
            else if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
            {
                owner.LiteralOperand(constant);
            }
        }

        private Pattern?[] EmitEntryShapeTests(FunctionDefinition function, Clause clause, int failure)
        {
            var checkedShapes = new Pattern?[clause.Patterns.Count];
            for (int index = 0; index < checkedShapes.Length; index++)
            {
                Pattern pattern = EntryShape(clause.Patterns[index]);
                if (ListEntryTest(pattern) is { } listOpcode)
                {
                    if (listOpcode != IsNonemptyList || !HasProvenConsEntry(function, index))
                    {
                        owner.Op(listOpcode, LabelTag, failure);
                        owner.Operand(XRegisterTag, index);
                    }
                    checkedShapes[index] = pattern;
                    continue;
                }

                if (pattern is not Pattern.Tuple tuple)
                {
                    continue;
                }

                if (HasTupleTag(tuple, out Atom tag))
                {
                    EmitDetailRecordTest(
                        index,
                        tuple.Items.Count,
                        tag,
                        failure
                    );
                }
                else
                {
                    EmitTupleShapeTest(
                        XRegisterTag,
                        index,
                        tuple.Items.Count,
                        failure
                    );
                }

                checkedShapes[index] = tuple;
            }

            return checkedShapes;
        }

        private static Pattern.Tuple? ReadDirectConsHead(Clause clause, int input)
        {
            if (clause.Guard is not null || clause.Patterns[input] is not Pattern.List
                { Items: [Pattern.Tuple tuple], Tail: Pattern.Variable tail }
                || tuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable)
                || clause.Patterns.Where((_, index) => index != input).Any(item => item is not Erlang.Pattern.Variable))
            {
                return null;
            }

            var bindings = clause.Patterns.OfType<Pattern.Variable>()
                .Concat(tuple.Items.OfType<Pattern.Variable>())
                .Append(tail)
                .Where(variable => variable.Name != VariableScopeNames.Wildcard)
                .ToArray();

            return DistinctBindings(bindings) ? tuple : null;
        }

        private (bool Shape, bool Arity, bool Tag)[] EmitSharedEntryTests(
            FunctionDefinition function, IReadOnlyList<Clause> clauses, int failure)
        {
            var checkedShapes = new (bool Shape, bool Arity, bool Tag)[clauses[0].Patterns.Count];
            for (int index = 0; index < checkedShapes.Length; index++)
            {
                Pattern first = EntryShape(clauses[0].Patterns[index]);
                if (ListEntryTest(first) is { } opcode
                    && clauses.All(clause => ListEntryTest(EntryShape(clause.Patterns[index])) == opcode))
                {
                    if (opcode != IsNonemptyList || !HasProvenConsEntry(function, index))
                    {
                        owner.Op(opcode, LabelTag, failure);
                        owner.Operand(XRegisterTag, index);
                    }
                    checkedShapes[index] = (true, false, false);
                }
                else if (first is Pattern.Literal { Value: Atom or Integer } literal && clauses.All(clause =>
                    clause.Patterns[index] is Pattern.Literal other && literal.Value.Equals(other.Value)))
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, index);
                    owner.LiteralOperand(literal.Value);
                    checkedShapes[index] = (true, false, false);
                }
                else if (first is Pattern.Tuple tuple && clauses.All(clause =>
                    EntryShape(clause.Patterns[index]) is Pattern.Tuple))
                {
                    bool sharedArity = clauses.All(clause =>
                        ((Pattern.Tuple)EntryShape(clause.Patterns[index])).Items.Count == tuple.Items.Count);
                    bool hasTag = HasTupleTag(tuple, out Atom tag);
                    bool sharedTag = sharedArity && hasTag && clauses.All(clause =>
                        EntryShape(clause.Patterns[index]) is Pattern.Tuple other
                        && HasTupleTag(other, out Atom otherTag) && tag.Equals(otherTag));
                    if (sharedTag)
                    {
                        EmitDetailRecordTest(
                            index,
                            tuple.Items.Count,
                            tag,
                            failure
                        );
                    }
                    else if (sharedArity)
                    {
                        EmitTupleShapeTest(
                            XRegisterTag,
                            index,
                            tuple.Items.Count,
                            failure
                        );
                    }
                    else
                    {
                        owner.Op(IsTuple, LabelTag, failure);
                        owner.Operand(XRegisterTag, index);
                    }

                    checkedShapes[index] = (true, sharedArity, sharedTag);
                }
            }

            return checkedShapes;
        }

        private HashSet<int>?[] EmitSharedEntryTupleLiterals(IReadOnlyList<Clause> clauses, int failure)
        {
            int arity = clauses[0].Patterns.Count;
            var checkedFields = new HashSet<int>?[arity];
            for (int argument = 0; argument < arity; argument++)
            {
                if (clauses[0].Patterns[argument] is not Pattern.Tuple tuple
                    || clauses.Any(clause => clause.Patterns[argument] is not Pattern.Tuple other
                        || other.Items.Count != tuple.Items.Count))
                {
                    continue;
                }

                for (int field = 0; field < tuple.Items.Count; field++)
                {
                    if (tuple.Items[field] is not Pattern.Literal literal
                        || field == 0 && literal.Value is Atom
                        || clauses.Any(clause => ((Pattern.Tuple)clause.Patterns[argument]).Items[field]
                            is not Pattern.Literal other || !literal.Value.Equals(other.Value)))
                    {
                        continue;
                    }

                    owner.EmitTuplePart(argument, field, arity);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, arity);
                    owner.LiteralOperand(literal.Value);
                    (checkedFields[argument] ??= []).Add(field);
                }
            }

            return checkedFields;
        }

        private static Pattern EntryShape(Pattern pattern) => pattern switch
        {
            AliasPattern { Left: Pattern.Variable { }, Right: Pattern.Tuple { } or Pattern.List { } } alias => alias.Right,
            AliasPattern { Left: Pattern.Tuple { } or Pattern.List { }, Right: Pattern.Variable { } } alias => alias.Left,
            _ => pattern
        };

        private static byte? ListEntryTest(Pattern pattern) => pattern switch
        {
            Pattern.List { Items.Count: > 0 } => IsNonemptyList,
            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil } => IsNil,
            _ => null
        };

        private static bool HasTupleTag(Pattern.Tuple tuple, out Atom tag)
        {
            if (tuple.Items.Count > 0 && tuple.Items[0] is Pattern.Literal { Value: Atom first })
            {
                tag = first;

                return true;
            }

            tag = null!;

            return false;
        }

        private void EmitTupleShapeTest(
            int registerTag,
            int value,
            int arity,
            int failure,
            int selectorType = 0
        )
        {
            owner.Op(IsTuple, LabelTag, failure);
            if (selectorType == 0)
            {
                owner.Operand(registerTag, value);
            }
            else
            {
                owner.EmitTypedRegister(value, selectorType, registerTag: registerTag);
            }

            owner.Op(TestArity, LabelTag, failure);
            owner.Operand(registerTag, value);
            owner.Operand(BeamWire.UnsignedTag, arity);
        }

        private void Pattern(
            Pattern pattern,
            int value,
            int failure,
            Pattern? checkedShape = null,
            bool checkedTupleTag = true,
            bool checkedTupleArity = true,
            bool entryPatternScratch = false,
            HashSet<int>? checkedTupleLiterals = null,
            int? receiveResultField = null,
            int valueTag = YRegisterTag,
            int? tupleScratch = null
        )
        {
            switch (pattern)
            {
                case Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                    return;
                case Pattern.Variable variable:
                    if (variables.TryGetValue(variable.Name, out int bound))
                    {
                        owner.Op(ExactEqualTest, LabelTag, failure);
                        owner.Operand(valueTag, value);
                        owner.Operand(YRegisterTag, bound);
                    }
                    else
                    {
                        if (valueTag == XRegisterTag)
                        {
                            int slot = Slot();
                            owner.MoveRegister(
                                XRegisterTag,
                                value,
                                YRegisterTag,
                                slot
                            );
                            value = slot;
                        }

                        variables.Add(variable.Name, value);
                    }

                    return;
                case AliasPattern alias:
                    Pattern(
                        alias.Left,
                        value,
                        failure,
                        checkedShape,
                        checkedTupleTag,
                        checkedTupleArity,
                        entryPatternScratch: valueTag == XRegisterTag && entryPatternScratch,
                        valueTag: valueTag,
                        tupleScratch: tupleScratch
                    );
                    Pattern(
                        alias.Right,
                        value,
                        failure,
                        checkedShape,
                        checkedTupleTag,
                        checkedTupleArity,
                        entryPatternScratch: valueTag == XRegisterTag && entryPatternScratch,
                        valueTag: valueTag,
                        tupleScratch: tupleScratch
                    );

                    return;
                case Pattern.Literal literal:
                    if (ReferenceEquals(literal, checkedShape))
                    {
                        return;
                    }

                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(valueTag, value);
                    owner.LiteralOperand(literal.Value);

                    return;
                case Pattern.Tuple tuple:
                    bool prechecked = ReferenceEquals(tuple, checkedShape);
                    int firstMember = prechecked && checkedTupleTag && HasTupleTag(tuple, out _) ? 1 : 0;
                    if (!prechecked && receiveResultField != 0 && checkedTupleLiterals?.Contains(0) != true
                        && HasTupleTag(tuple, out Atom tupleTag))
                    {
                        owner.EmitRecordTest(
                            failure,
                            valueTag,
                            value,
                            tupleTag,
                            tuple.Items.Count
                        );
                        firstMember = 1;
                    }
                    else if (!prechecked)
                    {
                        EmitTupleShapeTest(
                            valueTag,
                            value,
                            tuple.Items.Count,
                            failure
                        );
                    }
                    else if (!checkedTupleArity)
                    {
                        owner.Op(TestArity, LabelTag, failure);
                        owner.Operand(valueTag, value);
                        owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
                    }

                    for (int index = firstMember; index < tuple.Items.Count; index++)
                    {
                        Pattern item = tuple.Items[index];
                        if (item is Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            || checkedTupleLiterals?.Contains(index) == true)
                        {
                            continue;
                        }

                        if (index == receiveResultField)
                        {
                            owner.Op(GetTupleElement, valueTag, value);
                            owner.Operand(BeamWire.UnsignedTag, index);
                            owner.Operand(XRegisterTag, 0);
                            continue;
                        }

                        bool simpleScratch = entryPatternScratch && (item is Pattern.Literal
                            || valueTag == XRegisterTag && item is Pattern.Variable name && variables.ContainsKey(name.Name));
                        bool nestedTuple = tupleScratch.HasValue && item is Pattern.Tuple nested && CanTestTupleInX0(nested);
                        int? scratch = tupleScratch is int available
                            ? available < FirstReservedXRegister && (nestedTuple || simpleScratch) ? available : null
                            : simpleScratch ? valueTag == XRegisterTag ? 1 : 0 : null;
                        int tag = scratch.HasValue ? XRegisterTag : YRegisterTag;
                        int member = scratch ?? Slot();
                        owner.Op(GetTupleElement, valueTag, value);
                        owner.Operand(BeamWire.UnsignedTag, index);
                        owner.Operand(tag, member);
                        Pattern(
                            item,
                            member,
                            failure,
                            entryPatternScratch: entryPatternScratch,
                            valueTag: tag,
                            tupleScratch: nestedTuple ? tupleScratch + 1 : null
                        );
                    }

                    return;
                case Pattern.List list:
                    int current = value;
                    int currentTag = YRegisterTag;
                    bool useScratch = entryPatternScratch
                        && list.Items.All(item => item is Pattern.Variable { } or Pattern.Literal { } or Pattern.Any { })
                        && list.Tail is null or Pattern.Variable { };
                    bool needsListTest = !ReferenceEquals(list, checkedShape);
                    for (int index = 0; index < list.Items.Count; index++)
                    {
                        Pattern item = list.Items[index];
                        bool needsHead = item is not (Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard });
                        bool needsTail = index + 1 < list.Items.Count
                            || list.Tail is not (Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard });
                        if (needsListTest)
                        {
                            owner.Op(IsNonemptyList, LabelTag, failure);
                            owner.Operand(currentTag, current);
                        }

                        needsListTest = true;
                        bool scratchHead = useScratch && item is Pattern.Literal { };
                        int headTag = scratchHead ? XRegisterTag : YRegisterTag;
                        int head = scratchHead ? 1 : needsHead ? Slot() : -1;
                        bool scratchTail = useScratch && list.Items.Count > 1
                            && (index + 1 < list.Items.Count || list.Tail is null);
                        int tailTag = scratchTail ? XRegisterTag : YRegisterTag;
                        int tail = needsTail ? scratchTail ? 0 : Slot() : -1;
                        if (needsHead && needsTail)
                        {
                            owner.Op(GetList, currentTag, current);
                            owner.Operand(headTag, head);
                            owner.Operand(tailTag, tail);
                        }
                        else if (needsHead || needsTail)
                        {
                            owner.Op(needsHead ? GetHead : GetTail, currentTag, current);
                            owner.Operand(needsHead ? headTag : tailTag, needsHead ? head : tail);
                        }

                        if (scratchHead && item is Pattern.Literal literalHead)
                        {
                            owner.Op(ExactEqualTest, LabelTag, failure);
                            owner.Operand(headTag, head);
                            owner.LiteralOperand(literalHead.Value);
                        }
                        else if (needsHead)
                        {
                            Pattern(item, head, failure);
                        }

                        current = tail;
                        currentTag = tailTag;
                    }

                    if (list.Tail is null && needsListTest)
                    {
                        owner.EmitNilTest(currentTag, current, failure);
                    }
                    else if (list.Tail is not null)
                    {
                        Pattern(list.Tail, current, failure);
                    }

                    return;
                case MapPattern map:
                    EmitMapPattern(map, value, failure);

                    return;
                case BitPattern bits:
                    BinaryPattern(bits, value, failure);

                    return;
                default:
                    throw new NotSupportedException(Unsupported);
            }
        }

        private void BinaryPattern(BitPattern bits, int value, int failure)
        {
            if (bits.Segments is [
                {
                    Value: var signed, Specification:
                    {
                        Type: BitSegmentTypes.Integer,
                        Signed: true, Endian: BitByteOrders.Big, Unit: BitSyntaxDefaults.BinaryUnit, Size: Expr.Variable width
                    }
                }]
                && variables.TryGetValue(width.Name, out int size))
            {
                SignedBytePattern(
                    signed,
                    value,
                    size,
                    failure
                );

                return;
            }
            if (owner.receiveScratchRegisters is null && bits.Segments is [var first, var tail]
                && IsByte(first.Specification)
                && tail.Specification is
                {
                    Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit,
                    Signed: false, Endian: BitByteOrders.Big
                })
            {
                ByteAndBinaryTail(
                    first.Value,
                    tail.Value,
                    value,
                    failure
                );

                return;
            }
            if (owner.receiveScratchRegisters is not null || bits.Segments.Any(segment => !IsByte(segment.Specification)))
            {
                EmitFixedBitPattern(bits, value, failure);

                return;
            }
            FixedBytePattern(bits, value, failure);
        }

        private static int FixedByteOperands(Pattern pattern, bool single) => pattern switch
        {
            Pattern.Variable { Name: VariableScopeNames.Wildcard } => single ? 0 : 2,
            Pattern.Literal { Value: Integer integer } when !single && integer.Value >= byte.MinValue && integer.Value <= byte.MaxValue => 4,
            _ => 6
        };

        private void FixedBytePattern(BitPattern bits, int value, int failure)
        {
            const int byteWidth = BitSyntaxDefaults.BinaryUnit;
            bool single = bits.Segments.Count == 1;
            Load(value);
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2 + bits.Segments.Sum(segment => FixedByteOperands(segment.Value, single)));
            owner.Operand(AtomTag, owner.Intern("ensure_exactly"));
            owner.Operand(BeamWire.UnsignedTag, checked(bits.Segments.Count * byteWidth));

            var captures = new List<(Pattern Pattern, int Slot)>();
            foreach (var segment in bits.Segments)
            {
                switch (segment.Value)
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        if (!single)
                        {
                            owner.Operand(AtomTag, owner.Intern("skip"));
                            owner.Operand(BeamWire.UnsignedTag, byteWidth);
                        }
                        break;
                    case Pattern.Literal { Value: Integer integer } when !single && integer.Value >= byte.MinValue && integer.Value <= byte.MaxValue:
                        owner.Operand(AtomTag, owner.Intern("=:="));
                        owner.Operand(AtomTag, NilAtomIndex);
                        owner.Operand(BeamWire.UnsignedTag, byteWidth);
                        owner.Operand(BeamWire.UnsignedTag, integer.Value);
                        break;
                    default:
                        int slot = Slot();
                        owner.Operand(AtomTag, owner.Intern("integer"));
                        owner.Operand(BeamWire.UnsignedTag, 2);
                        owner.Operand(AtomTag, NilAtomIndex);
                        owner.Operand(BeamWire.UnsignedTag, byteWidth);
                        owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
                        owner.Operand(YRegisterTag, slot);
                        captures.Add((segment.Value, slot));
                        break;
                }
            }
            foreach (var capture in captures)
            {
                Pattern(capture.Pattern, capture.Slot, failure);
            }
        }

        private static bool IsByte(BitSegment specification) =>
            specification is { Type: BitSegmentTypes.Integer, Unit: 1, Signed: false, Endian: BitByteOrders.Big }
                && (specification.Size is null
                    || specification.Size is Expr.Literal { Value: Integer size } && size.Value == 8);

        private void SignedBytePattern(
            Pattern pattern,
            int value,
            int size,
            int failure
        )
        {
            Load(value);
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);
            Load(size, 2);
            owner.Op(GetBinaryInteger, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 4); // signed, big-endian
            owner.Operand(XRegisterTag, 2);
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, owner.Intern("ensure_exactly"));
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            Pattern(pattern, Save(), failure);
        }

        private void ByteAndBinaryTail(
            Pattern first,
            Pattern tail,
            int value,
            int failure
        )
        {
            Load(value);
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);

            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 13);
            owner.Operand(AtomTag, owner.Intern("ensure_at_least"));
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.Operand(AtomTag, owner.Intern("integer"));
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(AtomTag, owner.Intern("get_tail"));
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.Operand(XRegisterTag, 3);

            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            int prefix = Save();
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            int suffix = Save();
            Pattern(first, prefix, failure);
            Pattern(tail, suffix, failure);
        }

        private void Call(string? module, string name, int arity)
        {
            if (module is null && entries.TryGetValue((name, arity), out int entry))
            {
                owner.EmitFieldCall(arity, entry);
            }
            else
            {
                module ??= owner.ResolveExternalModule(name, arity);
                if (module is null)
                {
                    throw new NotSupportedException(Unsupported);
                }
                owner.Op(ExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, name, arity));
            }
        }

        private void EmitFinalCallOperand(Expr expression, int destination)
        {
            if (destination != 0 && owner.receiveScratchRegisters is null && expression is Expr.RecordAccess access)
            {
                EmitRecordAccess(access, destination);

                return;
            }

            bool constructInDestination = expression is Expr.List && owner.receiveScratchRegisters is null;
            Expression(expression, listDestination: constructInDestination ? destination : 0);
            if (!constructInDestination && destination != 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    destination
                );
            }
        }

        private void Expression(Expr expression, int? guardFailure = null, int listDestination = 0)
        {
            if (ReferenceEquals(expression, preparedReceiveCall))
            {
                return;
            }
            if (ReadExternalFunctionLiteral(expression) is { } functionLiteral)
            {
                owner.EmitConstant(functionLiteral, 0);

                return;
            }
            if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
            {
                owner.EmitConstant(constant, listDestination);

                return;
            }
            if (owner.ReadRecordTest(expression) is { } recordTest)
            {
                Expression(recordTest.Value, guardFailure);
                int failed = Label();
                int done = Label();
                owner.EmitRecordTest(
                    failed,
                    XRegisterTag,
                    0,
                    recordTest.Name,
                    recordTest.Arity
                );
                owner.EmitConstant(Term.A("true"), 0);
                JumpTo(done);
                Mark(failed);
                owner.EmitConstant(Term.A("false"), 0);
                Mark(done);

                return;
            }
            switch (expression)
            {
                case Expr.Variable variable:
                    if (deferredPairMembers.TryGetValue(variable.Name, out var member))
                    {
                        EmitDeferredPairMember(member, 0);
                    }
                    else
                    {
                        Load(variables.TryGetValue(variable.Name, out int slot) ? slot : throw new NotSupportedException(Unsupported));
                    }

                    return;
                case Expr.Bits bits:
                    EmitBits(bits, guardFailure);

                    return;
                case Expr.Map map:
                    EmitMap(map, guardFailure);

                    return;
                case Expr.Tuple tuple:
                    if (TryEmitLinearCallListTuple(tuple) || TryEmitTupleWithConstantCallResult(tuple, guardFailure))
                    {
                        return;
                    }
                    if (guardFailure is null && tuple.Items is [Expr.Literal { Value: Atom tag }, Expr.Call or Expr.Block])
                    {
                        Expression(tuple.Items[1]);
                        owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                        owner.Operand(BeamWire.UnsignedTag, Math.Max(1, ReceiveGuardLiveRegisters));
                        owner.Op(PutTuple, XRegisterTag, 0);
                        owner.instructions.WriteByte(ExtendedListOperand);
                        owner.Operand(BeamWire.UnsignedTag, 2);
                        owner.LiteralOperand(tag);
                        owner.Operand(XRegisterTag, 0);

                        return;
                    }
                    if (guardFailure is null && TryEmitTupleWithRegisterMember(tuple))
                    {
                        return;
                    }

                    int tupleOperandBase = nextSlot;
                    bool finalCall = guardFailure is null && tuple.Items.Count > 0 && tuple.Items[^1] is Expr.Call;
                    int savedCount = tuple.Items.Count - (finalCall ? 1 : 0);
                    int?[] members = tuple.Items.Take(savedCount).Select(item => GuardSlot(item, guardFailure)).ToArray();
                    if (finalCall)
                    {
                        Expression(tuple.Items[^1]);
                    }

                    owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
                    owner.Operand(BeamWire.UnsignedTag, Math.Max(finalCall ? 1 : 0, ReceiveGuardLiveRegisters));
                    owner.Op(PutTuple, XRegisterTag, 0);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
                    for (int index = 0; index < members.Length; index++)
                    {
                        GuardOperand(tuple.Items[index], members[index]);
                    }

                    if (finalCall)
                    {
                        owner.Operand(XRegisterTag, 0);
                    }

                    ReleaseOperandSlots(tupleOperandBase);

                    return;
                case Expr.List list:
                    if (TryEmitDeferredPairList(list))
                    {
                        if (listDestination != 0)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                XRegisterTag,
                                listDestination
                            );
                        }

                        return;
                    }
                    if (list.Items.Count == 0)
                    {
                        Expression(list.Tail ?? new Expr.Literal(Nil.Value), guardFailure);
                        if (listDestination != 0)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                XRegisterTag,
                                listDestination
                            );
                        }

                        return;
                    }

                    Term? tailLiteral = list.Tail is null ? Nil.Value
                        : AttributeLiteral.TryNormalizeConstant(list.Tail, out Term constantTail) ? constantTail : null;
                    int count = list.Items.Count;
                    while (tailLiteral is not null && count > 0
                        && AttributeLiteral.TryNormalizeConstant(list.Items[count - 1], out Term suffixHead))
                    {
                        tailLiteral = new Cons(suffixHead, tailLiteral);
                        count--;
                    }
                    if (count == 0)
                    {
                        owner.EmitConstant(tailLiteral!, listDestination);

                        return;
                    }

                    int listOperandBase = nextSlot;
                    bool headInX = guardFailure is null && tailLiteral is not null
                        && list.Items[count - 1] is Expr.Call or Expr.Tuple;
                    int savedHeadCount = count - (headInX ? 1 : 0);
                    int?[] items = list.Items.Take(savedHeadCount).Select(item => GuardSlot(item, guardFailure)).ToArray();
                    Atom? listElementTag = null;
                    if (headInX)
                    {
                        if (list.Items[count - 1] is Expr.Tuple
                            { Items: [Expr.Literal { Value: Atom elementTag }, Expr.Call { Module: not null, DynamicRemote: false } elementCall] })
                        {
                            Expression(elementCall);
                            listElementTag = elementTag;
                        }
                        else
                        {
                            Expression(list.Items[count - 1]);
                        }
                    }

                    int? tailSlot = list.Tail is Expr.Variable tail && variables.TryGetValue(tail.Name, out int tailRegister)
                        ? tailRegister : null;
                    bool tailInX = tailSlot is null && tailLiteral is null;
                    if (tailInX)
                    {
                        Expression(list.Tail!, guardFailure);
                    }

                    if (listElementTag is not null)
                    {
                        EmitTaggedRegisterResult(listElementTag, additionalHeapWords: count * ConsHeapWords);
                    }
                    else
                    {
                        owner.Op(TestHeap, BeamWire.UnsignedTag, count * ConsHeapWords);
                        owner.Operand(BeamWire.UnsignedTag, Math.Max(tailInX || headInX ? 1 : 0, ReceiveGuardLiveRegisters));
                    }
                    for (int index = count - 1; index >= 0; index--)
                    {
                        owner.instructions.WriteByte(PutList);
                        if (headInX && index == count - 1)
                        {
                            owner.Operand(XRegisterTag, 0);
                        }
                        else
                        {
                            GuardOperand(list.Items[index], items[index]);
                        }

                        if (index != count - 1 || tailInX)
                        {
                            owner.Operand(XRegisterTag, 0);
                        }
                        else if (tailSlot is int savedTail)
                        {
                            owner.Operand(YRegisterTag, savedTail);
                        }
                        else
                        {
                            owner.LiteralOperand(tailLiteral!);
                        }
                        owner.Operand(XRegisterTag, index == 0 ? listDestination : 0);
                    }

                    ReleaseOperandSlots(listOperandBase);

                    return;
                case Expr.Sequence sequence:
                    var sequenceRecords = checkedRecordBindings;
                    DiscardSequencePrefix(sequence.Items, guardFailure, rememberRecords: guardFailure is null);
                    Expression(sequence.Items[^1], guardFailure);
                    checkedRecordBindings = sequenceRecords;

                    return;
                case Expr.Block block:
                    Expression(block.Body, guardFailure);

                    return;
                case Expr.Match match:
                    Match(match, guardFailure, returnValue: true);

                    return;
                case Expr.Receive receive when guardFailure is null:
                    EmitReceive(receive);

                    return;
                case Expr.Case @case:
                    EmitCase(@case, guardFailure);

                    return;
                case Expr.Maybe conditional when guardFailure is null:
                    EmitMaybe(conditional);

                    return;
                case Expr.Fun closure when guardFailure is null:
                    EmitGeneralClosure(closure);

                    return;
                case Expr.FunctionReference reference when guardFailure is null
                    && entries.TryGetValue((reference.Name, reference.Arity), out int entry):
                    owner.EmitClosureAllocation(0, 0, 1);
                    owner.EmitDeclaredFunctionReference(reference, entry, 0);

                    return;
                case Expr.FunctionReference reference when guardFailure is null
                    && owner.ReadAutoImportedFunctionLiteral(reference, entries) is { } literalReference:
                    owner.EmitConstant(literalReference, 0);

                    return;
                case Expr.RecordConstruction record:
                    Expression(RecordExpressions.Create(record), guardFailure);

                    return;
                case Expr.RecordAccess access:
                    EmitRecordAccess(access, 0, guardFailure);

                    return;

                case Expr.RecordUpdate update when guardFailure is null:
                    EmitCheckedRecordUpdate(update);

                    return;
                case Expr.Try { After: { } cleanup } protectedBody when guardFailure is null:
                    EmitTryAfter(protectedBody, cleanup);

                    return;
                case Expr.Try { After: null } protectedBody when guardFailure is null:
                    ProtectedExpression(protectedBody);

                    return;
                case Expr.Catch caught when guardFailure is null:
                    EmitCatch(caught);

                    return;
                case Expr.If conditional when guardFailure is null:
                    Conditional(conditional);

                    return;
                case Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse } logical:
                    ShortCircuit(logical, guardFailure);

                    return;
                case Expr.Binary { Operator: ErlangOperators.SubtractList } subtract when guardFailure is null:
                    EmitAppend(subtract, tail: false);

                    return;
                case Expr.Binary { Operator: ErlangOperators.Append } append when guardFailure is null:
                    if (AppendPrefix(append.Left) is { } prefix)
                    {
                        Expression(prefix.Count == 0 ? append.Right : new Expr.List(prefix, append.Right));
                    }
                    else
                    {
                        EmitAppend(append, tail: false);
                    }

                    return;
                case Expr.Binary numeric when IsNumericEquality(numeric):
                    EmitNumericComparison(numeric, guardFailure);

                    return;
                case Expr.Binary { Operator: ErlangOperators.Send } send when guardFailure is null:
                    int sendOperandBase = nextSlot;
                    int[] operands = Values([send.Left, send.Right], guardFailure);
                    Load(operands[0]);
                    Load(operands[1], 1);
                    var sendLocation = location;
                    owner.EmitArithmeticLine(send.SourceLine, ref sendLocation);
                    owner.instructions.WriteByte(Send);
                    ReleaseOperandSlots(sendOperandBase);

                    return;
                case Expr.Unary { Operator: ErlangOperators.Not } booleanNot:
                    EmitBooleanNot(booleanNot, guardFailure);

                    return;
                case Expr.Unary { Operator: ErlangOperators.Minus or ErlangOperators.BitwiseNot } unaryArithmetic:
                    EmitUnaryArithmetic(unaryArithmetic, guardFailure);

                    return;
                case Expr.Binary { Operator: ErlangOperators.Divide } divide when guardFailure is null:
                    EmitFloatDivision(divide);

                    return;
                case Expr.Binary
                {
                    Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                    or ErlangOperators.IntegerDivide or ErlangOperators.Divide or ErlangOperators.Remainder or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight
                    or ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.Xor
                } arithmetic:
                    EmitArithmetic(arithmetic, guardFailure);

                    return;
                case Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                    or ErlangOperators.LessOrEqual or ErlangOperators.Less or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
                } binary:
                    EmitComparisonBif(binary, binary.Operator, guardFailure);

                    return;
                case Expr.Call
                {
                    Module: "lists", Function: "foldl",
                    Arguments: [Expr.Fun { SelfName: null } fun, var accumulator, var source]
                } fold:
                    Fold(
                        fold,
                        fun,
                        accumulator,
                        source
                    );

                    return;
                case Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "binary_to_list",
                    Arguments: [Expr.Call { Module: null, DynamicRemote: false } inner]
                } convert
                    when guardFailure is null && (convert.Module is not null || !entries.ContainsKey(("binary_to_list", 1)))
                        && entries.ContainsKey((inner.Function, inner.Arguments.Count)):
                    Expression(inner);
                    owner.EmitCallLine(convert, CallLocation(inner, location));
                    Call(convert.Module, convert.Function, 1);

                    return;
                case Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "iolist_to_binary",
                    Arguments: [Expr.Call { DynamicRemote: false, Arguments: [_, Expr.Call producer] } projection]
                } convert
                    when guardFailure is null && (convert.Module is not null || !entries.ContainsKey(("iolist_to_binary", 1)))
                        && TryTupleProjection(projection):
                    owner.EmitCallLine(convert, CallLocation(producer, location));
                    Call(convert.Module, convert.Function, 1);

                    return;
                case Expr.Apply apply when guardFailure is null:
                    EmitFunctionValueCall(apply);

                    return;
                case Expr.Call { DynamicRemote: true, Arguments: [_, _, Expr.List { Tail: null }] } dynamic
                    when guardFailure is null:
                    EmitDynamicRemote(dynamic, tail: false);

                    return;
                case Expr.Call { DynamicRemote: false } sourceCall:
                    Expr.Call call = owner.ResolveImportedCall(sourceCall, entries);
                    if (guardFailure is null && ReadStackFreeThrow(call, requireCallerHandler: true) is { } raised)
                    {
                        EmitStackFreeThrow(raised, tail: false);

                        return;
                    }

                    if (guardFailure is null && call is
                        {
                            Module: null or ErlangModuleNames.Module,
                            Function: AutoImportNames.Self, Arguments: []
                        }
                        && owner.ResolvesErlangBif(call))
                    {
                        EmitZeroGuardBif(call.Function, 0);

                        return;
                    }
                    if (guardFailure is null && call is
                        {
                            Module: null or ErlangModuleNames.Module,
                            Function: AutoImportNames.Get, Arguments: [var key]
                        }
                        && owner.ResolvesErlangBif(call))
                    {
                        if (AttributeLiteral.TryNormalizeConstant(key, out Term literalKey))
                        {
                            owner.EmitDictionaryLookup(0, literalKey);
                        }
                        else
                        {
                            Expression(key);
                            owner.EmitDictionaryLookup(0);
                        }

                        return;
                    }
                    if (IsIntegerRangeCall(call))
                    {
                        EmitTernaryBifValue(call, guardFailure, needsGc: false);

                        return;
                    }
                    if (call is { Function: RecordPredicateNames.IsRecord, Arguments: [var recordValue] }
                        && owner.ResolvesErlangBif(call))
                    {
                        int? slot = GuardSlot(recordValue, guardFailure);
                        owner.Op(BifOne, LabelTag, guardFailure ?? 0);
                        owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, RecordPredicateNames.IsRecord, 1));
                        GuardOperand(recordValue, slot);
                        owner.Operand(XRegisterTag, 0);

                        return;
                    }
                    if (guardFailure is int partFailure && IsBinaryPartGuard(call))
                    {
                        EmitTernaryBifValue(call, partFailure, needsGc: true);

                        return;
                    }
                    if (guardFailure is int binaryGuardFailure && IsNonAllocatingBinaryGuardCall(call))
                    {
                        EmitNonAllocatingBinaryGuardValue(call, binaryGuardFailure);

                        return;
                    }
                    if (guardFailure is int gcUnaryFailure && IsGcUnaryGuardCall(call))
                    {
                        EmitGcUnaryGuard(call, gcUnaryFailure);

                        return;
                    }
                    if (guardFailure is int functionFailure && IsFunctionArityCall(call))
                    {
                        EmitFunctionArityGuardValue(call, functionFailure);

                        return;
                    }
                    if (guardFailure is int elementFailure && IsElementGuardCall(call))
                    {
                        EmitElementGuard(call, elementFailure);

                        return;
                    }
                    if (guardFailure is not null && IsZeroGuardBif(call))
                    {
                        EmitZeroGuardBif(call.Function, 0);

                        return;
                    }
                    if (guardFailure is int unaryFailure
                        && (call.Function is AutoImportNames.TupleSize or AutoImportNames.Hd or AutoImportNames.Tl or AutoImportNames.Node
                            && IsGuardBif(call, call.Function)
                            || call is { Function: AutoImportNames.IsBitstring or AutoImportNames.IsReference, Arguments.Count: 1 }
                            && owner.ResolvesErlangBif(call)))
                    {
                        EmitUnaryGuardBif(call, unaryFailure);

                        return;
                    }
                    if (guardFailure is null && TryTupleProjection(call))
                    {
                        return;
                    }
                    int argumentBase = nextSlot;
                    int arity = call.Arguments.Count;
                    var argumentLocation = LoadCallArguments(
                        call.Arguments,
                        guardFailure,
                        orderSimpleOperands: guardFailure is null,
                        callSite: CallLocation(call, location)
                    );
                    owner.EmitCallLine(call, argumentLocation);
                    if (guardFailure is int failureLabel)
                    {
                        if (arity != 1 || call.Module is not (null or ErlangModuleNames.Module)
                            || call.Function is not ("is_atom" or "is_binary" or "is_integer" or "is_list" or "is_tuple" or "is_boolean" or AutoImportNames.IsPid or AutoImportNames.IsFunction or AutoImportNames.IsMap or AutoImportNames.IsNumber or AutoImportNames.IsPort or AutoImportNames.IsFloat)
                            || call.Module is null && entries.ContainsKey((call.Function, 1)))
                        {
                            throw new NotSupportedException(UnsupportedGuardCall(call));
                        }
                        owner.Op(BifOne, LabelTag, failureLabel);
                        owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, arity));
                        owner.Operand(XRegisterTag, 0);
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        Call(call.Module, call.Function, arity);
                    }
                    nextSlot = Math.Max(argumentBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);

                    return;
                case Expr.ListComprehension comprehension:
                    Comprehension(comprehension);

                    return;
                case Expr.MapComprehension comprehension:
                    EmitMapComprehension(comprehension);

                    return;
                case Expr.BinaryComprehension comprehension when guardFailure is null:
                    EmitBinaryComprehension(comprehension);

                    return;
                default:
                    throw new NotSupportedException(UnsupportedExpression(expression));
            }
        }

        private bool TryEmitProtectedProjection(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause]
                || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Try
                    {
                        Body: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: "iolist_to_binary", Arguments: [Expr.Call projection]
                        } convert,
                        After: null,
                        Clauses: [
                            {
                                Guard: null, Patterns: [Pattern.Variable result],
                                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] }
                            }],
                        Catches: [var handler]
                    }
                }
                || !DistinctBindings(first, second, result) || result.Name != value.Name
                || !ReadWrappedCatch(clause, handler)
                || convert.Module is null && entries.ContainsKey(("iolist_to_binary", 1))
                || ReadTupleProjection(projection) is not { } projected
                || projected.Producer.Arguments is not [Expr.Variable left, Expr.Variable right]
                || left.Name != first.Name || right.Name != second.Name
                || !entries.ContainsKey((projected.Producer.Function, 2)))
            {
                return false;
            }

            int token = Slot();
            int catchLabel = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, catchLabel);
            owner.EmitCallLine(projected.Producer, location);
            Call(projected.Producer.Module, projected.Producer.Function, 2);
            owner.EmitTupleElement(projected.Index, 0);
            owner.EmitCallLine(convert, CallLocation(projected.Producer, location));
            Call(convert.Module, convert.Function, 1);
            EmitWrappedCatchResult(tag, token, catchLabel);

            return true;
        }

        private bool TryEmitProtectedProjectionArgument(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause]
                || clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Try
                    {
                        After: null, Clauses.Count: 0, Catches: [var handler],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable result, Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable key, Expr.Call
                                    {
                                        Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "element",
                                        Arguments: [Expr.Literal { Value: Integer index }, Expr.Call
                                        { Module: null, DynamicRemote: false, Arguments: [Expr.Variable data] } read]
                                    } projection]
                                } dispatch
                            },
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] }
                            ]
                        }
                    }
                }
                || !DistinctBindings(first, second, result) || result.Name != value.Name
                || key.Name != first.Name || data.Name != second.Name || index.Value != 1
                || projection.Module is null && entries.ContainsKey(("element", 2))
                || !entries.ContainsKey((read.Function, 1)) || !entries.ContainsKey((dispatch.Function, 2))
                || !ReadWrappedCatch(clause, handler))
            {
                return false;
            }

            int keySlot = Slot();
            int token = Slot();
            int catchLabel = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                keySlot
            );
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, catchLabel);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 1);
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(projection, readLocation);
            EmitChoiceElement(owner.Import(ErlangModuleNames.Module, "element", 2), 1, 1);
            Load(keySlot);
            owner.InitStackSlot(keySlot);
            owner.EmitCallLine(dispatch, CallLocation(projection, readLocation));
            Call(dispatch.Module, dispatch.Function, 2);
            EmitWrappedCatchResult(tag, token, catchLabel);

            return true;
        }

        private void EmitWrappedCatchResult(Atom tag, int token, int catchLabel)
        {
            owner.Op(TryEnd, YRegisterTag, token);
            EmitTaggedRegisterResult(tag);
            owner.EmitFrameReturn(token + 1);
            owner.EmitWrappedCatch(catchLabel, token);
        }

        private (Expr.Call Producer, int Index)? ReadTupleProjection(Expr.Call call)
        {
            if (call is not
                {
                    Function: "element", Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Integer index }, Expr.Call { Module: null, DynamicRemote: false } producer]
                }
                || index.Value < 1 || index.Value > int.MaxValue
                || call.Module is null && localFunctions.ContainsKey(("element", 2))
                || !ReturnsTuple(producer, (int)index.Value, []))
            {
                return null;
            }

            return (producer, (int)index.Value - 1);
        }

        private bool TryTupleProjection(Expr.Call call)
        {
            if (ReadTupleProjection(call) is not { } projected)
            {
                return false;
            }

            Expression(projected.Producer);
            owner.EmitTupleElement(projected.Index, 0);

            return true;
        }

        private void EmitDeferredPairMember((int Slot, int Index) member, int destination)
        {
            owner.Op(GetTupleElement, member.Slot == -1 ? XRegisterTag : YRegisterTag, Math.Max(0, member.Slot));
            owner.Operand(BeamWire.UnsignedTag, member.Index);
            owner.Operand(XRegisterTag, destination);
        }

        private bool TryEmitDeferredPairList(Expr.List list, int destination = 0)
        {
            if (deferredPairMembers.Count == 0 || list.Tail is not null || list.Items.Count == 0
                || !list.Items.All(item => item is Expr.Variable variable && deferredPairMembers.ContainsKey(variable.Name)))
            {
                return false;
            }

            int memberRegister = destination + 1;
            owner.EmitConstant(Nil.Value, destination);
            owner.Op(TestHeap, BeamWire.UnsignedTag, list.Items.Count * 2);
            owner.Operand(BeamWire.UnsignedTag, memberRegister);
            foreach (Expr.Variable variable in list.Items.Reverse())
            {
                EmitDeferredPairMember(deferredPairMembers[variable.Name], memberRegister);
                owner.Op(PutList, XRegisterTag, memberRegister);
                owner.Operand(XRegisterTag, destination);
                owner.Operand(XRegisterTag, destination);
            }

            return true;
        }

        private bool TryEmitTupleWithConstantCallResult(Expr.Tuple tuple, int? guardFailure)
        {
            if (guardFailure is not null
                || tuple.Items is not [Expr.Literal { Value: Atom tag }, Expr.Call call]
                || ReadConstantCallResult(call) is not { } value)
            {
                return false;
            }

            // The call still validates its argument and may raise.
            Expression(call);
            owner.EmitConstant(Term.Tuple(tag, value), 0);

            return true;
        }

        private Term? ReadConstantCallResult(Expr.Call call)
        {
            if (call is not { Module: null, DynamicRemote: false }
                || !localFunctions.TryGetValue((call.Function, call.Arguments.Count), out var function)
                || function.Clauses is not [var clause])
            {
                return null;
            }

            Expr result = clause.Body is Expr.Sequence sequence ? sequence.Items[^1] : clause.Body;
            if (result is not Expr.Case { Clauses: [var success, var failure] }
                || !AttributeLiteral.TryNormalizeConstant(success.Body, out Term value)
                || failure.Body is not Expr.Call
                {
                    Function: "exit", Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments.Count: 1
                } exit
                || exit.Module is null && localFunctions.ContainsKey(("exit", 1)))
            {
                return null;
            }

            return value;
        }

        private bool ReturnsTuple(
            Expr expression,
            int minimumArity,
            HashSet<(string Name, int Arity)> active,
            bool exact = false
        )
        {
            switch (expression)
            {
                case Expr.Tuple tuple:
                    return exact ? tuple.Items.Count == minimumArity : tuple.Items.Count >= minimumArity;
                case Expr.Sequence sequence:
                    return ReturnsTuple(
                        sequence.Items[^1],
                        minimumArity,
                        active,
                        exact
                    );
                case Expr.Block block:
                    return ReturnsTuple(
                        block.Body,
                        minimumArity,
                        active,
                        exact
                    );
                case Expr.Case decision:
                    return decision.Clauses.All(clause => ReturnsTuple(
                        clause.Body,
                        minimumArity,
                        active,
                        exact
                    ));
                case Expr.If decision:
                    return decision.Clauses.All(clause => ReturnsTuple(
                        clause.Body,
                        minimumArity,
                        active,
                        exact
                    ));
                case Expr.Call { Module: null, DynamicRemote: false } call
                    when localFunctions.TryGetValue((call.Function, call.Arguments.Count), out var function):
                    var key = (function.Name, function.Arity);
                    if (!active.Add(key))
                    {
                        // A recursive edge can only return a result from another checked clause.
                        return true;
                    }
                    bool proven = function.Clauses.All(clause => ReturnsTuple(
                        clause.Body,
                        minimumArity,
                        active,
                        exact
                    ));
                    active.Remove(key);

                    return proven;
                case Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "exit", Arguments.Count: 1 } exit
                    when owner.ResolvesErlangBif(exit):
                    return true;
                case Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: AutoImportNames.Throw, Arguments.Count: 1
                } thrown when owner.ResolvesErlangBif(thrown):
                    return true;
                default:
                    return false;
            }
        }

        private void EmitArithmetic(Expr.Binary expression, int? guardFailure)
        {
            int operandBase = nextSlot;
            int? left = GuardSlot(expression.Left, guardFailure);
            int? right = GuardSlot(expression.Right, guardFailure);
            if (guardFailure is null)
            {
                var previous = location;
                owner.EmitArithmeticLine(expression.SourceLine, ref previous);
            }
            bool needsGc = expression.Operator != ErlangOperators.Xor;
            owner.Op(needsGc ? GcBifTwo : BifTwo, LabelTag, guardFailure ?? 0);
            if (needsGc)
            {
                owner.Operand(BeamWire.UnsignedTag, ReceiveGuardLiveRegisters);
            }

            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, expression.Operator, 2));
            GuardOperand(expression.Left, left);
            GuardOperand(expression.Right, right);
            owner.Operand(XRegisterTag, 0);
            ReleaseOperandSlots(operandBase);
        }

        private void ReleaseOperandSlots(int operandBase) =>
            nextSlot = Math.Max(operandBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);

        private static bool IsIgnoredListTail(Expr expression, string name) =>
            expression is Expr.Case
            {
                Value: Expr.Variable input,
                Clauses:
                    [
                    {
                        Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null }],
                        Body: Expr.Literal { Value: Atom { Name: "true" } }
                    },
                    {
                        Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Body: Expr.Literal { Value: Atom { Name: "true" } }
                    }
                    ]
            } && input.Name == name;

        private void DiscardSequencePrefix(
            IReadOnlyList<Expr> items,
            int? guardFailure,
            bool reuseCallSlots = false,
            bool rememberRecords = false
        )
        {
            for (int index = 0; index < items.Count - 1; index++)
            {
                DiscardSequenceItem(
                    items,
                    index,
                    guardFailure,
                    reuseCallSlots
                );
                if (rememberRecords)
                {
                    RememberRecordAccesses(items[index]);
                }
            }
        }

        private void DiscardSequenceItem(
            IReadOnlyList<Expr> items,
            int index,
            int? guardFailure,
            bool reuseCallSlots = false
        )
        {
            EmitWithContinuation(
                items[index],
                items.Skip(index + 1),
                guardFailure,
                returnValue: false,
                reuseCallSlots: reuseCallSlots
            );
        }

        private void EmitWithContinuation(
            Expr item,
            IEnumerable<Expr> continuation,
            int? guardFailure,
            bool returnValue,
            bool reuseCallSlots = false,
            bool caseInputPrepared = false
        )
        {
            if (item is Expr.Block block)
            {
                EmitWithContinuation(
                    block.Body,
                    continuation,
                    guardFailure,
                    returnValue,
                    reuseCallSlots
                );

                return;
            }
            if (item is Expr.Sequence sequence)
            {
                for (int index = 0; index < sequence.Items.Count - 1; index++)
                {
                    EmitWithContinuation(
                        sequence.Items[index],
                        sequence.Items.Skip(index + 1).Concat(continuation),
                        guardFailure,
                        returnValue: false,
                        reuseCallSlots: reuseCallSlots
                    );
                }

                EmitWithContinuation(
                    sequence.Items[^1],
                    continuation,
                    guardFailure,
                    returnValue,
                    reuseCallSlots
                );

                return;
            }

            if (!caseInputPrepared && reuseCallSlots && !returnValue && guardFailure is null
                && item is Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call } nestedMatch
                && tuple.Items.Any(field => field is Pattern.Tuple) && IsPlainTuplePattern(tuple))
            {
                var scope = new Clause([tuple], null, new Expr.Sequence(continuation.ToArray()));
                Pattern pattern = OmitUnusedCaseBindings(scope, null, null).Patterns[0];
                item = nestedMatch with { Pattern = pattern };
            }

            if (!caseInputPrepared && reuseCallSlots && !returnValue && guardFailure is null
                && (TryBindSplitBinaryResult(item, continuation)
                    || TryReuseBindingSlot(item, continuation, variables.Values.OrderDescending())))
            {
                return;
            }

            var match = item as Expr.Match;
            Expr value = match?.Value ?? item;
            if (match is not null)
            {
                continuation = continuation.Prepend(match with { Value = new Expr.Literal(Nil.Value) });
            }

            string[] exports;
            bool reuseBranchSlots = reuseCallSlots && guardFailure is null
                && (match is null || match.Pattern is Pattern.Variable { });
            switch (value)
            {
                case Expr.Case decision:
                    exports = ReadExports(decision.Clauses);
                    if (!caseInputPrepared && exports.Length == 0)
                    {
                        if (!reuseBranchSlots || !returnValue && IsDiscardedListTail(item))
                        {
                            break;
                        }
                        if (!returnValue && match is not null && TryBindDecodedResult(match))
                        {
                            return;
                        }
                    }

                    EmitCase(
                        decision,
                        guardFailure,
                        inputPrepared: caseInputPrepared,
                        exports: exports.Length == 0 ? null : exports,
                        branchContinuation: reuseBranchSlots ? continuation.ToArray() : null
                    );
                    BindResult();

                    return;
                case Expr.If conditional when guardFailure is null:
                    exports = ReadExports(conditional.Clauses);
                    if (exports.Length == 0 && !reuseBranchSlots)
                    {
                        break;
                    }

                    Conditional(conditional, exports, reuseBranchSlots ? continuation.ToArray() : null);
                    BindResult();

                    return;
                case Expr.Receive receive when guardFailure is null:
                    exports = ReadExports(receive.Clauses, receive.After);
                    if (exports.Length == 0)
                    {
                        break;
                    }

                    EmitReceive(receive, exports);
                    BindResult();

                    return;
            }

            if (returnValue)
            {
                Expression(item, guardFailure);
            }
            else
            {
                Discard(item, guardFailure);
            }

            string[] ReadExports(IReadOnlyList<Clause> clauses, Expr? after = null) =>
                ClosureCaptures.ReadBranchExports(
                    clauses,
                    variables.Keys.Concat(deferredPairMembers.Keys),
                    continuation,
                    after
                );

            void BindResult()
            {
                if (match is not null)
                {
                    MatchValue(match, Save(), returnValue);
                }
            }
        }

        private static bool IsPlainTuplePattern(Pattern pattern) => pattern switch
        {
            Pattern.Tuple tuple => tuple.Items.All(IsPlainTuplePattern),
            Erlang.Pattern.Variable or Erlang.Pattern.Literal or Erlang.Pattern.Any => true,
            _ => false
        };

        private bool IsDiscardedListTail(Expr expression) =>
            expression is Expr.Case { Value: Expr.Variable input }
            && variables.ContainsKey(input.Name) && IsIgnoredListTail(expression, input.Name);

        private bool TryBindSplitBinaryResult(Expr item, IEnumerable<Expr> continuation)
        {
            if (owner.receiveScratchRegisters is not null || item is not Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] },
                    Value: Expr.Call { Function: AutoImportNames.SplitBinary, DynamicRemote: false, Arguments.Count: 2 } call
                }
                || call.Arguments.Any(argument => argument is not (Expr.Variable or Expr.Literal))
                || (call.Module ?? owner.ResolveExternalModule(call.Function, call.Arguments.Count)) != ErlangModuleNames.Module
                || call.Module is null && entries.ContainsKey((call.Function, call.Arguments.Count)))
            {
                return false;
            }

            Pattern.Variable[] fields = [first, second];
            if (!DistinctBindings(fields)
                || fields.Any(field => variables.ContainsKey(field.Name) || deferredPairMembers.ContainsKey(field.Name))
                || ReadUsedEntryParameters(new Clause(fields, null, new Expr.Sequence(continuation.ToArray()))) is not { } used)
            {
                return false;
            }

            Expression(call);
            for (int index = 0; index < fields.Length; index++)
            {
                string name = fields[index].Name;
                if (used.Contains(name))
                {
                    int slot = Slot();
                    owner.Op(GetTupleElement, XRegisterTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, index);
                    owner.Operand(YRegisterTag, slot);
                    variables.Add(name, slot);
                }
            }

            return true;
        }

        private void Discard(Expr expression, int? guardFailure)
        {
            if (IsDiscardedListTail(expression))
            {
                return;
            }
            if (expression is Expr.Match match)
            {
                Match(match, guardFailure, returnValue: false);
            }
            else
            {
                Expression(expression, guardFailure);
            }
        }

        private (Expr.Call Call, string Module)? ReadEntryCaseExternalTail(Expr body)
        {
            if (body is not Expr.Call { DynamicRemote: false } source)
            {
                return null;
            }

            Expr.Call call = owner.ResolveImportedCall(source, entries);
            int arity = call.Arguments.Count;
            if (call.Module is null && entries.ContainsKey((call.Function, arity)))
            {
                return null;
            }

            string? module = call.Module ?? owner.ResolveExternalModule(call.Function, arity);

            return module is not null
                && (module != ErlangModuleNames.Module || call.Function == AutoImportNames.Apply && arity is 2 or 3)
                && call.Arguments.All(argument => argument is Expr.Variable { }
                    || AttributeLiteral.TryNormalizeConstant(argument, out _))
                ? (call, module) : null;
        }

        private Expr.Case? FoldEntryCaseFallback(Expr.Case decision, IReadOnlyList<Pattern> parameters)
        {
            if (decision.Clauses is not [
                { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Literal { } or Pattern.Variable { }] }] } first,
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                    Body: Expr.Call { Module: null, DynamicRemote: false } skipped
                },
                { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Call { Module: null, DynamicRemote: false } target } last]
                || fallback.Name != VariableScopeNames.Wildcard && parameters.OfType<Pattern.Variable>()
                    .Any(parameter => parameter.Name == fallback.Name)
                || skipped.Function != target.Function || skipped.Arguments.Count != target.Arguments.Count
                || !entries.ContainsKey((target.Function, target.Arguments.Count))
                || !skipped.Arguments.Zip(target.Arguments).All(pair => SameOperand(pair.First, pair.Second)))
            {
                return null;
            }

            return decision with { Clauses = [first, last with { Patterns = [new Pattern.Variable(VariableScopeNames.Wildcard)] }] };

            bool SameOperand(Expr left, Expr right) => left is Expr.Variable variable
                ? right is Expr.Variable other && variable.Name == other.Name && variable.Name != fallback.Name
                : AttributeLiteral.TryNormalizeConstant(left, out Term literal)
                    && AttributeLiteral.TryNormalizeConstant(right, out Term otherLiteral) && literal.Equals(otherLiteral);
        }

        private void EmitCase(
            Expr.Case decision,
            int? guardFailure,
            (int First, int Second)? destinations = null,
            bool inputPrepared = false,
            bool returnResult = false,
            IReadOnlyList<string>? exports = null,
            bool reuseBranchSlots = false,
            Expr[]? branchContinuation = null,
            bool tailFallback = false,
            List<(int Label, BeamSourceLocation? Location)>? deferredBooleanFailures = null,
            (int True, int False)? reservedBooleanLabels = null,
            bool terminalCase = false
        )
        {
            if (!inputPrepared && guardFailure is null
                && TryEmitPreparedTupleCase(
                    decision,
                    destinations: destinations,
                    returnResult: returnResult,
                    exports: exports,
                    reuseSelectorSlots: reuseBranchSlots,
                    reuseBranchSlots: reuseBranchSlots,
                    branchContinuation: branchContinuation
                ))
            {
                return;
            }

            if (exports is null && !inputPrepared && destinations is null && guardFailure is null && TryEmitSingletonUnwrap(decision))
            {
                return;
            }

            var incomingRecords = checkedRecordBindings;
            int? savedInput = null;
            string? selectorBinding = null;
            if (!inputPrepared)
            {
                if (decision.Value is Expr.Variable variable && variables.TryGetValue(variable.Name, out int slot))
                {
                    savedInput = slot;
                    selectorBinding = variable.Name;
                }
                else
                {
                    Expression(decision.Value, guardFailure);
                    if (guardFailure is null)
                    {
                        RememberRecordAccesses(decision.Value);
                    }
                }
            }
            if (exports is null && destinations is null && guardFailure is null
                && TryCaseSelection(
                    decision,
                    savedInput,
                    returnResult,
                    reuseBranchSlots,
                    branchContinuation,
                    deferredBooleanFailures,
                    reservedBooleanLabels,
                    allowUnmatchedAtoms: inputPrepared
                ))
            {
                checkedRecordBindings = incomingRecords;
                return;
            }
            bool testInX0 = savedInput is null && guardFailure is null
                && decision.Clauses is [
                {
                    Guard: null, Patterns: [Pattern.Literal { }
                        or Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Literal { } or Pattern.Variable { }] }]
                },
                { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
                ];
            bool registerPatterns = !testInX0 && savedInput is null && guardFailure is null
                && destinations is null && exports is null && owner.receiveScratchRegisters is null
                && decision.Clauses.All(CanTestCasePatternInX0);
            int? input = testInX0 || registerPatterns ? null : savedInput ?? Save();
            EmitCaseClauses(
                decision.Clauses,
                input,
                guardFailure,
                destinations,
                returnResult,
                exports: exports,
                reuseBranchSlots: reuseBranchSlots,
                branchContinuation: branchContinuation,
                tailFallback: tailFallback,
                registerPatterns: registerPatterns,
                terminalCase: terminalCase,
                selectorBinding: selectorBinding
            );
            checkedRecordBindings = incomingRecords;
        }

        private bool CanTestCasePatternInX0(Clause clause) =>
            clause.Patterns is [var pattern]
            && (pattern is Erlang.Pattern.Variable or Erlang.Pattern.Literal
                || pattern is (Erlang.Pattern.Tuple or AliasPattern)
                    && EntryShape(pattern) is Erlang.Pattern.Tuple tuple && CanTestTupleInX0(tuple))
            && (clause.Guard is null
                || CanTestCaseGuardInX0(clause.Guard)
                    && LocalFunctionEntries.Walk(clause.Guard).OfType<Expr.Variable>()
                        .All(variable => !deferredPairMembers.ContainsKey(variable.Name)));

        private bool CanTestCaseGuardInX0(Expr guard) => guard switch
        {
            Expr.Literal { Value: Atom { Name: "true" } } => true,
            Expr.GuardAlternatives alternatives => alternatives.Items.All(CanTestCaseGuardInX0),
            Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction =>
                CanTestCaseGuardInX0(conjunction.Left) && CanTestCaseGuardInX0(conjunction.Right),
            Expr.Binary { Operator: ErlangOperators.OrElse } disjunction =>
                IsTotalTypeGuard(disjunction.Left) && CanTestCaseGuardInX0(disjunction.Right),
            Expr.Binary comparison when GuardComparisonOpcode(comparison) != 0 =>
                comparison.Left is Expr.Variable or Expr.Literal && comparison.Right is Expr.Variable or Expr.Literal,
            _ => IsTotalTypeGuard(guard)
        };

        private static bool CanTestTupleInX0(Pattern.Tuple tuple) =>
            tuple.Items.All(item => item is Erlang.Pattern.Variable or Erlang.Pattern.Any or Erlang.Pattern.Literal
                || item is Pattern.Tuple nested && CanTestTupleInX0(nested));

        private void EmitReturnExpression(Expr body, bool reuseCallSlots = false, bool terminalCase = false)
        {
            var incomingRecords = checkedRecordBindings;
            if (reuseCallSlots && body is Expr.Sequence sequence)
            {
                DiscardSequencePrefix(sequence.Items, null, reuseCallSlots: true, rememberRecords: true);
                body = sequence.Items[^1];
            }

            if (body is Expr.Case decision)
            {
                if (reuseCallSlots)
                {
                    EmitCase(
                        decision,
                        null,
                        reuseBranchSlots: true,
                        terminalCase: terminalCase
                    );

                    checkedRecordBindings = incomingRecords;

                    return;
                }
                if (TryEmitPreparedTupleCase(decision, reuseSelectorSlots: true))
                {
                    checkedRecordBindings = incomingRecords;
                    return;
                }
            }

            if (reuseCallSlots && body is Expr.If conditional)
            {
                Conditional(conditional, branchContinuation: []);

                checkedRecordBindings = incomingRecords;

                return;
            }

            Expression(body);
            checkedRecordBindings = incomingRecords;
        }

        private bool TryEmitPreparedTupleCase(
            Expr.Case decision,
            string? x0Binding = null,
            (int First, int Second)? destinations = null,
            bool returnResult = false,
            IReadOnlyList<string>? exports = null,
            bool reuseSelectorSlots = false,
            bool reuseBranchSlots = false,
            Expr[]? branchContinuation = null
        )
        {
            if (decision.Value is not Expr.Tuple tuple
                || !tuple.Items.All(item => item is Expr.Variable variable
                    && (variable.Name == x0Binding || variables.ContainsKey(variable.Name)))
                || !decision.Clauses.All(clause => clause.Patterns is
                    [Pattern.Variable { Name: VariableScopeNames.Wildcard } or Pattern.Any { }]
                    || clause.Patterns is [Pattern.Tuple pattern] && pattern.Items.Count == tuple.Items.Count
                    && pattern.Items.All(item => item is Pattern.Variable { } or Pattern.Any { } or Pattern.Literal { })))
            {
                return false;
            }

            int?[] slots = tuple.Items.Cast<Expr.Variable>()
                .Select(variable => variable.Name == x0Binding ? (int?)null : variables[variable.Name]).ToArray();
            (int? Slot, Term? Constant)[]? failureOperands = null;
            if (x0Binding is not null)
            {
                var branches = decision with { Value = new Expr.Literal(Nil.Value) };
                if (ReadUsedEntryParameters(new Clause([new Pattern.Variable(x0Binding)], null, branches)) is not { Count: 0 }
                    || decision.Clauses.Any(clause => clause.Guard is not null
                        || clause.Patterns[0] is Pattern.Tuple pattern && pattern.Items.Where((_, index) => slots[index] is null)
                            .Any(item => item is Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                                && !variables.ContainsKey(variable.Name)))
                    || !TryReadBoundTupleOperands(
                        tuple,
                        x0Binding,
                        variables,
                        out failureOperands
                    ))
                {
                    return false;
                }
            }

            EmitCaseClauses(
                decision.Clauses,
                input: 0,
                destinations: destinations,
                returnResult: returnResult,
                exports: exports,
                tupleInput: (tuple, slots, failureOperands, reuseSelectorSlots || returnResult),
                reuseBranchSlots: reuseBranchSlots || returnResult,
                branchContinuation: branchContinuation
            );

            return true;
        }

        private void EmitCaseClauses(
            IReadOnlyList<Clause> clauses,
            int? input,
            int? guardFailure = null,
            (int First, int Second)? destinations = null,
            bool returnResult = false,
            string? failureReason = null,
            IReadOnlyList<string>? exports = null,
            (Expr.Tuple Value, int?[] Slots, (int? Slot, Term? Constant)[]? FailureOperands, bool ReuseSlots)? tupleInput = null,
            bool reuseBranchSlots = false,
            Expr[]? branchContinuation = null,
            bool tailFallback = false,
            bool registerPatterns = false,
            bool terminalCase = false,
            string? selectorBinding = null
        )
        {
            int? firstAlternative = linearBindings && clauses is [
            { Guard: null, Patterns: [Pattern.Literal { }] },
            { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
            ] ? Label() : null;
            var externalTail = tailFallback ? ReadEntryCaseExternalTail(clauses[0].Body) : null;
            bool tailBoolean = tailFallback && clauses[0].Body is Expr.Case booleanCase && IsTailBooleanCase(booleanCase);
            int end = externalTail is null && !tailBoolean ? Label() : 0;
            List<(int Label, BeamSourceLocation? Location)>? booleanFailures = tailBoolean ? [] : null;
            (int True, int False)? booleanLabels = tailBoolean ? (Label(), Label()) : null;
            var original = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            var originalRecordBindings = checkedRecordBindings;
            var joined = ReserveBranchExports(exports);

            int caseBase = nextSlot;
            int caseLimit = caseBase;
            bool exhausted = true;
            int groupedClauses = 0;
            int arityFallback = 0;
            var arityEntries = new Dictionary<int, (int Label, (Atom Tag, int Label)[] Tags)>();
            var tagEntries = new Dictionary<int, int>();
            var groupEnds = new HashSet<int>();
            bool savedTupleSelector = input.HasValue && tupleInput is null && guardFailure is null
                && owner.receiveScratchRegisters is null;
            if ((registerPatterns || savedTupleSelector) && !tailFallback)
            {
                var groups = clauses.TakeWhile(clause => EntryShape(clause.Patterns[0]) is Pattern.Tuple)
                    .GroupBy(clause => ((Pattern.Tuple)EntryShape(clause.Patterns[0])).Items.Count)
                    .OrderByDescending(group => group.Key).ToArray();
                if (groups.Length > 1 || (groups.Length == 1 && groups[0].Count() > 1
                    && (groups[0].Any(clause => !HasCaseTupleTag(clause)) || groups[0].Select(TupleTag).Distinct().Skip(1).Any())))
                {
                    var dispatch = new List<(int Arity, int Label)>();
                    var orderedClauses = new List<Clause>();
                    foreach (var group in groups)
                    {
                        int label = groups.Length == 1 ? 0 : Label();
                        dispatch.Add((group.Key, label));
                        int arityStart = orderedClauses.Count;
                        if (group.Any(clause => !HasCaseTupleTag(clause)))
                        {
                            orderedClauses.AddRange(group);
                            groupEnds.Add(orderedClauses.Count - 1);
                            arityEntries.Add(arityStart, (label, []));
                            continue;
                        }

                        var tags = group.GroupBy(TupleTag)
                            .OrderBy(tag => tag.Key, Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                            .ToArray();
                        var table = new (Atom Tag, int Label)[tags.Length > 1 ? tags.Length : 0];
                        for (int tagIndex = tags.Length - 1; tagIndex >= 0; tagIndex--)
                        {
                            if (table.Length != 0)
                            {
                                int tagLabel = Label();
                                table[tagIndex] = (tags[tagIndex].Key, tagLabel);
                                tagEntries.Add(orderedClauses.Count, tagLabel);
                            }

                            orderedClauses.AddRange(tags[tagIndex]);
                            groupEnds.Add(orderedClauses.Count - 1);
                        }

                        arityEntries.Add(arityStart, (label, table));
                    }

                    groupedClauses = orderedClauses.Count;
                    arityFallback = Label();
                    int selectorTag = input.HasValue ? YRegisterTag : XRegisterTag;
                    if (dispatch.Count == 1)
                    {
                        EmitTupleShapeTest(selectorTag, input ?? 0, dispatch[0].Arity, arityFallback);
                    }
                    else
                    {
                        dispatch.Reverse();
                        owner.EmitTupleArityDispatch(arityFallback, dispatch, input ?? 0, selectorTag);
                    }

                    clauses = [.. orderedClauses, .. clauses.Skip(groupedClauses)];
                }
            }

            bool tagKnown = false;
            for (int clauseIndex = 0; clauseIndex < clauses.Count; clauseIndex++)
            {
                if (arityEntries.TryGetValue(clauseIndex, out var arityEntry))
                {
                    if (arityEntry.Label != 0)
                    {
                        Mark(arityEntry.Label);
                    }

                    tagKnown = arityEntry.Tags.Length != 0;
                    if (tagKnown)
                    {
                        EmitRegisterCaseTagSelection(arityEntry.Tags, arityFallback, savedSelector: input);
                    }
                }

                if (tagEntries.TryGetValue(clauseIndex, out int tagEntry))
                {
                    Mark(tagEntry);
                }

                if (clauseIndex == groupedClauses && arityFallback != 0)
                {
                    Mark(arityFallback);
                    tagKnown = false;
                }

                variables = new(original, StringComparer.Ordinal);
                checkedRecordBindings = new(originalRecordBindings, StringComparer.Ordinal);
                nextSlot = caseBase;
                Clause clause = OmitUnusedCaseBindings(clauses[clauseIndex], exports, branchContinuation);
                if (tupleInput is { } prepared && clause.Patterns[0] is Pattern.Tuple tuple)
                {
                    bool unconditional = clause.Guard is null && IsUnconditionalTuple(tuple);
                    int nextTupleClause = unconditional ? 0 : Label();
                    var indices = Enumerable.Range(0, prepared.Slots.Length)
                        .OrderBy(index => prepared.Slots[index] is null && tuple.Items[index] is Pattern.Literal { } ? 0 : 1);
                    foreach (int index in indices)
                    {
                        if (prepared.Slots[index] is int slot)
                        {
                            Pattern(tuple.Items[index], slot, nextTupleClause);
                        }
                        else if (tuple.Items[index] is Pattern.Literal literal)
                        {
                            owner.Op(ExactEqualTest, LabelTag, nextTupleClause);
                            owner.Operand(XRegisterTag, 0);
                            owner.LiteralOperand(literal.Value);
                        }
                        else if (tuple.Items[index] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } bound)
                        {
                            owner.Op(ExactEqualTest, LabelTag, nextTupleClause);
                            owner.Operand(XRegisterTag, 0);
                            owner.Operand(YRegisterTag, variables[bound.Name]);
                        }
                    }

                    Guard(clause.Guard, nextTupleClause);
                    EmitBranchResult(clause.Body);
                    caseLimit = Math.Max(caseLimit, nextSlot);
                    if (unconditional)
                    {
                        exhausted = false;
                        break;
                    }

                    JumpTo(end);
                    Mark(nextTupleClause);
                    continue;
                }

                if (clause.Guard is null or Expr.Literal { Value: Atom { Name: "true" } }
                    && clause.Patterns[0] is Pattern.Variable variable
                    && (variable.Name == VariableScopeNames.Wildcard || !variables.ContainsKey(variable.Name)))
                {
                    if (registerPatterns && !tailFallback && destinations is null && joined.Length == 0 && branchContinuation is null
                        && clause.Body is Expr.Variable returnedSelector && returnedSelector.Name == variable.Name)
                    {
                        exhausted = false;
                        break;
                    }

                    if (input is int boundInput)
                    {
                        Pattern(variable, boundInput, 0);
                    }
                    else if (registerPatterns)
                    {
                        Pattern(
                            variable,
                            0,
                            0,
                            valueTag: XRegisterTag
                        );
                    }

                    if (!tailFallback || !TailLocal(clause.Body, caseTail: true))
                    {
                        EmitBranchResult(clause.Body);
                    }
                    caseLimit = Math.Max(caseLimit, nextSlot);
                    exhausted = false;
                    break;
                }

                bool arityKnown = clauseIndex < groupedClauses;
                bool lastInGroup = groupEnds.Contains(clauseIndex);
                int next = lastInGroup ? arityFallback : firstAlternative ?? Label();
                firstAlternative = null;
                bool tailCall = branchContinuation is null && (terminalCase || returnResult) && clause.Body is Expr.Call;
                bool savedRegisterArm = savedTupleSelector && destinations is null && joined.Length == 0;
                if ((savedRegisterArm || registerPatterns && branchContinuation is null) && !tailFallback
                    && TryEmitRegisterTupleCaseArm(
                        clauses[clauseIndex],
                        next,
                        tailCall,
                        arityKnown,
                        tagKnown,
                        savedSelector: input
                    ))
                {
                    if (savedRegisterArm)
                    {
                        caseLimit = Math.Max(caseLimit, nextSlot);
                    }
                    if (!tailCall)
                    {
                        JumpTo(end);
                    }

                    if (!lastInGroup)
                    {
                        Mark(next);
                    }

                    continue;
                }
                string? guardSelector = registerPatterns && clause.Guard is not null
                    && clause.Patterns[0] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } guarded
                    && !variables.ContainsKey(guarded.Name) ? guarded.Name : null;
                var guardedAlias = registerPatterns ? ReadRegisterCaseGuardAlias(clause) : null;
                if (input is int matchedInput)
                {
                    var checkedShape = arityKnown ? EntryShape(clause.Patterns[0]) : null;
                    bool checkedTag = tagKnown;
                    if (!checkedTag && checkedShape is Pattern.Tuple tupleShape && HasTupleTag(tupleShape, out Atom tupleTag))
                    {
                        int selector = EmitCaseTupleTag(matchedInput);
                        owner.Op(ExactEqualTest, LabelTag, next);
                        owner.Operand(XRegisterTag, selector);
                        owner.LiteralOperand(tupleTag);
                        checkedTag = true;
                    }

                    Pattern(
                        clause.Patterns[0],
                        matchedInput,
                        next,
                        checkedShape: checkedShape,
                        checkedTupleTag: checkedTag,
                        entryPatternScratch: arityKnown,
                        tupleScratch: reuseBranchSlots && guardFailure is null && owner.receiveScratchRegisters is null ? 1 : null
                    );
                }
                else if (guardSelector is not null)
                {
                    Guard(clause.Guard, next, guardSelector);
                    Pattern(clause.Patterns[0], 0, next, valueTag: XRegisterTag);
                }
                else if (registerPatterns)
                {
                    var pattern = clause.Patterns[0];
                    var shape = EntryShape(pattern) as Pattern.Tuple;
                    if (!tagKnown && shape is not null && HasTupleTag(shape, out Atom shapeTag))
                    {
                        EmitCaseTupleTest(
                            shape.Items.Count,
                            shapeTag,
                            next,
                            arityKnown
                        );
                    }

                    Pattern(
                        guardedAlias is null ? pattern : shape!,
                        0,
                        next,
                        arityKnown || (shape is not null && HasTupleTag(shape, out _)) ? shape : null,
                        entryPatternScratch: true,
                        valueTag: XRegisterTag,
                        tupleScratch: 1
                    );
                }
                else if (clause.Patterns[0] is Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, var field] })
                {
                    EmitDetailRecordTest(
                        0,
                        2,
                        tag,
                        next
                    );
                    if (field is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                    {
                        owner.EmitTuplePart(0, 1, 0);
                        if (field is Pattern.Variable variableField && !variables.ContainsKey(variableField.Name))
                        {
                            variables.Add(variableField.Name, Save());
                        }
                        else
                        {
                            owner.Op(ExactEqualTest, LabelTag, next);
                            owner.Operand(XRegisterTag, 0);
                            if (field is Pattern.Variable boundField)
                            {
                                owner.Operand(YRegisterTag, variables[boundField.Name]);
                            }
                            else
                            {
                                owner.LiteralOperand(((Pattern.Literal)field).Value);
                            }
                        }
                    }
                }
                else
                {
                    owner.Op(ExactEqualTest, LabelTag, next);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(((Pattern.Literal)clause.Patterns[0]).Value);
                }


                if (guardedAlias is not null)
                {
                    Guard(clause.Guard, next, guardedAlias.Name);
                    var body = branchContinuation is null ? clause.Body : new Expr.Sequence([clause.Body, .. branchContinuation]);
                    if (exports?.Contains(guardedAlias.Name) == true
                        || ReadUsedEntryParameters(new Clause([guardedAlias], null, body)) is not { Count: 0 })
                    {
                        variables.Add(guardedAlias.Name, Save());
                    }

                    guardSelector = guardedAlias.Name;
                }

                RememberCheckedRecord(clause.Patterns[0]);
                if (selectorBinding is not null && EntryShape(clause.Patterns[0]) is Pattern.Tuple testedTuple
                    && HasTupleTag(testedTuple, out Atom testedTag))
                {
                    RememberRecordShape(selectorBinding, testedTag.Name, testedTuple.Items.Count);
                }
                if (guardSelector is null)
                {
                    Guard(clause.Guard, next);
                }

                if (externalTail is { } tail)
                {
                    EmitCaseExternalTail(tail.Call, tail.Module);
                }
                else if (tailBoolean)
                {
                    EmitCase(
                        (Expr.Case)clause.Body,
                        null,
                        returnResult: true,
                        deferredBooleanFailures: booleanFailures,
                        reservedBooleanLabels: booleanLabels
                    );
                }
                else
                {
                    EmitBranchResult(clause.Body);
                    JumpTo(end);
                }
                caseLimit = Math.Max(caseLimit, nextSlot);
                if (!lastInGroup)
                {
                    Mark(next);
                }
            }
            if (arityFallback != 0 && groupedClauses == clauses.Count)
            {
                Mark(arityFallback);
            }

            checkedRecordBindings = originalRecordBindings;
            if (exhausted)
            {
                if (tupleInput is { } prepared)
                {
                    variables = original;
                    nextSlot = caseBase;
                    if (prepared.FailureOperands is { } operands)
                    {
                        EmitTupleWithX0(operands);
                    }
                    else
                    {
                        Expression(prepared.Value);
                    }
                    owner.Op(CaseEnd, XRegisterTag, 0);
                }
                else if (failureReason is null)
                {
                    if (input is int failedInput)
                    {
                        Load(failedInput);
                    }

                    owner.Op(CaseEnd, XRegisterTag, 0);
                }
                else
                {
                    ErrorTuple(failureReason, input!.Value);
                }
            }
            if (booleanFailures is not null)
            {
                foreach (var (label, failureLocation) in booleanFailures)
                {
                    EmitBooleanCaseFailure(label, failureLocation);
                }
            }
            if (end != 0)
            {
                Mark(end);
            }
            variables = original;
            foreach (var (name, slot) in joined)
            {
                variables.Add(name, slot);
            }
            nextSlot = caseLimit;
            if (returnResult && end != 0)
            {
                owner.EmitFrameReturn(frameSize);
            }

            void EmitBranchResult(Expr body)
            {
                if (branchContinuation is not null && destinations is null && guardFailure is null)
                {
                    EmitExportingBranch(body, joined, branchContinuation: branchContinuation);
                }
                else if (joined.Length == 0)
                {
                    if (destinations is null && guardFailure is null
                        && tupleInput is { ReuseSlots: true } prepared)
                    {
                        body = ReuseSelectorSlot(body, prepared.Slots);
                    }

                    if (reuseBranchSlots && destinations is null && guardFailure is null)
                    {
                        EmitReturnExpression(body, reuseCallSlots: true);
                    }
                    else
                    {
                        EmitCaseResult(body, guardFailure, destinations);
                    }
                }
                else
                {
                    EmitExportingBranch(body, joined, guardFailure);
                }
                StoreBranchExports(joined);
            }

            bool IsUnconditionalTuple(Pattern.Tuple tuple)
            {
                var names = new HashSet<string>(original.Keys, StringComparer.Ordinal);

                return tuple.Items.All(item => item is Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    || item is Pattern.Variable variable && names.Add(variable.Name));
            }
        }

        private Clause OmitUnusedCaseBindings(Clause clause, IReadOnlyList<string>? exports, Expr[]? continuation)
        {
            var pattern = clause.Patterns[0];
            if (pattern is not (Erlang.Pattern.Tuple or AliasPattern or Erlang.Pattern.Variable))
            {
                return clause;
            }

            var removable = Semantics.Variables(pattern)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() == 1 && !variables.ContainsKey(group.Key)
                    && !deferredPairMembers.ContainsKey(group.Key) && exports?.Contains(group.Key) != true)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);
            if (removable.Count == 0)
            {
                return clause;
            }

            Clause scope = continuation is { Length: > 0 }
                ? clause with { Body = new Expr.Sequence([clause.Body, .. continuation]) }
                : clause;
            try
            {
                removable.ExceptWith(ClosureCaptures.ReadEntryReferences(scope, removable));
            }
            catch (NotSupportedException)
            {
                return clause;
            }

            return removable.Count == 0 ? clause : clause with { Patterns = [WithoutUnusedBindings(pattern)] };

            Pattern WithoutUnusedBindings(Pattern current) => current switch
            {
                Pattern.Variable variable when removable.Contains(variable.Name) => new Pattern.Variable(VariableScopeNames.Wildcard),
                Pattern.Tuple tuple => tuple with { Items = tuple.Items.Select(WithoutUnusedBindings).ToArray() },
                AliasPattern alias => alias with
                {
                    Left = WithoutUnusedBindings(alias.Left),
                    Right = WithoutUnusedBindings(alias.Right)
                },
                _ => current
            };
        }

        private Pattern.Variable? ReadRegisterCaseGuardAlias(Clause clause)
        {
            if (clause.Guard is not { } guard || !CanTestCaseGuardInX0(guard)
                || clause.Patterns[0] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var binding, out var shape)
                || shape is not Pattern.Tuple { } || variables.ContainsKey(binding.Name)
                || Semantics.Variables(shape).Contains(binding.Name))
            {
                return null;
            }

            return binding;
        }

        private bool TryEmitTerminalIdentityCase(Expr.Case decision, Expr.Call selector)
        {
            if (decision.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }],
                        Body: Expr.Literal { Value: Atom result }
                    }
                ] || !key.Equals(result))
            {
                return false;
            }

            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key);
            owner.EmitFrameReturn(frameSize);
            var caseLocation = new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine);
            EmitBooleanCaseFailure(
                failure,
                caseLocation.Line == 0 || caseLocation == CallLocation(selector, location) ? null : caseLocation
            );

            return true;
        }

        private bool TryEmitTerminalTaggedCase(Expr.Case decision)
        {
            if (decision.Clauses.Count < 2)
            {
                return false;
            }

            const int payloadRegister = 2;
            var tags = new HashSet<Atom>();
            var branches = new List<(Atom Tag, Clause Clause)>();
            foreach (Clause clause in decision.Clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable field] }]
                    }
                    || field.Name == VariableScopeNames.Wildcard || variables.ContainsKey(field.Name)
                    || !tags.Add(tag)
                    || ReadUsedEntryParameters(new Clause([field], null, clause.Body)) is not { Count: 1 }
                    || !TryEmitRegisterTupleCaseArm(
                        clause,
                        failure: 0,
                        tailCall: clause.Body is Expr.Call,
                        arityKnown: true,
                        tagKnown: true,
                        payloadRegister: payloadRegister,
                        validateOnly: true
                    ))
                {
                    return false;
                }

                branches.Add((tag, clause));
            }

            branches.Sort((left, right) => TermOrder.Compare(left.Tag, right.Tag, exact: true));
            var table = new (Atom Tag, int Label)[branches.Count];
            for (int index = table.Length - 1; index >= 0; index--)
            {
                table[index] = (branches[index].Tag, Label());
            }

            int failure = Label();
            owner.EmitTupleCheck(0, 2, failure);
            EmitRegisterCaseTagSelection(table, failure, payloadRegister);
            for (int index = table.Length - 1; index >= 0; index--)
            {
                Clause clause = branches[index].Clause;
                bool tailCall = clause.Body is Expr.Call;
                Mark(table[index].Label);
                TryEmitRegisterTupleCaseArm(
                    clause,
                    failure,
                    tailCall,
                    arityKnown: true,
                    tagKnown: true,
                    payloadRegister: payloadRegister
                );
                if (!tailCall)
                {
                    owner.EmitFrameReturn(frameSize);
                }
            }

            EmitBooleanCaseFailure(
                failure,
                decision.SourceLine == 0 ? null : new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine)
            );
            return true;
        }

        private bool TryEmitRegisterTupleCaseArm(
            Clause clause,
            int failure,
            bool tailCall,
            bool arityKnown,
            bool tagKnown,
            int? payloadRegister = null,
            bool validateOnly = false,
            int? savedSelector = null
        )
        {
            Pattern pattern = clause.Patterns[0];
            if (EntryShape(pattern) is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag)
                || tuple.Items.Any(item => item is Pattern.Tuple))
            {
                return false;
            }

            if (savedSelector.HasValue && (clause.Guard is not null || pattern is AliasPattern
                || tuple.Items.Any(item => item is not (Pattern.Variable { } or Pattern.Any { } or Pattern.Literal { }))))
            {
                return false;
            }

            var guardBinding = ReadRegisterCaseGuardAlias(clause);
            if (clause.Guard is { } guard && (guardBinding is null
                || LocalFunctionEntries.Walk(guard).OfType<Expr.Variable>().Any(variable =>
                    variable.Name != guardBinding.Name && !variables.ContainsKey(variable.Name))))
            {
                return false;
            }

            var value = clause.Body is Expr.RecordConstruction record ? RecordExpressions.Create(record) : clause.Body;
            if (value is not (Expr.Variable or Expr.Tuple or Expr.List or Expr.Call))
            {
                return false;
            }

            var usedNames = LocalFunctionEntries.Walk(value).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            if (pattern is AliasPattern alias)
            {
                if (!TryReadPatternAlias(alias, out var binding, out _) || variables.ContainsKey(binding.Name)
                    || tuple.Items.OfType<Pattern.Variable>().Any(field => field.Name == binding.Name))
                {
                    return false;
                }

                if (usedNames.Contains(binding.Name))
                {
                    registers.Add(binding.Name, 0);
                }
            }

            var fields = new List<(int Field, int Register)>();
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            Pattern[] tests = tuple.Items.ToArray();
            for (int field = 1; field < tests.Length; field++)
            {
                if (tests[field] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                    || variables.ContainsKey(variable.Name))
                {
                    continue;
                }

                if (!fieldNames.Add(variable.Name))
                {
                    return false;
                }

                tests[field] = new Pattern.Variable(VariableScopeNames.Wildcard);
                if (!usedNames.Contains(variable.Name))
                {
                    continue;
                }

                int register = payloadRegister ?? fields.Count + 1;
                if (register >= FirstReservedXRegister)
                {
                    return false;
                }

                registers.Add(variable.Name, register);
                if (payloadRegister is null)
                {
                    fields.Add((field, register));
                }
            }

            if ((fieldNames.Count == 0 && registers.Count == 0) || (savedSelector.HasValue && fields.Count == 0))
            {
                return false;
            }

            if (fields.Count == 1 && registers.Count == 1 && value is not Expr.Call)
            {
                var (field, _) = fields[0];
                registers[((Pattern.Variable)tuple.Items[field]).Name] = 0;
                fields[0] = (field, 0);
            }

            int? result = value is Expr.Variable returned && registers.TryGetValue(returned.Name, out int source)
                ? source : null;
            CallArgumentConstructions? construction = value is Expr.Tuple or Expr.List
                ? ReadCallArgumentConstructions(
                    [value],
                    -1,
                    variables,
                    inputs: registers
                ) : null;
            EntryCall? call = null;
            if (value is Expr.Call invocation)
            {
                var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, Math.Max(fields.Count, payloadRegister.GetValueOrDefault()) + 1).ToArray();
                foreach (var (name, register) in registers)
                {
                    parameters[register] = name;
                }

                int savedSlotCount = variables.Count == 0 ? 0 : variables.Values.Max() + 1;
                string[] saved = Enumerable.Repeat(VariableScopeNames.Wildcard, savedSlotCount).ToArray();
                foreach (var (name, slot) in variables)
                {
                    saved[slot] = name;
                }

                call = ReadEntryCall(invocation, parameters, saved);
                if (call is null && TryResolveOrdinaryCallTarget(invocation, out string? module, allowControlTransfer: true)
                    && ReadCallArgumentConstructions(
                        invocation.Arguments,
                        -1,
                        variables,
                        inputs: registers
                    ) is { } arguments)
                {
                    construction = arguments;
                    call = new EntryCall(invocation, module, [], []);
                }

                if (call is not null && construction is null && fields.Count != 0 && !registers.ContainsValue(0)
                    && invocation.Arguments is [Expr.Variable first, ..]
                    && registers.TryGetValue(first.Name, out int argumentRegister) && fields[^1].Register == argumentRegister
                    && !invocation.Arguments.Skip(1).OfType<Expr.Variable>().Any(argument => argument.Name == first.Name))
                {
                    fields[^1] = (fields[^1].Field, 0);
                    call.Moves.Remove((0, argumentRegister));
                }
            }

            if (result is null && construction is null && call is null)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            var checkedPattern = tuple with { Items = tests };
            if (!tagKnown)
            {
                EmitCaseTupleTest(
                    tuple.Items.Count,
                    tag,
                    failure,
                    arityKnown,
                    savedSelector
                );
            }

            int selectorTag = savedSelector.HasValue ? YRegisterTag : XRegisterTag;
            Pattern(
                checkedPattern,
                savedSelector.GetValueOrDefault(),
                failure,
                checkedPattern,
                entryPatternScratch: true,
                valueTag: selectorTag
            );
            Guard(clause.Guard, failure, guardBinding?.Name);
            foreach (var (field, register) in fields)
            {
                owner.EmitTuplePart(
                    savedSelector.GetValueOrDefault(),
                    field,
                    register,
                    selectorTag
                );
            }

            if (construction is not null)
            {
                EmitCallArgumentConstructions(construction, -1);
            }

            if (call is not null)
            {
                if (construction is null)
                {
                    owner.EmitOrderedTailMoves(call.Moves, call.Constants, call.SavedInputs);
                }

                if (tailCall)
                {
                    EmitPreparedFrameTail(call, location, frameSize);
                }
                else
                {
                    owner.EmitCallLine(call.Call, location);
                    Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
                }
            }
            else if (result is > 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    result.Value,
                    XRegisterTag,
                    0
                );
            }

            return true;
        }

        private static bool HasCaseTupleTag(Clause clause) =>
            HasTupleTag((Pattern.Tuple)EntryShape(clause.Patterns[0]), out _);

        private static Atom TupleTag(Clause clause)
        {
            var tuple = (Pattern.Tuple)EntryShape(clause.Patterns[0]);
            return (Atom)((Pattern.Literal)tuple.Items[0]).Value;
        }

        private void EmitRegisterCaseTagSelection((Atom Tag, int Label)[] tags, int failure,
            int? payloadRegister = null, int? savedSelector = null)
        {
            int selector = EmitCaseTupleTag(savedSelector);
            if (savedSelector is null && payloadRegister is int register)
            {
                owner.EmitTuplePart(0, 1, register);
            }

            owner.Op(SelectValue, XRegisterTag, selector);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tags.Length * 2);
            foreach (var (tag, label) in tags)
            {
                owner.LiteralOperand(tag);
                owner.Operand(LabelTag, label);
            }
        }

        private int EmitCaseTupleTag(int? savedSelector)
        {
            int register = savedSelector.HasValue ? 0 : 1;
            owner.EmitTuplePart(
                savedSelector.GetValueOrDefault(),
                0,
                register,
                savedSelector.HasValue ? YRegisterTag : XRegisterTag
            );

            return register;
        }

        private void EmitCaseTupleTest(
            int arity,
            Atom tag,
            int failure,
            bool arityKnown,
            int? savedSelector = null
        )
        {
            if (!arityKnown)
            {
                owner.EmitRecordTest(
                    failure,
                    savedSelector.HasValue ? YRegisterTag : XRegisterTag,
                    savedSelector.GetValueOrDefault(),
                    tag,
                    arity
                );

                return;
            }

            int register = EmitCaseTupleTag(savedSelector);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.LiteralOperand(tag);
        }

        private Expr ReuseSelectorSlot(Expr body, int?[] selectorSlots)
        {
            if (body is Expr.Sequence { Items.Count: > 1 } sequence)
            {
                Expr[] continuation = sequence.Items.Skip(1).ToArray();
                if (TryReuseBindingSlot(sequence.Items[0], continuation, selectorSlots.OfType<int>()))
                {
                    return new Expr.Sequence(continuation);
                }
            }

            return body;
        }

        private bool TryReuseBindingSlot(Expr item, IEnumerable<Expr> continuation, IEnumerable<int> candidateSlots)
        {
            if (owner.receiveScratchRegisters is not null
                || item is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                    Value: var value
                }
                || variables.ContainsKey(result.Name) || deferredPairMembers.ContainsKey(result.Name)
                || value is Expr.Variable || !HasPreparedBindingOperands(value))
            {
                return false;
            }

            var bindings = variables.Keys.Select(name => (Pattern)new Pattern.Variable(name)).Append(result).ToArray();
            if (ReadUsedEntryParameters(new Clause(bindings, null, new Expr.Sequence(continuation.ToArray()))) is not { } used
                || !used.Contains(result.Name))
            {
                return false;
            }

            foreach (int slot in candidateSlots.Distinct())
            {
                string[] aliases = variables.Where(binding => binding.Value == slot).Select(binding => binding.Key).ToArray();
                if (aliases.Any(used.Contains) || deferredPairMembers.Values.Any(member => member.Slot == slot))
                {
                    continue;
                }

                if (AttributeLiteral.TryNormalizeConstant(value, out Term literal))
                {
                    owner.EmitConstant(literal, slot, YRegisterTag);
                }
                else
                {
                    Expression(value);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slot
                    );
                }
                foreach (string alias in aliases)
                {
                    variables.Remove(alias);
                }
                variables.Add(result.Name, slot);

                return true;
            }

            return false;
        }

        private static bool HasPreparedBindingOperands(Expr value) => value switch
        {
            Expr.Call call => HasSimpleDecodeArguments(call),
            Expr.Unary unary => IsPreparedOperand(unary.Operand),
            Expr.Binary binary => IsPreparedOperand(binary.Left) && IsPreparedOperand(binary.Right),
            Expr.Tuple tuple => tuple.Items.All(IsPreparedOperand),
            Expr.List list => list.Items.All(IsPreparedOperand) && (list.Tail is null || IsPreparedOperand(list.Tail)),
            Expr.RecordAccess access => IsPreparedOperand(access.Value),
            _ => AttributeLiteral.TryNormalizeConstant(value, out _)
        };

        private static bool IsPreparedOperand(Expr operand) =>
            operand is Expr.Variable || AttributeLiteral.TryNormalizeConstant(operand, out _);

        private bool TryEmitForwardedCaseInput(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Sequence
                    {
                        Items: [
                          Expr.Match
                        {
                            Pattern: Pattern.Variable result, Value: Expr.Call
                            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] } call
                        },
                          Expr.Case { Value: Expr.Case { Value: Expr.Variable input } unwrap } decision]
                    }
                }]
                || !DistinctBindings(first, second, result) || left.Name != first.Name || right.Name != second.Name
                || result.Name != input.Name || !entries.TryGetValue((call.Function, 2), out int entry)
                || LocalFunctionEntries.Walk(decision).Any(expression => expression switch
                {
                    Expr.Variable variable => variable.Name == first.Name || variable.Name == second.Name,
                    Expr.Match match => !CanBindExactPair(match.Pattern, first.Name, second.Name),
                    Expr.Case nested => nested.Clauses.Any(clause =>
                        clause.Patterns.Any(pattern => !CanBindExactPair(pattern, first.Name, second.Name))),
                    Expr.Block or Expr.Sequence or Expr.Call or Expr.Tuple or Expr.If or Expr.Binary or Expr.List
                        or Expr.Literal or Expr.Bits => false,
                    _ => true
                }))
            {
                return false;
            }

            EmitGeneralFrame(function.Arity);
            owner.EmitCallLine(call, location);
            owner.EmitFieldCall(2, entry);
            if (CanUnwrapSingleton(unwrap)
                && decision.Clauses.All(clause => clause.Patterns.All(pattern =>
                    CanBindExactPair(pattern, result.Name, result.Name)))
                && !LocalFunctionEntries.ClauseExpressions(decision.Clauses)
                    .SelectMany(LocalFunctionEntries.Walk).Any(expression => expression switch
                    {
                        Expr.Variable variable => variable.Name == result.Name,
                        Expr.Match match => !CanBindExactPair(match.Pattern, result.Name, result.Name),
                        Expr.Case nested => nested.Clauses.Any(clause => clause.Patterns.Any(pattern =>
                            !CanBindExactPair(pattern, result.Name, result.Name))),
                        _ => false
                    }))
            {
                EmitSingletonUnwrap();
                EmitCase(
                    decision,
                    null,
                    inputPrepared: true,
                    returnResult: true
                );
            }
            else
            {
                variables.Add(result.Name, Save());
                Expression(decision);
                owner.EmitFrameReturn(frameSize);
            }

            return true;
        }

        private bool TryEmitSingletonUnwrap(Expr.Case decision)
        {
            if (!CanUnwrapSingleton(decision))
            {
                return false;
            }

            Expression(decision.Value);
            EmitSingletonUnwrap();

            return true;
        }

        private bool CanUnwrapSingleton(Expr.Case decision)
        {
            if (decision.Value is not Expr.Variable input
                || decision.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: null }],
                    Body: Expr.Variable selected
                },
                { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable unchanged }]
                || head.Name == VariableScopeNames.Wildcard || head.Name == input.Name || selected.Name != head.Name
                || variables.ContainsKey(head.Name)
                || (fallback.Name != VariableScopeNames.Wildcard
                    && (fallback.Name == input.Name || variables.ContainsKey(fallback.Name)))
                || unchanged.Name != (fallback.Name == VariableScopeNames.Wildcard ? input.Name : fallback.Name))
            {
                return false;
            }

            return true;
        }

        private void EmitSingletonUnwrap()
        {
            int done = Label();
            owner.Op(IsNonemptyList, LabelTag, done);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.EmitNilTest(XRegisterTag, 2, done);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            Mark(done);
        }

        private void EmitCaseResult(Expr body, int? guardFailure, (int First, int Second)? destinations)
        {
            if (destinations is not { } slots)
            {
                Expression(body, guardFailure);

                return;
            }

            var tuple = (Expr.Tuple)body;
            StoreCaseMember(tuple.Items[0], slots.First);
            StoreCaseMember(tuple.Items[1], slots.Second);
        }

        private void StoreCaseMember(Expr expression, int destination)
        {
            if (expression is Expr.Variable variable && variables.TryGetValue(variable.Name, out int source))
            {
                owner.MoveRegister(
                    YRegisterTag,
                    source,
                    YRegisterTag,
                    destination
                );
            }
            else if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
            {
                owner.EmitConstant(literal, destination, YRegisterTag);
            }
            else
            {
                Expression(expression);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    destination
                );
            }
        }

        private bool TryBindDecodedResult(Expr.Match match) =>
            match.Pattern is Pattern.Variable destination
            && destination.Name != VariableScopeNames.Wildcard && !variables.ContainsKey(destination.Name)
            && (TryBindDecodedEnum(destination.Name, match.Value) || TryBindDecodedRange(destination.Name, match.Value));

        private static Expr FoldLiteralMatch(Expr expression) =>
            expression is Expr.Match
            {
                Pattern: Pattern.Literal { Value: Atom or Integer } pattern,
                Value: Expr.Literal literal
            } && pattern.Value.Equals(literal.Value)
                ? literal
                : expression;

        private void Match(Expr.Match match, int? guardFailure, bool returnValue)
        {
            if (FoldLiteralMatch(match) is Expr.Literal literal)
            {
                if (returnValue)
                {
                    owner.EmitConstant(literal.Value, 0);
                }

                return;
            }

            if (guardFailure is null && ReadCheckedListBinding(match) is { } binding
                && variables.TryGetValue(binding.Input.Name, out int input)
                && !variables.ContainsKey(binding.Alias.Name) && !deferredPairMembers.ContainsKey(binding.Alias.Name))
            {
                Match(binding.Check, null, returnValue: false);
                variables.Add(binding.Alias.Name, input);
                if (returnValue)
                {
                    Load(input);
                }

                return;
            }

            if (guardFailure is null && !returnValue && TryBindDecodedResult(match))
            {
                return;
            }

            if (guardFailure is null && !returnValue
                && match.Pattern is Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }
                && DistinctBindings(first, second)
                && !variables.ContainsKey(first.Name) && !variables.ContainsKey(second.Name)
                && CanBindExactPair(match.Value, first.Name, second.Name)
                && ReturnsTuple(
                    match.Value,
                    2,
                    [],
                    exact: true
                ))
            {
                if (match.Value is Expr.Case { Clauses.Count: 2 } decision
                    && decision.Clauses.All(clause => !AttributeLiteral.TryNormalizeConstant(clause.Body, out _)))
                {
                    int firstSlot = Slot();
                    int secondSlot = Slot();
                    EmitCase(decision, null, (firstSlot, secondSlot));
                    variables.Add(first.Name, firstSlot);
                    variables.Add(second.Name, secondSlot);
                }
                else
                {
                    Expression(match.Value);
                    BindExactPair(first, second);
                }

                return;
            }

            if (guardFailure is null && TryEmitRegisterTaggedMatch(match))
            {
                return;
            }

            MatchValue(match, ValueSlot(match.Value, guardFailure), returnValue);
        }

        private void MatchValue(Expr.Match match, int matched, bool returnValue)
        {
            if (match.Pattern is Pattern.Variable variable
                && (variable.Name == VariableScopeNames.Wildcard || !variables.ContainsKey(variable.Name)))
            {
                Pattern(variable, matched, 0);
                if (returnValue)
                {
                    Load(matched);
                }

                return;
            }

            int failure = Label();
            int done = Label();
            Pattern(match.Pattern, matched, failure);
            if (returnValue)
            {
                Load(matched);
            }
            JumpTo(done);
            Mark(failure);
            Load(matched);
            owner.EmitArithmeticLine(match.SourceLine, ref location);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(done);
        }

        private sealed record DecodedEnum(
            Expr.Call Call,
            Atom Tag,
            string Fallback,
            SortedDictionary<int, Atom> Alternatives
        );

        private DecodedEnum? ReadDecodedEnum(Expr value)
        {
            if (value is not Expr.Case { Value: Expr.Call { Module: null, DynamicRemote: false } call } decision
                || decision.Clauses.Count < 3
                || !HasSimpleDecodeArguments(call)
                || decision.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable fallback],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable payload] }
                }
                || fallback.Name == VariableScopeNames.Wildcard || variables.ContainsKey(fallback.Name)
                || fallback.Name != payload.Name)
            {
                return null;
            }

            var alternatives = new SortedDictionary<int, Atom>();
            foreach (Clause clause in decision.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Integer key }],
                        Body: Expr.Literal { Value: Atom atom }
                    }
                    || key.Value < int.MinValue || key.Value > int.MaxValue || !alternatives.TryAdd((int)key.Value, atom))
                {
                    return null;
                }
            }

            return new(
                call,
                tag,
                fallback.Name,
                alternatives
            );
        }

        private bool TryBindDecodedEnum(
            string destination,
            Expr value,
            Action<Expr.Call>? emitCall = null,
            int? resultSlot = null
        )
        {
            if (ReadDecodedEnum(value) is not { } decoded)
            {
                return false;
            }

            if (emitCall is null)
            {
                Expression(decoded.Call);
            }
            else
            {
                emitCall(decoded.Call);
            }
            int result = resultSlot ?? Slot();
            variables.Add(destination, result);
            EmitDecodedEnum(decoded, result);

            return true;
        }

        private void EmitDecodedEnum(DecodedEnum decoded, int result)
        {
            var labels = decoded.Alternatives.Reverse().Select(branch => (branch.Key, branch.Value, Label: Label())).ToArray();
            int otherwise = Label();
            int done = Label();
            owner.instructions.WriteByte(SelectValue);
            EmitRangeResultOperand((InferPairFacts(decoded.Call) & PairFacts.Integer) != 0);
            owner.Operand(LabelTag, otherwise);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, labels.Length * 2);
            foreach (var branch in labels.Reverse())
            {
                owner.Operand(BeamWire.IntegerTag, branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }
            foreach (var branch in labels)
            {
                Mark(branch.Label);
                owner.EmitConstant(branch.Value, result, YRegisterTag);
                JumpTo(done);
            }
            Mark(otherwise);
            EmitTaggedRegisterResult(decoded.Tag);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                result
            );
            Mark(done);
        }

        private bool TryEmitTerminalDecodedRange(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Block
                    {
                        Body: Expr.Sequence
                        {
                            Items:
                        [Expr.Match
                        {
                            Pattern: Pattern.Variable result, Value: Expr.Call
                            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable first, Expr.Variable second] } call
                        },
                         Expr.If test]
                        }
                    }
                }
                ]
                || !DistinctBindings(input, tags, result)
                || first.Name != input.Name || second.Name != tags.Name
                || !entries.ContainsKey((call.Function, 2))
                || ReadTaggedBranchRange(test, result.Name) is not { } range)
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);
            int failure = Label();
            EmitRangeTests(call, range, failure);
            owner.EmitFrameReturn(0);

            Mark(failure);
            var previous = CallLocation(call, location);
            var exitLocation = CallLocation(range.Exit, previous);
            EmitRangeExit(range, exitLocation == previous ? null : exitLocation, 0);

            return true;
        }

        private bool TryBindDecodedRange(
            string destination,
            Expr value,
            Action<Expr.Call>? emitCall = null,
            int? resultSlot = null
        )
        {
            if (value is not Expr.Block
                {
                    Body: Expr.Sequence
                    { Items: [Expr.Match { Pattern: Pattern.Variable number, Value: Expr.Call call }, Expr.If test] }
                }
                || number.Name == VariableScopeNames.Wildcard || variables.ContainsKey(number.Name)
                || !HasSimpleDecodeArguments(call)
                || ReadTaggedBranchRange(test, number.Name) is not { } range)
            {
                return false;
            }

            if (emitCall is null)
            {
                Expression(call);
            }
            else
            {
                emitCall(call);
            }
            int result = SaveDecodedResult(resultSlot);
            variables.Add(number.Name, result);
            if (destination != number.Name)
            {
                variables.Add(destination, result);
            }

            int failure = emitCall is null ? Label() : decoderRangeLabel ?? Label();
            if (emitCall is not null)
            {
                decoderRangeLabel--;
            }
            EmitRangeTests(call, range, failure);
            if (emitCall is not null)
            {
                decoderRangeFailures.Add((failure, range, CallLocation(range.Exit, CallLocation(call, location))));

                return true;
            }

            int done = Label();
            JumpTo(done);
            Mark(failure);
            owner.EmitConstant(range.Reason, 0);
            owner.EmitCallLine(range.Exit, CallLocation(call, location));
            Call(range.Exit.Module, range.Exit.Function, 1);
            Mark(done);

            return true;
        }

        private static bool HasSimpleDecodeArguments(Expr.Call call) =>
            call.Arguments.All(argument => argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _));

        private static bool CanBindExactPair(Expr value, string first, string second) => value switch
        {
            Expr.Call call => call.Arguments.All(argument => argument is Expr.Variable
                || AttributeLiteral.TryNormalizeConstant(argument, out _)),
            Expr.Case { Value: Expr.Variable } decision => decision.Clauses.All(branch =>
                branch.Guard is null && branch.Patterns.All(pattern => CanBindExactPair(pattern, first, second))
                && branch.Body is Expr.Tuple
                && LocalFunctionEntries.Walk(branch.Body).All(expression => expression is
                    Expr.Tuple or Expr.Block or Expr.Call or Expr.Variable or Expr.Literal or Expr.List or Expr.Bits)),
            _ => false
        };

        private static bool CanBindExactPair(Pattern pattern, string first, string second) => pattern switch
        {
            Pattern.Variable variable => variable.Name != first && variable.Name != second,
            Pattern.Tuple tuple => tuple.Items.All(item => CanBindExactPair(item, first, second)),
            Pattern.List list => list.Items.All(item => CanBindExactPair(item, first, second))
                && (list.Tail is null || CanBindExactPair(list.Tail, first, second)),
            Pattern.Literal { } => true,
            _ => false
        };

        private void BindExactPair(Pattern.Variable first, Pattern.Variable second)
        {
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 0);
            int firstSlot = Slot();
            int secondSlot = Slot();
            variables.Add(first.Name, firstSlot);
            variables.Add(second.Name, secondSlot);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                firstSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                secondSlot
            );
        }

        private bool IsTailBooleanCase(Expr.Case expression) =>
            IsBooleanCase(expression) && expression.Clauses.All(clause =>
                ReadEntryCaseExternalTail(clause.Body) is not null
                || clause.Body is Expr.Call { Module: null, DynamicRemote: false } call
                    && entries.ContainsKey((call.Function, call.Arguments.Count))
                    && call.Arguments.All(argument => argument is Expr.Variable { }
                        || AttributeLiteral.TryNormalizeConstant(argument, out _)));

        private BeamSourceLocation LoadCaseTailArguments(IReadOnlyList<Expr> arguments)
        {
            if (!arguments.All(argument => argument is Expr.Variable variable
                && variables.ContainsKey(variable.Name) && !deferredPairMembers.ContainsKey(variable.Name)))
            {
                return LoadCallArguments(arguments, null, orderSimpleOperands: true);
            }

            var moves = arguments.Cast<Expr.Variable>()
                .Select((argument, destination) => (Source: variables[argument.Name], Destination: destination));
            foreach (var (source, destination) in moves.OrderBy(move => move.Source))
            {
                Load(source, destination);
            }

            return location;
        }

        private void EmitCaseExternalTail(Expr.Call call, string module)
        {
            var argumentLocation = LoadCaseTailArguments(call.Arguments);
            owner.EmitCallLine(call, argumentLocation);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, call.Arguments.Count));
            owner.Operand(BeamWire.UnsignedTag, frameSize);
        }

        private static bool IsBooleanCase(Expr.Case expression) => expression.Clauses is [
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }] },
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }] }
            ] or [
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }] },
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }] }
            ];

        private bool TryEmitCaseCallClearingFrame(Expr body)
        {
            if (frameSize == 0 || body is not Expr.Call
                {
                    Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable input, var constant]
                    } inner]
                } outer
                || !variables.ContainsKey(input.Name) || deferredPairMembers.ContainsKey(input.Name)
                || !AttributeLiteral.TryNormalizeConstant(constant, out _))
            {
                return false;
            }

            var argumentLocation = LoadCallArguments(inner.Arguments, null, orderSimpleOperands: true);
            // The selected branch and its tail continuation no longer read the frame.
            EmitFrameInitialization(0, frameSize);
            owner.EmitCallLine(inner, argumentLocation);
            Call(inner.Module, inner.Function, 2);
            owner.EmitCallLine(outer, CallLocation(inner, argumentLocation));
            Call(outer.Module, outer.Function, 1);

            return true;
        }

        private void EmitBooleanCaseFailure(int label, BeamSourceLocation? failureLocation)
        {
            Mark(label);
            if (failureLocation is { } source)
            {
                owner.EmitLine(source);
            }
            owner.Op(CaseEnd, XRegisterTag, 0);
        }

        private Expr.Case? ReadTerminalAtomCase(Expr.Case decision, IEnumerable<string> bindings)
        {
            var names = bindings.ToHashSet(StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            bool hasTail = false;
            var branches = new List<Clause>();
            foreach (Clause branch in decision.Clauses)
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] }
                    || !keys.Add(key.Name))
                {
                    return null;
                }

                Expr body = branch.Body;
                if (branch.Body is Expr.Case
                    {
                        Value: Expr.Call { Module: null, DynamicRemote: false } call,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [
                                Pattern.Literal { Value: Atom tag }, Pattern.Variable payload
                            ]
                            }], Body: Expr.Tuple
                            {
                                Items: [
                                Expr.Literal { Value: Atom returnedTag }, Expr.Variable returnedPayload
                            ]
                            }
                        },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returnedFallback }
                        ]
                    }
                    && tag.Name == returnedTag.Name && payload.Name == returnedPayload.Name
                    && fallback.Name == returnedFallback.Name
                    && payload.Name != VariableScopeNames.Wildcard && fallback.Name != VariableScopeNames.Wildcard
                    && !names.Contains(payload.Name) && !names.Contains(fallback.Name))
                {
                    body = call;
                }

                var available = names;
                Expr tail = body;
                if (body is Expr.Sequence { Items.Count: > 1 } sequence)
                {
                    available = new HashSet<string>(names, StringComparer.Ordinal);
                    foreach (Expr item in sequence.Items.SkipLast(1))
                    {
                        if (item is not Expr.Match { Pattern: Pattern.Variable variable, Value: var value }
                            || variable.Name == VariableScopeNames.Wildcard
                            || !CanPrepareTerminalAtomArgument(value, available)
                            || !available.Add(variable.Name))
                        {
                            return null;
                        }
                    }

                    tail = sequence.Items[^1];
                }

                if (tail is Expr.Call { Module: null, DynamicRemote: false } local
                    && entries.ContainsKey((local.Function, local.Arguments.Count))
                    && local.Arguments.All(argument => CanPrepareTerminalAtomArgument(argument, available)))
                {
                    hasTail = true;
                }
                else if (body is not Expr.Literal && (body is not Expr.Variable returned || !names.Contains(returned.Name)))
                {
                    return null;
                }
                branches.Add(branch with { Body = body });
            }

            return hasTail && branches.Count >= 2 ? decision with { Clauses = branches } : null;
        }

        private static bool CanPrepareTerminalAtomArgument(Expr argument, HashSet<string> names) =>
            AttributeLiteral.TryNormalizeConstant(argument, out _) || argument switch
            {
                Expr.Variable variable => variable.Name != VariableScopeNames.Wildcard && names.Contains(variable.Name),
                Expr.Tuple tuple => tuple.Items.All(item => CanPrepareTerminalAtomArgument(item, names)),
                Expr.List list => list.Items.All(item => CanPrepareTerminalAtomArgument(item, names))
                    && (list.Tail is null || CanPrepareTerminalAtomArgument(list.Tail, names)),
                Expr.Call { DynamicRemote: false } call =>
                    call.Arguments.All(item => CanPrepareTerminalAtomArgument(item, names)),
                Expr.Unary unary => CanPrepareTerminalAtomArgument(unary.Operand, names),
                Expr.Binary binary => CanPrepareTerminalAtomArgument(binary.Left, names)
                    && CanPrepareTerminalAtomArgument(binary.Right, names),
                _ => false
            };

        private bool TryCaseSelection(
            Expr.Case expression,
            int? input,
            bool returnResult,
            bool reuseBranchSlots,
            Expr[]? branchContinuation,
            List<(int Label, BeamSourceLocation? Location)>? deferredBooleanFailures,
            (int True, int False)? reservedBooleanLabels,
            bool allowUnmatchedAtoms,
            bool terminalAtomTails = false
        )
        {
            bool booleanPair = IsBooleanCase(expression);
            var fallback = expression.Clauses[^1].Patterns is [Pattern.Variable final] ? final : null;
            bool unmatchedAtoms = allowUnmatchedAtoms && fallback is null && expression.Clauses.Count >= 2
                && expression.Clauses.All(clause => clause is { Guard: null, Patterns: [Pattern.Literal { Value: Atom }] });
            if (!booleanPair && !unmatchedAtoms && (expression.Clauses.Count < 3 || expression.Clauses[^1].Guard is not null
                || fallback is null || fallback.Name != VariableScopeNames.Wildcard && variables.ContainsKey(fallback.Name)))
            {
                return false;
            }
            bool tuple = expression.Clauses[0].Patterns is [Pattern.Tuple _];
            var keys = new HashSet<Term>();
            var branches = new List<(Term Key, Pattern.Variable? Field, Expr Body)>();
            foreach (Clause clause in expression.Clauses.SkipLast(booleanPair || unmatchedAtoms ? 0 : 1))
            {
                if (clause is not { Guard: null, Patterns: [var pattern] })
                {
                    return false;
                }
                var key = pattern as Pattern.Literal;
                Pattern.Variable? field = null;
                if (tuple && pattern is Pattern.Tuple { Items: [Pattern.Literal tag, Pattern.Variable variable] })
                {
                    key = tag;
                    field = variable;
                }
                if (tuple && field is null || key?.Value is not (Atom or Integer)
                    || key.Value is Integer integer && (integer.Value < int.MinValue || integer.Value > int.MaxValue)
                    || branches.Count != 0 && branches[0].Key.GetType() != key.Value.GetType()
                    || !keys.Add(key.Value)
                    || field is not null && field.Name != VariableScopeNames.Wildcard && variables.ContainsKey(field.Name))
                {
                    return false;
                }
                branches.Add((key.Value, field, clause.Body));
            }

            branches.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            var direct = branches.Select(branch => ReadDirectTaggedBranch(branch.Field, branch.Body, returnResult)).ToArray();
            bool sharedPayload = direct.Any(branch => branch.HasValue);
            bool keepTupleInX0 = tuple && input is null && fallback?.Name == VariableScopeNames.Wildcard;
            if (input is int saved)
            {
                Load(saved);
            }
            else if (!sharedPayload && fallback is { Name: not VariableScopeNames.Wildcard })
            {
                input = Save();
            }
            bool tailBoolean = booleanPair && returnResult && IsTailBooleanCase(expression);
            bool orderedLabels = terminalAtomTails || tailBoolean || returnResult && direct.All(branch => branch.HasValue);
            bool deferredContinuation = booleanPair && !returnResult && deferredBooleanFailures is not null;
            int otherwise = orderedLabels || deferredContinuation ? 0 : Label();
            int done = returnResult || deferredContinuation ? 0 : Label();
            var labels = new int[branches.Count];
            int[]? sharedTails = terminalAtomTails ? FindSharedAtomTails(branches.Select(branch => branch.Body).ToArray()) : null;
            for (int index = labels.Length - 1; index >= 0; index--)
            {
                if (sharedTails is not null && sharedTails[index] != index)
                {
                    continue;
                }

                labels[index] = reservedBooleanLabels is { } reserved
                    ? index == 0 ? reserved.False : reserved.True
                    : Label();
                if (orderedLabels && direct[index]?.Range is not null)
                {
                    Label();
                }
            }
            if (sharedTails is not null)
            {
                for (int index = 0; index < labels.Length; index++)
                {
                    labels[index] = labels[sharedTails[index]];
                }
            }

            if (deferredContinuation)
            {
                done = Label();
            }
            if (orderedLabels || deferredContinuation)
            {
                otherwise = Label();
            }
            if (tuple)
            {
                owner.EmitTupleCheck(0, 2, otherwise);
                owner.EmitTupleElement(0, sharedPayload || keepTupleInX0 ? 1 : 0);
                if (sharedPayload)
                {
                    owner.EmitTupleElement(1, 2);
                }
            }
            owner.Op(SelectValue, XRegisterTag, sharedPayload || keepTupleInX0 ? 1 : 0);
            owner.Operand(LabelTag, otherwise);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Count * 2);
            for (int index = 0; index < branches.Count; index++)
            {
                owner.LiteralOperand(branches[index].Key);
                owner.Operand(LabelTag, labels[index]);
            }
            int selectorLineCount = owner.lineInstructions;
            var original = variables;
            int fieldSlot = branches.Where((_, index) => direct[index] is null)
                .Any(branch => branch.Field is { Name: not VariableScopeNames.Wildcard }) ? Slot() : -1;
            int branchBase = nextSlot;
            int branchLimit = branchBase;
            for (int index = branches.Count - 1; index >= 0; index--)
            {
                if (sharedTails is not null && sharedTails[index] != index)
                {
                    continue;
                }

                Mark(labels[index]);
                variables = new(original, StringComparer.Ordinal);
                nextSlot = branchBase;
                var branch = branches[index];
                if (terminalAtomTails && branch.Body is Expr.Call or Expr.Sequence)
                {
                    TailLocal(branch.Body, reuseCallSlots: true, caseTail: true);
                    branchLimit = Math.Max(branchLimit, nextSlot);
                    continue;
                }
                if (terminalAtomTails && branch.Body is Expr.Literal literal && literal.Value.Equals(branch.Key))
                {
                    owner.EmitFrameReturn(frameSize);
                    continue;
                }
                if (tailBoolean)
                {
                    if (ReadEntryCaseExternalTail(branch.Body) is { } tail)
                    {
                        EmitCaseExternalTail(tail.Call, tail.Module);
                    }
                    else
                    {
                        TailLocal(branch.Body, caseTail: true);
                    }
                    branchLimit = Math.Max(branchLimit, nextSlot);
                    continue;
                }
                if (direct[index] is { } call)
                {
                    EmitDirectTaggedBranch(
                        call.Tag,
                        call.Call,
                        call.Argument,
                        call.Conversion,
                        call.Range,
                        orderedLabels ? labels[index] + 1 : null
                    );
                }
                else
                {
                    if (branch.Field is { Name: not VariableScopeNames.Wildcard } bound)
                    {
                        if (sharedPayload)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                2,
                                YRegisterTag,
                                fieldSlot
                            );
                        }
                        else
                        {
                            owner.Op(GetTupleElement, keepTupleInX0 ? XRegisterTag : YRegisterTag, keepTupleInX0 ? 0 : input!.Value);
                            owner.Operand(BeamWire.UnsignedTag, 1);
                            owner.Operand(YRegisterTag, fieldSlot);
                        }
                        variables.Add(bound.Name, fieldSlot);
                    }
                    if ((booleanPair || unmatchedAtoms) && branchContinuation is not null)
                    {
                        EmitExportingBranch(branch.Body, [], branchContinuation: branchContinuation);
                    }
                    else if ((booleanPair || unmatchedAtoms) && reuseBranchSlots)
                    {
                        if (!booleanPair || deferredBooleanFailures is null || returnResult
                            || !TryEmitCaseCallClearingFrame(branch.Body))
                        {
                            EmitReturnExpression(branch.Body, reuseCallSlots: true);
                        }
                    }
                    else
                    {
                        Expression(branch.Body);
                    }
                }
                branchLimit = Math.Max(branchLimit, nextSlot);
                if (direct[index]?.Range is not null)
                {
                    continue;
                }
                if (returnResult)
                {
                    owner.EmitFrameReturn(frameSize);
                }
                else if (!booleanPair || deferredBooleanFailures is null || index != 0)
                {
                    JumpTo(done);
                }
            }
            variables = new(original, StringComparer.Ordinal);
            nextSlot = branchBase;
            if (booleanPair)
            {
                BeamSourceLocation? failureLocation = (tailBoolean || deferredBooleanFailures is not null && !returnResult)
                    && expression.Value is Expr.Call { SourceLine: not 0 } selector
                    ? CallLocation(selector, location) : null;
                if (deferredBooleanFailures is null)
                {
                    EmitBooleanCaseFailure(otherwise, failureLocation);
                }
                else
                {
                    deferredBooleanFailures.Add((otherwise, failureLocation));
                }
            }
            else if (unmatchedAtoms)
            {
                BeamSourceLocation? failureLocation = owner.lineInstructions != selectorLineCount
                    && expression.Value is Expr.Call { SourceLine: not 0 } selector
                    ? CallLocation(selector, location) : null;
                EmitBooleanCaseFailure(otherwise, failureLocation);
            }
            else
            {
                Mark(otherwise);
                Expr fallbackBody = expression.Clauses[^1].Body;
                if (input is not null || !TryEmitTaggedFallback(fallback!, fallbackBody))
                {
                    if (fallback is { Name: not VariableScopeNames.Wildcard } named)
                    {
                        // Tagged dispatch keeps X0 intact on its failure edge.
                        variables.Add(named.Name, input ?? Save());
                    }
                    Expression(fallbackBody);
                }
                if (returnResult)
                {
                    owner.EmitFrameReturn(frameSize);
                }
            }
            nextSlot = Math.Max(branchLimit, nextSlot);
            if (!returnResult)
            {
                Mark(done);
            }
            variables = original;

            return true;
        }

        private static int[] FindSharedAtomTails(Expr[] bodies)
        {
            var representatives = new int[bodies.Length];
            for (int index = 0; index < bodies.Length; index++)
            {
                representatives[index] = index;
                if (bodies[index] is not Expr.Call call)
                {
                    continue;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (bodies[previous] is Expr.Call other && call.Function == other.Function
                        && call.Arguments.Count == other.Arguments.Count
                        && call.Arguments.Zip(other.Arguments).All(pair => SameTailOperand(pair.First, pair.Second)))
                    {
                        representatives[index] = previous;
                        break;
                    }
                }
            }

            return representatives;

            static bool SameTailOperand(Expr left, Expr right) => left is Expr.Variable variable
                ? right is Expr.Variable other && variable.Name == other.Name
                : AttributeLiteral.TryNormalizeConstant(left, out Term value)
                    && AttributeLiteral.TryNormalizeConstant(right, out Term otherValue) && value.Equals(otherValue);
        }

        private bool TryEmitTaggedFallback(Pattern.Variable fallback, Expr body)
        {
            if (fallback.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }
            if (body is Expr.Variable returned && returned.Name == fallback.Name)
            {
                return true;
            }
            if (body is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom tag }, Expr.Call
                    { Module: null, DynamicRemote: false, Arguments: [Expr.Variable payload] } call]
                }
                || payload.Name != fallback.Name || !entries.ContainsKey((call.Function, 1)))
            {
                return false;
            }

            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 1);
            EmitTaggedCallResult(tag, call);

            return true;
        }

        private sealed record TaggedBranchRange(
            Integer Lower,
            Integer Upper,
            Expr.Call Exit,
            Term Reason
        );

        private (Atom Tag, Expr.Call Call, Term Argument, Expr.Call? Conversion, TaggedBranchRange? Range)?
            ReadDirectTaggedBranch(Pattern.Variable? field, Expr body, bool returnResult)
        {
            if (field is null || field.Name == VariableScopeNames.Wildcard
                || body is not Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, var value] })
            {
                return null;
            }

            Expr.Call? conversion = null;
            TaggedBranchRange? range = null;
            if (returnResult && value is Expr.Block
                {
                    Body: Expr.Sequence
                    { Items: [Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call read }, Expr.If test] }
                }
                && result.Name != field.Name && !variables.ContainsKey(result.Name)
                && ReadTaggedBranchRange(test, result.Name) is { } checkedRange)
            {
                range = checkedRange;
                value = read;
            }
            else if (value is Expr.Block
            {
                Body: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: "binary_to_list", DynamicRemote: false,
                    Arguments: [Expr.Call inner]
                } convert
            }
                && (convert.Module is not null || !entries.ContainsKey(("binary_to_list", 1))))
            {
                conversion = convert;
                value = inner;
            }
            if (value is not Expr.Call
                { Module: null, DynamicRemote: false, Arguments: [Expr.Variable payload, var argument] } call
                || payload.Name != field.Name || !entries.ContainsKey((call.Function, 2))
                || !AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
            {
                return null;
            }

            return (tag, call, constant, conversion, range);
        }

        private TaggedBranchRange? ReadTaggedBranchRange(Expr.If test, string result)
        {
            if (test.Clauses is not
                [
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary
                        {
                            Operator: ErlangOperators.LessOrEqual,
                            Left: Expr.Literal { Value: Integer lower }, Right: Expr.Variable first
                        },
                        Right: Expr.Binary
                        {
                            Operator: ErlangOperators.LessOrEqual,
                            Left: Expr.Variable second, Right: Expr.Literal { Value: Integer upper }
                        }
                    },
                    Body: Expr.Variable accepted
                },
                {
                    Guard: Expr.Literal { Value: Atom { Name: "true" } }, Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "exit",
                        Arguments: [var reason]
                    } exit
                }
                ]
                || result != first.Name || result != second.Name || result != accepted.Name
                || exit.Module is null && entries.ContainsKey(("exit", 1))
                || !AttributeLiteral.TryNormalizeConstant(reason, out Term error))
            {
                return null;
            }

            return new(
                lower,
                upper,
                exit,
                error
            );
        }

        private void EmitDirectTaggedBranch(
            Atom tag,
            Expr.Call call,
            Term argument,
            Expr.Call? conversion,
            TaggedBranchRange? range,
            int? rangeFailure
        )
        {
            owner.EmitConstant(argument, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);
            if (conversion is not null)
            {
                owner.EmitCallLine(conversion, CallLocation(call, location));
                Call(conversion.Module, conversion.Function, 1);
            }
            if (range is null)
            {
                EmitTaggedCallResult(tag, conversion ?? call);

                return;
            }

            int failure = rangeFailure ?? Label();
            EmitRangeTests(call, range, failure);
            EmitTaggedCallResult(tag, call);
            owner.EmitFrameReturn(frameSize);

            Mark(failure);
            var previous = CallLocation(call, location);
            var exitLocation = CallLocation(range.Exit, previous);
            EmitRangeExit(range, exitLocation == previous ? null : exitLocation, frameSize);
        }

        private void EmitRangeExit(TaggedBranchRange range, BeamSourceLocation? exitLocation, int frameSlots)
        {
            owner.EmitConstant(range.Reason, 0);
            if (exitLocation is { } line)
            {
                owner.EmitLine(line);
            }
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, range.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frameSlots);
        }

        private void EmitRangeTests(Expr.Call call, TaggedBranchRange range, int failure)
        {
            bool integerResult = (InferPairFacts(call) & PairFacts.Integer) != 0;
            owner.Op(IsGreaterOrEqual, LabelTag, failure);
            EmitRangeResultOperand(integerResult);
            owner.LiteralOperand(range.Lower);
            owner.Op(IsGreaterOrEqual, LabelTag, failure);
            owner.LiteralOperand(range.Upper);
            long? lower = range.Lower.Value >= long.MinValue && range.Lower.Value <= long.MaxValue
                ? (long)range.Lower.Value : null;
            EmitRangeResultOperand(integerResult, lower);
        }

        private void EmitRangeResultOperand(bool integerResult, long? lower = null)
        {
            if (integerResult)
            {
                owner.EmitTypedRegister(0, IntegerTypeBits, lower);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
        }

        private void EmitTaggedCallResult(Atom tag, Expr.Call call)
        {
            if (ReadConstantCallResult(call) is { } value)
            {
                owner.EmitConstant(Term.Tuple(tag, value), 0);

                return;
            }

            EmitTaggedRegisterResult(tag);
        }

        private void EmitTaggedRegisterResult(Atom tag, int additionalHeapWords = 0)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + additionalHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 0);
        }

        private (string Name, int Slot)[] ReserveBranchExports(IReadOnlyList<string>? exports) =>
            exports is null ? [] : exports.Select(name => (name, Slot())).ToArray();

        private void EmitExportingBranch(
            Expr body,
            (string Name, int Slot)[] joined,
            int? guardFailure = null,
            Expr[]? branchContinuation = null
        )
        {
            if (joined.Length == 0 && branchContinuation is null)
            {
                Expression(body, guardFailure);

                return;
            }

            IEnumerable<Expr> continuation = joined.Select(binding => new Expr.Variable(binding.Name));
            if (branchContinuation is not null)
            {
                continuation = continuation.Concat(branchContinuation);
            }

            EmitWithContinuation(
                body,
                continuation,
                guardFailure,
                returnValue: true,
                reuseCallSlots: branchContinuation is not null
            );
        }

        private void StoreBranchExports((string Name, int Slot)[] joined)
        {
            foreach (var (name, slot) in joined)
            {
                if (!variables.TryGetValue(name, out int source))
                {
                    throw new NotSupportedException(Unsupported);
                }

                owner.MoveRegister(
                    YRegisterTag,
                    source,
                    YRegisterTag,
                    slot
                );
            }
        }

        private void Conditional(Expr.If expression, IReadOnlyList<string>? exports = null, Expr[]? branchContinuation = null)
        {
            int done = Label();
            var original = variables;
            var joined = ReserveBranchExports(exports);
            int branchBase = nextSlot;
            int branchLimit = branchBase;
            bool exhausted = true;
            foreach (Clause clause in expression.Clauses)
            {
                variables = new(original, StringComparer.Ordinal);
                nextSlot = branchBase;
                if (clause.Guard is Expr.Literal { Value: Atom { Name: "true" } })
                {
                    EmitExportingBranch(clause.Body, joined, branchContinuation: branchContinuation);
                    StoreBranchExports(joined);
                    branchLimit = Math.Max(branchLimit, nextSlot);
                    exhausted = false;
                    break;
                }
                int next = Label();
                Guard(clause.Guard, next);
                EmitExportingBranch(clause.Body, joined, branchContinuation: branchContinuation);
                StoreBranchExports(joined);
                branchLimit = Math.Max(branchLimit, nextSlot);
                JumpTo(done);
                Mark(next);
            }
            if (exhausted)
            {
                Error("if_clause");
            }
            Mark(done);
            variables = original;
            nextSlot = branchLimit;
            foreach (var (name, slot) in joined)
            {
                variables.Add(name, slot);
            }
        }

        private (int Head, Expr.Call Tail, Expr.Call Raise, Term Class, Term Reason)? ReadForwardedStackRaise(Expr body, string stack)
        {
            if (body is not Expr.Sequence
                {
                    Items: [
                Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.List { Items: [Expr.Variable head], Tail: Expr.Call tail } },
                Expr.Call
                    {
                        Module: ErlangModuleNames.Module, Function: "raise", DynamicRemote: false,
                        Arguments: [var classArgument, var reasonArgument, Expr.Variable trace]
                    } raise
            ]
                }
                || binding.Name == VariableScopeNames.Wildcard || binding.Name == stack || binding.Name != trace.Name
                || variables.ContainsKey(binding.Name) || deferredPairMembers.ContainsKey(binding.Name)
                || !variables.TryGetValue(head.Name, out int headSlot) || deferredPairMembers.ContainsKey(head.Name)
                || tail.Arguments is not [Expr.Variable source] || source.Name != stack || stack == VariableScopeNames.Wildcard
                || !IsGuardBif(tail, AutoImportNames.Tl) || !owner.ResolvesErlangBif(tail)
                || !AttributeLiteral.TryNormalizeConstant(classArgument, out Term exceptionClass)
                || !AttributeLiteral.TryNormalizeConstant(reasonArgument, out Term reason))
            {
                return null;
            }

            return (headSlot, tail, raise, exceptionClass, reason);
        }

        private void EmitForwardedStackRaise((int Head, Expr.Call Tail, Expr.Call Raise, Term Class, Term Reason) plan)
        {
            owner.EmitCallLine(plan.Tail, location);
            owner.Op(BifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Tl, 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, plan.Head);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.EmitConstant(plan.Reason, 1);
            owner.EmitConstant(plan.Class, 0);
            owner.EmitCallLine(plan.Raise, CallLocation(plan.Tail, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, plan.Raise.Function, 3));
            owner.Operand(BeamWire.UnsignedTag, frameSize);
        }

        private bool TryEmitDirectExceptionMatch(Expr.Try expression, int? reservedToken, bool terminal = false)
        {
            if (expression.Body is not Expr.Call
                {
                    DynamicRemote: false, Module: null or ErlangModuleNames.Module, Arguments.Count: 1,
                    Function: AutoImportNames.Error or AutoImportNames.Exit or AutoImportNames.Throw
                } call || !owner.ResolvesErlangBif(call)
                || expression.Catches is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                        Pattern.Literal { Value: Atom exceptionClass }, Pattern.Literal reason, Pattern.Variable stack
                    ]
                    }]
                } clause]
                || variables.ContainsKey(stack.Name) || deferredPairMembers.ContainsKey(stack.Name)
                || ReadUsedEntryParameters(new Clause([stack], null, clause.Body)) is not { } used)
            {
                return false;
            }

            var forwardedRaise = terminal ? ReadForwardedStackRaise(clause.Body, stack.Name) : null;
            if (terminal && forwardedRaise is null)
            {
                return false;
            }

            var original = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            int token = reservedToken ?? Slot();
            int handler = Label();
            int unmatched = Label();
            int done = terminal ? 0 : Label();
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            Expression(call);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(exceptionClass);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(reason.Value);

            variables = new(original, StringComparer.Ordinal);
            if (used.Contains(stack.Name))
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    0
                );
                owner.instructions.WriteByte(BuildStacktrace);
                if (!terminal)
                {
                    variables.Add(stack.Name, Save());
                }
            }
            if (forwardedRaise is { } forwarded)
            {
                EmitForwardedStackRaise(forwarded);
            }
            else
            {
                Expression(clause.Body);
                JumpTo(done);
            }

            Mark(unmatched);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            if (!terminal)
            {
                Mark(done);
            }
            variables = original;

            return true;
        }

        private void ProtectedExpression(Expr.Try expression, int? reservedToken = null)
        {
            if (TryEmitDirectExceptionMatch(expression, reservedToken))
            {
                return;
            }
            int token = reservedToken ?? Slot();
            int handler = Label();
            int done = Label();
            var original = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            Expression(expression.Body, null);
            owner.Op(TryEnd, YRegisterTag, token);
            if (expression.Clauses.Count == 0)
            {
                JumpTo(done);
            }
            else if (expression.Clauses is [
                {
                    Guard: null, Patterns: [Pattern.Variable success],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] }
                }]
                && success.Name != VariableScopeNames.Wildcard && success.Name == value.Name
                && !variables.ContainsKey(success.Name))
            {
                EmitTaggedRegisterResult(tag);
                JumpTo(done);
            }
            else
            {
                int result = Save();
                var successScope = new Dictionary<string, int>(variables, StringComparer.Ordinal);
                foreach (Clause clause in expression.Clauses)
                {
                    int next = Label();
                    variables = new(successScope, StringComparer.Ordinal);
                    Pattern(clause.Patterns.Single(), result, next);
                    Guard(clause.Guard, next);
                    Expression(clause.Body);
                    JumpTo(done);
                    Mark(next);
                }
                Load(result);
                owner.Op(TryCaseEnd, XRegisterTag, 0);
            }

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            int exceptionClass = Save();
            int reason = Slot();
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                reason
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            int rawStack = Save();
            owner.instructions.WriteByte(BuildStacktrace);
            int stack = Save();
            bool unpacked = expression.Catches.All(clause => clause.Patterns is [Pattern.Tuple { Items.Count: 3 }]);
            int caught = -1;
            if (!unpacked)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 3);
                owner.Operand(YRegisterTag, exceptionClass);
                owner.Operand(YRegisterTag, reason);
                owner.Operand(YRegisterTag, stack);
                caught = Save();
            }
            foreach (Clause clause in expression.Catches)
            {
                int next = Label();
                variables = new(original, StringComparer.Ordinal);
                if (unpacked && clause.Patterns[0] is Pattern.Tuple tuple)
                {
                    Pattern(tuple.Items[0], exceptionClass, next);
                    Pattern(tuple.Items[1], reason, next);
                    Pattern(tuple.Items[2], stack, next);
                }
                else
                {
                    Pattern(clause.Patterns.Single(), caught, next);
                }
                Guard(clause.Guard, next);
                Expression(clause.Body, null);
                JumpTo(done);
                Mark(next);
            }
            Load(rawStack);
            Load(reason, 1);
            owner.Op(Raise, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            Mark(done);
            variables = original;
        }

        private int ValueSlot(Expr expression, int? failure)
        {
            if (expression is Expr.Variable variable && variables.TryGetValue(variable.Name, out int slot))
            {
                return slot;
            }

            Expression(expression, failure);

            return Save();
        }

        private int[] Values(IReadOnlyList<Expr> expressions, int? failure)
        {
            var slots = new List<int>();
            foreach (Expr expression in expressions)
            {
                slots.Add(ValueSlot(expression, failure));
            }

            return slots.ToArray();
        }

        private void ShortCircuit(Expr.Binary binary, int? guardFailure)
        {
            Expression(binary.Left, guardFailure);
            int done = Label();
            int evaluate = Label();
            string shortValue = binary.Operator == ErlangOperators.AndAlso ? "false" : "true";
            owner.Op(ExactEqualTest, LabelTag, evaluate);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(AtomTag, owner.Intern(shortValue));
            JumpTo(done);
            Mark(evaluate);
            int invalid = Label();
            owner.Op(ExactEqualTest, LabelTag, invalid);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(AtomTag, owner.Intern(shortValue == "false" ? "true" : "false"));
            var outer = variables;
            variables = new(outer, StringComparer.Ordinal);
            Expression(binary.Right, guardFailure);
            variables = outer;
            JumpTo(done);
            Mark(invalid);
            if (guardFailure is int fail)
            {
                JumpTo(fail);
            }
            else
            {
                Error("badarg");
            }
            Mark(done);
        }

        private void Comprehension(Expr.ListComprehension comprehension)
        {
            if (comprehension is not
                {
                    Items: [var body],
                    Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: var pattern, Source: var source }]
                })
            {
                EmitListQualifiers(comprehension);

                return;
            }

            Expression(source);
            int current = Save();
            owner.EmitConstant(Nil.Value, 0);
            int accumulator = Save();
            int loop = Label();
            int skip = Label();
            int end = Label();
            Mark(loop);
            Load(current);
            owner.EmitNonemptyList(0, end);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                current
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            int item = Save();
            var outer = variables;
            variables = new(outer, StringComparer.Ordinal);
            MatchGeneratorPattern(pattern, item, skip);
            Expression(body);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, accumulator);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                accumulator
            );
            variables = outer;
            Mark(skip);
            JumpTo(loop);
            Mark(end);
            Load(current);
            int invalid = Label();
            owner.EmitNilTest(XRegisterTag, 0, invalid);
            Load(accumulator);
            owner.EmitConstant(Nil.Value, 1);
            Call("lists", "reverse", 2);
            int done = Label();
            JumpTo(done);
            Mark(invalid);
            ErrorTuple("bad_generator", current);
            Mark(done);
        }

        private void Fold(
            Expr.Call call,
            Expr.Fun fun,
            Expr initial,
            Expr source
        )
        {
            EmitGeneralClosure(fun, emitCapturedFailure: true);
            int callback = Save();
            int accumulator = ValueSlot(initial, null);
            int list = ValueSlot(source, null);
            Load(callback);
            Load(accumulator, 1);
            Load(list, 2);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 3);
        }

        private void ShadowPattern(Pattern pattern)
        {
            switch (pattern)
            {
                case Pattern.Variable variable:
                    variables.Remove(variable.Name);
                    checkedRecordBindings.Remove(variable.Name);
                    break;
                case AliasPattern alias:
                    ShadowPattern(alias.Left);
                    ShadowPattern(alias.Right);
                    break;
                case Pattern.Tuple tuple:
                    foreach (Pattern item in tuple.Items)
                    {
                        ShadowPattern(item);
                    }
                    break;
                case Pattern.List list:
                    foreach (Pattern item in list.Items)
                    {
                        ShadowPattern(item);
                    }
                    if (list.Tail is { } tail)
                    {
                        ShadowPattern(tail);
                    }
                    break;
                case MapPattern map:
                    foreach (var field in map.Fields)
                    {
                        ShadowPattern(field.Value);
                    }
                    break;
                case BitPattern bits:
                    foreach (var segment in bits.Segments)
                    {
                        ShadowPattern(segment.Value);
                    }
                    break;
            }
        }

        private void Error(string reason)
        {
            owner.EmitConstant(Term.A(reason), 0);
            Call(ErlangModuleNames.Module, "error", 1);
        }

        private void ErrorTuple(string reason, int value)
        {
            Load(value);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitProtectedPair(AtomTag, owner.Intern(reason));
            Call(ErlangModuleNames.Module, "error", 1);
        }
    }
}
