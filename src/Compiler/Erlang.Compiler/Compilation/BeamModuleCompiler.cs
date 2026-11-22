// SPDX-License-Identifier: Apache-2.0
// Module layout based on OTP-29.1.1 lib/compiler/src/beam_asm.erl.
// Original Erlang implementation copyright Ericsson AB 1996-2026.

using System.Numerics;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Compiler.BeamContainerWriter;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler(
    string name,
    string? defaultLineFile = null,
    ModuleDefinition? sourceModule = null,
    bool maybeEnabled = true,
    bool automaticCoreInlining = false,
    bool entryTupleAliasesEnabled = true,
    bool inlineListFunctions = false
)
{
    internal bool CoreMapCallsEnabled { get; init; } = true;

    private string? OnLoadFunction => sourceModule?.OnLoadFunction;

    // The source binding parser records no_ssa_opt under other compile options.
    private bool SsaTypeInferenceEnabled => sourceModule?.HasOtherCompileOptions != true;
    private bool PruneAliasedEntryTupleFields =>
        entryTupleAliasesEnabled && !ModuleOptimizationDisabled && SsaTypeInferenceEnabled;

    private readonly Dictionary<string, int> atoms = new(StringComparer.Ordinal) { [name] = 1 };
    private readonly List<(int Atom, int Arity, int Entry)> exports = [];
    private readonly List<(int Atom, int Arity, int Entry)> locals = [];
    private readonly Dictionary<(int Module, int Function, int Arity), int> imports = [];
    private readonly string implicitLineFile = name + ".erl";
    private readonly Dictionary<BeamSourceLocation, int> lines = [];
    private readonly Dictionary<string, int> filenames = new(StringComparer.Ordinal);
    private int lineInstructions;
    private readonly MemoryStream instructions = new();
    private int highestOpcode = MinimumHighestOpcode;
    private int nextLabel = 1;
    private readonly HashSet<string> deferredProjections = new(StringComparer.Ordinal);
    private readonly HashSet<string> wxUnicodeMappings = new(StringComparer.Ordinal);
    private readonly List<(int Position, int Target)> deferredLabelOperands = [];
    private readonly Dictionary<int, int> deferredEntries = [];
    private IReadOnlySet<(string Name, int Arity)> exportedFunctions = new HashSet<(string, int)>();

    internal byte[] Emit(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyList<BeamSourceLocation> sourceLines,
        IReadOnlySet<(string Name, int Arity)> publicFunctions,
        BeamModuleMetadata metadata
    )
    {
        int declaredCount = functions.Count(function => !function.IsInlineFailure);
        var callbackInfo = ReadCallbackInfo();
        int callbackLabels = callbackInfo is { } callbacks
            ? BehaviourInfoLabels - (callbacks.Callbacks.Equals(callbacks.Optional) ? 1 : 0)
            : 0;
        exportedFunctions = publicFunctions;
        functions = functions.Select(FoldLiteralAtomConversions).Select(FoldUnusedTupleBindings).Select(FoldBinaryMatchTupleBindings).Select(PruneUnusedEntryTupleElements).ToArray();
        functions = PropagateLocalConstantArguments(functions);
        if (!ModuleOptimizationDisabled)
        {
            functions = functions.Select(FoldCoveredCaseArms)
                .Select(FoldTerminalCaseConstants)
                .Select(FoldTerminalReconstructionCases)
                .Select(SinkCheckedCallArgumentFields)
                .Select(ReuseNonterminalCaseCarriers)
                .Select(ReuseAssignedPairConsHeads)
                .Select(ReuseTerminalEffectfulCaseCarriers)
                .Select(SubstituteDirectAnonymousHelperConstants).ToArray();
            if (entryTupleAliasesEnabled)
            {
                functions = functions.Select(ReuseEntryTupleCarriers).ToArray();
            }
        }

        (functions, sourceLines) = ExtractClosedComprehensions(functions, sourceLines);
        IndexEntryMatchedMapUpdates(functions);
        using var code = instructions;
        var localEntries = new Dictionary<(string Name, int Arity), int>();
        var reachable = new HashSet<(string Name, int Arity)>();
        var byName = functions.ToDictionary(function => (function.Name, function.Arity));
        properListSourceFunctions = byName;
        foreach (var function in functions)
        {
            if (ReadChoiceSearch(function.Clauses) is { } search && search.Recur.Function == function.Name)
            {
                choiceResultFunctions.Add(function.Name);
            }
            if (ReadTaggedChoice(function.Clauses) is { } tagged && tagged.Recur.Function == function.Name
                || ReadChoiceAcceptance(function.Clauses) is { } acceptance && acceptance.Recur.Function == function.Name)
            {
                booleanChoiceFunctions.Add(function.Name);
            }
            if (ReadBerContinuation(function.Clauses) is { } ber && ber.Recur.Function == function.Name)
            {
                resumableBerFunctions.Add(function.Name);
            }
            if (function.Clauses is [var clause] && ReadSignedWidth(clause) is { } width
                && width.Recur.Function == function.Name)
            {
                numericWidthFunctions.Add((function.Name, function.Arity));
            }
        }
        foreach (var function in functions)
        {
            if (ReadTupleMappingCase(function) is { } mapping)
            {
                tupleMappingCases.Add((function.Name, function.Arity), mapping);
            }
        }

        var general = PlanGeneralFunctions(functions, sourceLines, byName);
        var pending = new Stack<(string Name, int Arity)>(publicFunctions);
        if (sourceModule?.OnLoadFunction is { } onLoad)
        {
            pending.Push((onLoad, 0));
        }
        while (pending.TryPop(out var entry))
        {
            if (!reachable.Add(entry) || !byName.TryGetValue(entry, out var function))
            {
                continue;
            }
            if (zippedListOwners.TryGetValue(entry, out var zipped))
            {
                pending.Push((zipped.Helper, ZippedListArity));
                pending.Push((zipped.Mapper, ZippedListArity));
            }

            if (zippedMapOwners.TryGetValue(entry, out string? zippedMap))
            {
                pending.Push((zippedMap, ZippedListArity));
            }

            Clause first = function.Clauses[0];
            foreach (var reference in LocalFunctionEntries.Walk(first.Body).OfType<Expr.FunctionReference>())
            {
                if (byName.ContainsKey((reference.Name, reference.Arity)))
                {
                    pending.Push((reference.Name, reference.Arity));
                }
            }
            foreach (var closure in LocalFunctionEntries.Walk(first.Body).OfType<Expr.Fun>())
            {
                if (closure.Clauses is [{ Guard: not null, Body: var guardedBody }]
                    && ReadGuardedLocalReply(guardedBody) is { } reply
                    && byName.ContainsKey((reply.Tail.Function, reply.Tail.Arguments.Count)))
                {
                    pending.Push((reply.Tail.Function, reply.Tail.Arguments.Count));
                }
                if (closure.Clauses is [{ Guard: not null, Body: var filterBody }]
                    && ReadGuardedOptionFilter(filterBody) is { } filter
                    && byName.ContainsKey((filter.Filter.Function, 1)))
                {
                    pending.Push((filter.Filter.Function, 1));
                }
                if (closure.Clauses is not [{ Patterns: [], Body: var closureBody }])
                {
                    continue;
                }
                if (ReadFormattingClosure(closureBody, first.Patterns) is { } formatting)
                {
                    pending.Push((formatting.Finish.Function, 2));
                }
                if (ReadProtectedTermClosure(closureBody, first.Patterns) is { } protectedTerm)
                {
                    pending.Push((protectedTerm.Render.Function, 2));
                }
                if (ReadBinaryTextClosure(closureBody, first.Patterns) is { } text)
                {
                    pending.Push((text.Digits.Function, 2));
                    pending.Push((text.Finish.Function, 2));
                }
            }
            if ((FindCall(first.Body) ?? ReadCallChain(first)?.Input) is { Module: null } call
                && byName.ContainsKey((call.Function, call.Arguments.Count)))
            {
                pending.Push((call.Function, call.Arguments.Count));
            }
            if (ReadConstantCallSequence(first) is { } sequence)
            {
                foreach (var item in sequence.Calls)
                {
                    if (item.Source.Module is null && byName.ContainsKey((item.Source.Function, item.Arguments.Count)))
                    {
                        pending.Push((item.Source.Function, item.Arguments.Count));
                    }
                }
            }
            if (function.Clauses.Count == 1 && HasNilParameter(first) && ReadCallListResult(first) is { } callList)
            {
                foreach (Expr item in callList.Calls.Items)
                {
                    if (item is Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } child
                        && byName.ContainsKey((child.Function, 0)))
                    {
                        pending.Push((child.Function, 0));
                    }
                }
            }
            if (ReadOctetMapping(function.Clauses) is { } octets)
            {
                pending.Push((octets.Convert.Function, 1));
            }
            if (ReadTableProjection(function.Clauses) is { } table)
            {
                pending.Push((table.Columns.Function, 0));
            }
            if (ReadAllocatorRows(function.Clauses) is { } allocator)
            {
                pending.Push((allocator.Columns.Function, 1));
            }
            if (ReadTaggedEncoding(function.Clauses) is { } encodeTags)
            {
                pending.Push((encodeTags.Wrap.Function, 3));
            }
            if (ReadTagDecoding(function.Clauses) is { } tags)
            {
                pending.Push((tags.Recur.Function, 3));
                foreach (var payload in new[] { tags.Plain, tags.Wrapped })
                {
                    pending.Push((payload.Test.Function, 1));
                    pending.Push((payload.Tag.Function, 1));
                    pending.Push((payload.Decode.Function, 2));
                }
            }
            if (ReadFieldDecoding(function.Clauses) is { } decodeFields)
            {
                pending.Push((decodeFields.Convert.Function, 2));
                pending.Push((decodeFields.Append.Function, 3));
                pending.Push((decodeFields.Test.Function, 2));
                pending.Push((decodeFields.Missing.Function, 1));
            }
            if (ReadFieldEncoding(function.Clauses) is { } fields)
            {
                pending.Push((fields.Convert.Function, 2));
                pending.Push((fields.Recur.Function, 2));
            }
            if (ReadChoiceSearch(function.Clauses) is { } search)
            {
                pending.Push((search.Test.Function, 2));
                pending.Push((search.Recur.Function, 2));
            }
            if (ReadChoiceAcceptance(function.Clauses) is { } acceptance)
            {
                pending.Push((acceptance.Lookup.Function, 1));
                pending.Push((acceptance.Recur.Function, 2));
                pending.Push((acceptance.Search.Function, 2));
                pending.Push((acceptance.Tag.Function, 1));
            }
            if (ReadBerTag(function.Clauses) is { } tag)
            {
                pending.Push((tag.Long.Function, 2));
            }
            if (ReadBerContinuation(function.Clauses) is { } continuation)
            {
                pending.Push((continuation.Recur.Function, 2));
            }
            if (ReadSignedWriter(first) is { } writer)
            {
                pending.Push((writer.Width.Function, 2));
            }
            if (ReadSignedWidth(first) is { } signedWidth)
            {
                pending.Push((signedWidth.Recur.Function, 2));
            }
            if (ReadSizedCall(first) is { } sizedCall)
            {
                pending.Push((sizedCall.Lookup.Function, 1));
                pending.Push((sizedCall.Convert.Function, 2));
                pending.Push((sizedCall.Classify.Function, 1));
                pending.Push((sizedCall.Serialize.Function, 3));
            }
            if (ReadProtectedCall(first) is { } protectedCall)
            {
                pending.Push((protectedCall.Lookup.Function, 1));
                pending.Push((protectedCall.Transform.Function, 2));
            }
            if (ReadLocalBinding(first) is { } binding)
            {
                pending.Push((binding.Input.Function, binding.Input.Arguments.Count));
                if (binding.Tail is { } tail)
                {
                    pending.Push((tail.Function, tail.Arguments.Count));
                }
            }
            if (ReadTaggedField(function.Clauses) is { } field)
            {
                pending.Push((field.Input.Function, 1));
                pending.Push((field.Tail.Function, 1));
            }
            if (ReadTaggedChoice(function.Clauses) is { } choice)
            {
                pending.Push((choice.Input.Function, 1));
            }
        }
        if (general.Count == 0 && tupleMappingCases.Count == 0 && !reachable.SetEquals(byName.Keys))
        {
            throw new NotSupportedException(Unsupported);
        }

        var finiteAtomDispatches = new Dictionary<(string Name, int Arity), FiniteAtomLiteralDispatch>();
        int entryLabel = 2;
        for (int index = 0; index < functions.Count; index++)
        {
            if (index == declaredCount)
            {
                RecordModuleInfoEntries(entryLabel);
                entryLabel += 4 + callbackLabels;
            }
            var function = functions[index];
            if (index >= declaredCount && deferredProjections.Contains(function.Name))
            {
                localEntries.Add((function.Name, function.Arity), -index);
                continue;
            }
            localEntries.Add((function.Name, function.Arity), entryLabel);
            if (function.IsInlineFailure)
            {
                entryLabel += 2;
                continue;
            }
            if (zippedListOwners.ContainsKey((function.Name, function.Arity)))
            {
                entryLabel += 2;
                continue;
            }
            if (mapListIteratorHelpers.ContainsKey((function.Name, function.Arity)))
            {
                entryLabel += FilteredMapIteratorHelperLabels;
                continue;
            }

            if (mapIteratorHelpers.TryGetValue((function.Name, function.Arity), out var excludedMapValue))
            {
                entryLabel += excludedMapValue is null ? MapIteratorHelperLabels : FilteredMapIteratorHelperLabels;
                continue;
            }
            if (zippedListHelpers.ContainsKey((function.Name, function.Arity))
                || zippedMapHelpers.ContainsKey((function.Name, function.Arity)))
            {
                entryLabel += ZippedListHelperLabelCount;
                continue;
            }
            if (writableBinaryListOwners.TryGetValue((function.Name, function.Arity), out var writableOwner))
            {
                entryLabel += writableOwner.TuplePair ? WritableBinaryTupleOwnerLabelCount : WritableBinaryListLabelCount;
                continue;
            }
            if (writableBinaryListHelpers.TryGetValue((function.Name, function.Arity), out var writableHelper))
            {
                entryLabel += writableHelper.TuplePair ? WritableBinaryTupleHelperLabelCount : WritableBinaryListLabelCount;
                continue;
            }
            if (general.TryGetValue((function.Name, function.Arity), out var plan))
            {
                entryLabel += plan.Labels;
                continue;
            }
            if (tupleMappingCases.ContainsKey((function.Name, function.Arity)))
            {
                entryLabel += 4;
                continue;
            }

            if (capturedListFilters.ContainsKey((function.Name, function.Arity)))
            {
                entryLabel += 5;
                continue;
            }

            var clauses = function.Clauses;
            if (ReadAllocatorRows(clauses) is not null)
            {
                entryLabel += 7;
            }
            else if (ReadDiscardedListCalls(function, byName) is not null)
            {
                entryLabel += 4;
            }
            else if (ReadOctetMapping(clauses) is not null || ReadTaggedListJoin(clauses) is not null
                || ReadTupleListFormatting(clauses) is not null || ReadTableProjection(clauses) is not null)
            {
                entryLabel += 3;
            }
            else if (ReadTaggedEncoding(clauses) is not null)
            {
                entryLabel += 4;
            }
            else if (ReadTagDecoding(clauses) is { } tags)
            {
                entryLabel += 7;
                if (!booleanChoiceFunctions.Contains(tags.Plain.Test.Function))
                {
                    entryLabel++;
                }
                if (!booleanChoiceFunctions.Contains(tags.Wrapped.Test.Function))
                {
                    entryLabel++;
                }
            }
            else if (ReadFieldDecoding(clauses) is { } decodeFields)
            {
                entryLabel += booleanChoiceFunctions.Contains(decodeFields.Test.Function) ? 6 : 7;
            }
            else if (ReadFieldEncoding(clauses) is not null)
            {
                entryLabel += 7;
            }
            else if (ReadChoiceSearch(clauses) is { } search)
            {
                entryLabel += booleanChoiceFunctions.Contains(search.Test.Function) ? 4 : 5;
            }
            else if (ReadChoiceAcceptance(clauses) is not null)
            {
                entryLabel += 5;
            }
            else if (ReadBerTag(clauses) is not null || ReadBerContinuation(clauses) is not null)
            {
                entryLabel += 4;
            }
            else if (ReadTaggedField(clauses) is not null)
            {
                entryLabel += 6;
            }
            else if (ReadTaggedChoice(clauses) is not null)
            {
                entryLabel += 5;
            }
            else if (clauses is [var only] &&
                (ReadBinaryCapture(only) is not null || ReadLocalBinding(only) is { Tail: null }
                    || ReadSignedInteger(only) is not null || ReadSignedWidth(only) is not null))
            {
                entryLabel += 3;
            }
            else if (IsFiniteLiteralDispatch(clauses) && clauses.Count > 1)
            {
                var analyzer = new GeneralEmitter(
                    this,
                    localEntries,
                    byName,
                    sourceLines[index],
                    frameSize: 0
                );
                if (analyzer.ReadFiniteAtomLiteralDispatch(function) is { } dispatch)
                {
                    finiteAtomDispatches.Add((function.Name, function.Arity), dispatch);
                    entryLabel += dispatch.LabelCount + 2;
                }
                else
                {
                    entryLabel += clauses.Select(DispatchResult).Distinct().Count() + 2;
                }
            }
            else
            {
                entryLabel += clauses.Count > 2 ? clauses.SkipLast(1).Select(DispatchResult).Distinct().Count() + 3 : clauses.Count + 1;
            }
            if (clauses is [var protectedClause] && ReadProtectedCall(protectedClause) is { } protectedCall)
            {
                entryLabel += (protectedCall.Unpack ? 4 : 3) + (protectedCall.UnwrapReason ? 1 : 0);
            }
            if (ReadPreservedCallCase(function.Clauses) is not null
                || function.Clauses.Count == 1 && ReadWrappedCallCase(function.Clauses[0]) is not null)
            {
                entryLabel++;
            }
            else if (function.Clauses.Count == 1 && ReadCallCase(function.Clauses[0]) is not null)
            {
                entryLabel += 2;
            }
        }
        if (declaredCount == functions.Count)
        {
            RecordModuleInfoEntries(entryLabel);
        }

        void RecordModuleInfoEntries(int firstEntry)
        {
            int moduleInfoEntry = firstEntry + callbackLabels;
            localEntries.Add(("module_info", 0), moduleInfoEntry);
            localEntries.Add(("module_info", 1), moduleInfoEntry + 2);
        }

        var mappings = new List<(string Name, MappingPlan Map, BeamSourceLocation Location)>();
        int helperEntry = entryLabel + (declaredCount == functions.Count ? 4 + callbackLabels : 0);
        for (int index = functions.Count - 1; index >= 0; index--)
        {
            var function = functions[index];
            var map = ReadListMappingPlan(function.Clauses);
            if (map is null)
            {
                continue;
            }
            string helper = MappingName(function.Name, function.Arity);
            if (!localEntries.TryAdd((helper, 1), helperEntry))
            {
                throw new NotSupportedException(Unsupported);
            }
            helperEntry += map switch
            {
                ListMapping { Discard: not null } => 6,
                ListMapping { Filter: not null } => 5,
                PairConsMapping => 5,
                _ => 4
            };
            mappings.Add((helper, map, sourceLines[index]));
        }
        var tupleMappingHelpers = new List<(string Name, TupleMappingCase Mapping, BeamSourceLocation Location)>();
        for (int index = functions.Count - 1; index >= 0; index--)
        {
            var function = functions[index];
            if (tupleMappingCases.TryGetValue((function.Name, function.Arity), out var mapping))
            {
                string helper = MappingName(function.Name, function.Arity);
                if (!localEntries.TryAdd((helper, 2), helperEntry))
                {
                    throw new NotSupportedException(Unsupported);
                }

                helperEntry += 3;
                tupleMappingHelpers.Add((helper, mapping, sourceLines[index]));
            }
        }

        var discardedHelpers = new List<(string Name, DiscardedListCalls Flow, BeamSourceLocation Location)>();
        for (int index = functions.Count - 1; index >= 0; index--)
        {
            var function = functions[index];
            if (ReadDiscardedListCalls(function, byName) is not { } flow)
            {
                continue;
            }

            string helper = MappingName(function.Name, function.Arity);
            if (!localEntries.TryAdd((helper, 1), helperEntry))
            {
                throw new NotSupportedException(Unsupported);
            }
            helperEntry += 4;
            discardedHelpers.Add((helper, flow, sourceLines[index]));
        }

        var capturedTableHelpers = new List<(string Name, CapturedTableList Flow, BeamSourceLocation Location)>();
        for (int index = functions.Count - 1; index >= 0; index--)
        {
            var function = functions[index];
            if (ReadCapturedTableList(function, byName) is not { } flow)
            {
                continue;
            }
            string helper = MappingName(function.Name, 0);
            if (!localEntries.TryAdd((helper, 2), helperEntry))
            {
                throw new NotSupportedException(Unsupported);
            }
            helperEntry += 3;
            capturedTableHelpers.Add((helper, flow, sourceLines[index]));
        }

        var pageHelpers = new List<(string Name, PageComprehension Flow, BeamSourceLocation Location)>();
        for (int index = functions.Count - 1; index >= 0; index--)
        {
            var function = functions[index];
            if (ReadPageComprehension(function, byName) is not { } flow)
            {
                continue;
            }

            string helper = MappingName(function.Name, function.Arity);
            if (!localEntries.TryAdd((helper, 3), helperEntry))
            {
                throw new NotSupportedException(Unsupported);
            }

            helperEntry += 5;
            pageHelpers.Add((helper, flow, sourceLines[index]));
        }

        var closureBoundaries = new Dictionary<int, List<int>>();
        int[]? firstClosure = hasMixedRecursiveDefinitions ? new int[declaredCount + 1] : null;
        for (int index = 0; index < functions.Count; index++)
        {
            if (firstClosure is not null && index < declaredCount)
            {
                firstClosure[index] = closures.Count;
            }

            if (index == declaredCount)
            {
                AddMetadataFunctions(callbackInfo);
            }
            var function = functions[index];
            var line = sourceLines[index];
            if (index >= declaredCount && deferredProjections.Contains(function.Name))
            {
                continue;
            }
            if (index < declaredCount && wxUnicodeMappings.Count != 0)
            {
                string helper = MappingName(function.Name, function.Arity);
                if (wxUnicodeMappings.Contains(helper))
                {
                    if (!closureBoundaries.TryGetValue(closures.Count, out var group))
                    {
                        group = [];
                        closureBoundaries.Add(closures.Count, group);
                    }

                    group.Add(-localEntries[(helper, 1)]);
                }
            }

            StartFunction(
                function.Name,
                function.Arity,
                line,
                publicFunctions.Contains((function.Name, function.Arity))
            );
            if (function.IsInlineFailure)
            {
                Op(Jump, LabelTag, nextLabel - 2);
            }
            else if (tupleMappingCases.TryGetValue((function.Name, function.Arity), out var tupleMapping))
            {
                EmitTupleMappingCase(tupleMapping, line, localEntries[(MappingName(function.Name, function.Arity), 2)]);
            }
            else if (capturedListFilters.TryGetValue((function.Name, function.Arity), out var filter))
            {
                EmitCapturedListFilter(
                    filter,
                    localEntries[(function.Name, function.Arity)],
                    localEntries,
                    line
                );
            }
            else if (zippedListOwners.TryGetValue((function.Name, function.Arity), out var zippedOwner))
            {
                Op(TailLocalCall, BeamWire.UnsignedTag, ZippedListArity);
                Operand(LabelTag, localEntries[(zippedOwner.Helper, ZippedListArity)]);
            }
            else if (mapListIteratorHelpers.TryGetValue((function.Name, function.Arity), out var mapListProjection))
            {
                EmitMapIteratorHelper(localEntries[(function.Name, function.Arity)], null, mapListProjection);
            }
            else if (mapIteratorHelpers.TryGetValue((function.Name, function.Arity), out var excludedMapValue))
            {
                EmitMapIteratorHelper(localEntries[(function.Name, function.Arity)], excludedMapValue);
            }
            else if (zippedMapHelpers.TryGetValue((function.Name, function.Arity), out bool reversedMapPair))
            {
                var signature = (function.Name, function.Arity);
                EmitZippedMapPairHelper(
                    localEntries[signature],
                    reversedMapPair,
                    HasProperListEntryFact(signature, 0),
                    HasProperListEntryFact(signature, 1)
                );
            }
            else if (zippedListHelpers.TryGetValue((function.Name, function.Arity), out var zippedHelper))
            {
                EmitZippedListHelper(
                    localEntries[(function.Name, function.Arity)],
                    localEntries[(zippedHelper.Mapper, ZippedListArity)]
                );
            }
            else if (writableBinaryListOwners.TryGetValue((function.Name, function.Arity), out var writableOwner))
            {
                EmitWritableBinaryListOwner(
                    localEntries[(writableOwner.HelperName, WritableBinaryListHelperArity)],
                    writableOwner
                );
            }
            else if (writableBinaryListHelpers.TryGetValue((function.Name, function.Arity), out var writableHelper))
            {
                EmitWritableBinaryListHelper(
                    localEntries[(function.Name, function.Arity)],
                    writableHelper,
                    localEntries
                );
            }
            else if (TryEmitReturnedClosure(function, line))
            {
                continue;
            }
            else if (ReadDiscardedListCalls(function, byName) is { } flow)
            {
                EmitDiscardedListDispatch(flow, localEntries[(MappingName(function.Name, function.Arity), 1)], line);
            }
            else if (general.TryGetValue((function.Name, function.Arity), out var plan))
            {
                new GeneralEmitter(
                    this,
                    localEntries,
                    byName,
                    line,
                    plan.Slots,
                    plan.Labels,
                    plan.ListFailures,
                    plannedTupleFrames: plan.TupleFrames,
                    plannedReversedFrame: plan.ReverseFrameSlots
                )
                    .Function(function);
            }
            else
            {
                try
                {
                    if (new GeneralEmitter(
                        this,
                        localEntries,
                        byName,
                        line,
                        frameSize: 0
                    )
                        .TryEmitSingletonResultTailWrapper(function))
                    {
                        continue;
                    }

                    int selectorType = 0;
                    if (function.Clauses.Count >= 2 && function.Clauses[0].Patterns is [Pattern.Literal { Value: Atom }])
                    {
                        var analyzer = new GeneralEmitter(
                            this,
                            localEntries,
                            byName,
                            line,
                            frameSize: 0
                        );
                        selectorType = analyzer.ReadAtomEntryType(function, 0);
                    }

                    EmitFunctionBody(
                        function.Clauses,
                        line,
                        localEntries,
                        selectorType,
                        finiteAtomDispatches.GetValueOrDefault((function.Name, function.Arity))
                    );
                }
                catch (NotSupportedException error)
                {
                    throw new NotSupportedException($"{name}:{function.Name}/{function.Arity}: {error.Message}", error);
                }
            }
        }

        if (declaredCount == functions.Count)
        {
            AddMetadataFunctions(callbackInfo);
        }
        foreach (var (helper, map, location) in mappings)
        {
            if (map is ColumnMapping columns)
            {
                EmitColumnMappingHelper(
                    helper,
                    columns,
                    location,
                    localEntries[(helper, 1)]
                );
            }
            else if (map is PairConsMapping pairs)
            {
                EmitPairConsHelper(
                    helper,
                    pairs,
                    location,
                    localEntries[(helper, 1)]
                );
            }
            else
            {
                EmitMappingHelper(
                    helper,
                    (ListMapping)map,
                    location,
                    localEntries
                );
            }
        }
        foreach (var (helper, mapping, location) in tupleMappingHelpers)
        {
            EmitTupleMappingHelper(
                helper,
                mapping,
                location,
                localEntries[(helper, 2)]
            );
        }
        foreach (var (helper, flow, location) in discardedHelpers)
        {
            EmitDiscardedListHelper(
                helper,
                flow,
                location,
                localEntries
            );
        }
        foreach (var (helper, flow, location) in capturedTableHelpers)
        {
            EmitCapturedTableHelper(
                helper,
                flow,
                location,
                localEntries[(helper, 2)]
            );
        }

        foreach (var (helper, flow, location) in pageHelpers)
        {
            EmitPageComprehensionHelper(
                helper,
                flow,
                location,
                localEntries
            );
        }

        if (firstClosure is not null)
        {
            firstClosure[^1] = closures.Count;
            RecordRecursiveClosureBoundaries(
                functions,
                firstClosure,
                localEntries,
                closureBoundaries
            );
        }

        var guardFailures = new Dictionary<string, int>(StringComparer.Ordinal);

        void EmitDeferredProjection(int index)
        {
            var function = functions[index];
            var line = sourceLines[index];
            StartFunction(
                function.Name,
                function.Arity,
                line,
                exported: false
            );
            deferredEntries.Add(-index, nextLabel - 1);
            if (mapListIteratorHelpers.TryGetValue((function.Name, function.Arity), out var mapListProjection))
            {
                EmitMapIteratorHelper(nextLabel - 1, null, mapListProjection);

                return;
            }

            if (mapIteratorHelpers.TryGetValue((function.Name, function.Arity), out var excludedMapValue))
            {
                EmitMapIteratorHelper(nextLabel - 1, excludedMapValue);

                return;
            }

            if (writableBinaryListHelpers.TryGetValue((function.Name, function.Arity), out var writable))
            {
                EmitWritableBinaryListHelper(nextLabel - 1, writable, localEntries);

                return;
            }

            if (!general.TryGetValue((function.Name, function.Arity), out var plan))
            {
                EmitFunctionBody(function.Clauses, line, localEntries);

                return;
            }
            new GeneralEmitter(
                this,
                localEntries,
                byName,
                line,
                plan.Slots,
                plan.Labels,
                plan.ListFailures,
                plannedTupleFrames: plan.TupleFrames,
                plannedReversedFrame: plan.ReverseFrameSlots
            ).Function(function);
        }

        void EmitHelpersAtBoundary(int boundary)
        {
            if (!closureBoundaries.TryGetValue(boundary, out var group))
            {
                return;
            }

            for (int index = group.Count - 1; index >= 0; index--)
            {
                if (!deferredEntries.ContainsKey(-group[index]))
                {
                    EmitDeferredProjection(group[index]);
                }
            }
        }

        EmitLiftedClosures(
            localEntries,
            byName,
            guardFailures,
            emitAtBoundary: EmitHelpersAtBoundary
        );
        int emittedClosures = closures.Count;
        for (int index = declaredCount; index < functions.Count; index++)
        {
            if (deferredProjections.Contains(functions[index].Name) && !deferredEntries.ContainsKey(-index))
            {
                EmitDeferredProjection(index);
            }
        }
        EmitLiftedClosures(
            localEntries,
            byName,
            guardFailures,
            emittedClosures
        );
        EmitLiftedGuardFailures(guardFailures);
        if (callbackInfo is not null)
        {
            EmitBehaviourInfoFailure();
        }
        ResolveListClosureTargets();
        ResolveLocalCallTargets();
        ResolveDeferredLabelOperands();
        code.WriteByte(EndCode);

        var resolvedEntries = localEntries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value > 0 && emittedFunctionEntries.TryGetValue(entry.Key, out int actual) ? actual : entry.Value
        );

        return WriteImage(metadata, output => EmitCode(output, functions, resolvedEntries));
    }

    private byte[] WriteImage(BeamModuleMetadata metadata, Action<Stream> writeCode)
    {
        byte[] code = Payload(writeCode);
        (string Id, byte[] Payload)[] essentials =
        [
            (BeamWire.AtomsChunk, Payload(EmitAtoms)),
            (BeamWire.CodeChunk, code),
            ("StrT", stringTable.ToArray()),
            (BeamWire.ImportsChunk, Payload(output =>
            {
                Word(output, imports.Count);
                foreach (var entry in imports.OrderBy(entry => emittedImports?[entry.Value] ?? entry.Value))
                {
                    Word(output, MapEmittedAtom(entry.Key.Module));
                    Word(output, MapEmittedAtom(entry.Key.Function));
                    Word(output, entry.Key.Arity);
                }
            })),
            (BeamWire.ExportsChunk, Payload(output => EmitFunctionTable(output, exports))),
            .. (closures.Count == 0 ? Array.Empty<(string, byte[])>() : new[] { (ClosureChunk, Payload(output => EmitClosureTable(output, 0))) }),
            .. (literalBytes.Count == 0 ? Array.Empty<(string, byte[])>() : new[] { (LiteralsChunk, Payload(EmitLiterals)) }),
            .. (maybeEnabled ? new[] { ("Meta", ExternalTermFormat.Encode(Cons.From([
                Term.Tuple(Term.A("enabled_features"), Cons.From([Term.A(BeamMacroPreprocessor.MaybeFeature)]))
            ]))) } : Array.Empty<(string, byte[])>())
        ];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        foreach (var (_, payload) in essentials)
        {
            hash.AppendData(payload);
        }

        byte[] digest = hash.GetHashAndReset();
        if (closures.Count != 0)
        {
            int oldUnique = (int)(BinaryPrimitives.ReadUInt32BigEndian(digest)
                >> (FunctionCodeLayout.UniquePrefixBits - FunctionCodeLayout.OldUniqueBits));
            int index = Array.FindIndex(essentials, chunk => chunk.Id == ClosureChunk);
            essentials[index] = (ClosureChunk, Payload(output => EmitClosureTable(output, oldUnique)));
        }

        Term declared = Cons.From(metadata.Attributes.Select(attribute =>
            Term.Tuple(Term.A(attribute.Name), ModuleCodeAttributes.Values(attribute.Value))));
        byte[] attributes = ExternalTermFormat.Encode(ModuleCodeAttributes.WithDefaultVersion(declared, new BitString(digest)));
        byte[] info = metadata.CompileInfo ?? ExternalTermFormat.Encode(Cons.From([Term.Tuple(Term.A("version"), Term.String(CompilerVersion))]));
        byte[] debug = metadata.DebugInfo ?? ExternalTermFormat.Encode(Term.Tuple(
            Term.A("debug_info_v1"),
            Term.A("erl_abstract_code"),
            Term.Tuple(Term.A("none"), Cons.From([Term.A("deterministic")]))
        ));

        return Write([
            .. essentials,
            ("LocT", Payload(output => EmitFunctionTable(output, locals))),
            ("Attr", attributes),
            ("CInf", info),
            ("Dbgi", debug),
            .. (metadata.Documentation is null ? Array.Empty<(string, byte[])>() : new[] { ("Docs", metadata.Documentation) }),
            ("Line", Payload(EmitLines)),
            ("Type", Payload(EmitTypes))
        ]);
    }

    private void StartFunction(
        string function,
        int arity,
        BeamSourceLocation line,
        bool exported = true
    )
    {
        activeFunction = function;
        int atom = Intern(function);
        Op(Label, BeamWire.UnsignedTag, nextLabel++);
        EmitLine(line);
        instructions.WriteByte(FunctionInfo);
        Operand(AtomTag, 1);
        Operand(AtomTag, atom);
        Operand(BeamWire.UnsignedTag, arity);
        (exported ? exports : locals).Add((atom, arity, nextLabel));
        emittedFunctionEntries[(function, arity)] = nextLabel;
        Op(Label, BeamWire.UnsignedTag, nextLabel++);
        if (arity == 0 && function == sourceModule?.OnLoadFunction)
        {
            instructions.WriteByte(OnLoad);
        }
        if (IsDeclaredNative((function, arity)))
        {
            instructions.WriteByte(NifStart);
        }
    }

    internal static bool HasUnconditionalParameters(Clause clause)
    {
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in clause.Patterns)
        {
            if (parameter is not Pattern.Variable variable
                || (variable.Name != VariableScopeNames.Wildcard && !bound.Add(variable.Name)))
            {
                return false;
            }
        }

        return true;
    }

    private void EmitBody(Clause clause)
    {
        if (clause.Body is Expr.RecordConstruction record)
        {
            clause = clause with { Body = RecordExpressions.Create(record) };
        }
        if (clause.Body is Expr.Variable argument)
        {
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable parameter && parameter.Name == argument.Name)
                {
                    if (index != 0)
                    {
                        MoveToX0(XRegisterTag, index);
                    }

                    return;
                }
            }

            throw new NotSupportedException(Unsupported);
        }
        if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term literal))
        {
            EmitConstant(literal, 0);
        }
        else if (TryFoldBindings(clause, out literal))
        {
            EmitConstant(literal, 0);
        }
        else if (!TryEmitConstructor(clause))
        {
            throw new NotSupportedException(Unsupported);
        }
    }

    private static bool TryFoldBindings(Clause clause, out Term literal)
    {
        literal = Nil.Value;

        return clause.Body is Expr.Sequence { Items.Count: > 1 } sequence
            && ReadConstantBindings(clause, sequence) is { } bindings
            && AttributeLiteral.TryNormalizeConstant(sequence.Items[^1], out literal, bindings);
    }

    private static Dictionary<string, Term>? ReadConstantBindings(Clause clause, Expr.Sequence sequence) =>
        ReadConstantBindings(
            sequence,
            clause.Patterns.OfType<Pattern.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal)
        );

    private static Dictionary<string, Term>? ReadConstantBindings(Expr.Sequence sequence, IReadOnlySet<string> parameters)
    {
        var bindings = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Expr item in sequence.Items.SkipLast(1))
        {
            if (item is not Expr.Match { Pattern: Pattern.Variable variable } binding
                || variable.Name == VariableScopeNames.Wildcard || parameters.Contains(variable.Name)
                || !AttributeLiteral.TryNormalizeConstant(binding.Value, out Term value, bindings)
                || !bindings.TryAdd(variable.Name, value))
            {
                return null;
            }
        }

        return bindings;
    }

    private void AddMetadataFunctions((Term Callbacks, Term Optional)? callbackInfo)
    {
        if (callbackInfo is { } callbacks)
        {
            EmitBehaviourInfo(callbacks.Callbacks, callbacks.Optional);
        }
        StartFunction("module_info", 0, default);
        MoveToX0(AtomTag, 1);
        Op(TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import("erlang", "get_module_info", 1));
        StartFunction("module_info", 1, default);
        Op(Move, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        MoveToX0(AtomTag, 1);
        Op(TailExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import("erlang", "get_module_info", 2));
    }

    private int Import(string module, string function, int arity)
    {
        var key = (Intern(module), Intern(function), arity);
        imports.TryAdd(key, imports.Count);

        return imports[key];
    }

    private void MoveToX0(int tag, BigInteger value)
    {
        Op(Move, tag, value);
        Operand(XRegisterTag, 0);
    }

    private int Intern(string atom)
    {
        atoms.TryAdd(atom, atoms.Count + 1);

        return atoms[atom];
    }

    private void EmitAtoms(Stream output)
    {
        bool legacy = sourceModule?.LegacyBeamTables == true;
        int count = emittedAtoms is null ? atoms.Count : emittedAtomCount;
        Word(output, legacy ? count : -count);
        foreach (var entry in atoms.OrderBy(entry => MapEmittedAtom(entry.Value)))
        {
            if (MapEmittedAtom(entry.Value) == 0)
            {
                continue;
            }

            byte[] text = Encoding.UTF8.GetBytes(entry.Key);
            if (legacy)
            {
                output.WriteByte(checked((byte)text.Length));
            }
            else
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, text.Length);
            }
            output.Write(text);
        }
    }

    private void EmitCode(
        Stream output,
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        using var projected = hasTupleProjections ? new MemoryStream() : null;
        if (projected is not null)
        {
            WriteCodeInstructions(projected);
        }

        var stream = projected ?? instructions;
        ReadOnlyMemory<byte> code = stream.GetBuffer().AsMemory(0, checked((int)stream.Length));
        code = LowerPreparedMapDefaults(code);
        if (FoldStackAndArgumentSwaps(code) is { } swapped)
        {
            code = swapped;
        }

        if (TrimLinearStackFrames(code, nextLabel) is { } trimmed)
        {
            code = trimmed;
        }

        if (RemoveProvenRegisterMoves(code, ReadSingletonCallResults(functions, localEntries)) is { } simplified)
        {
            code = simplified;
        }

        byte[]? shared = ShareReturnBlocks(code);
        if (shared is not null)
        {
            // Expanding return jumps can expose a linear suffix to stack trimming.
            code = shared;
            if (TrimLinearStackFrames(code, nextLabel - sharedReturnLabelCount) is { } lateTrimmed)
            {
                code = lateTrimmed;
            }
        }

        int labelCount = nextLabel - sharedReturnLabelCount;
        code = FoldInitialFrameTrims(code, labelCount) ?? code;
        code = TrimStackBetweenCalls(code, labelCount) ?? code;
        code = StoreListIntoSurvivingSlot(code, labelCount) ?? code;
        code = ClearConsumedListHeadBeforeRedefinition(code, labelCount) ?? code;
        code = MoveColdBooleanCaseFailures(code, labelCount) ?? code;
        code = GroupEntryLiteralTests(code, labelCount) ?? code;
        if (GroupDisjointEntryTests(
            code,
            labelCount,
            functions,
            localEntries
        ) is { } entryPartitions)
        {
            (code, labelCount) = entryPartitions;
        }

        code = RemoveRepeatedLines(code, labelCount);
        code = SpecializeProvenListMatchFailures(code, labelCount) ?? code;
        code = ReuseLateAtomicProjectionSlots(code, labelCount);
        code = PromoteLateInterCallFrameSlots(code, labelCount);
        code = CombineComputedTupleListConstruction(code, labelCount) ?? code;
        var localResultEntries = ReadLocalResultEntries(functions, localEntries);
        code = AnnotateAdjacentNumericResults(
            code,
            labelCount,
            localResultEntries
        ) ?? code;
        code = AnnotateFiniteIntegerBounds(
            code,
            labelCount,
            functions,
            localEntries
        ) ?? code;
        code = AnnotateListProducerResults(
            code,
            labelCount,
            localResultEntries,
            recordListMappers: ReadNilTerminatedMapperEntries(functions, localEntries)
        ) ?? code;
        code = AnnotateProvenListOperands(code, labelCount, localResultEntries) ?? code;
        code = AnnotateBoundedPrivateEntryTests(code, labelCount) ?? code;
        code = LocalizeCheckedListGuardFrame(code, labelCount) ?? code;
        code = SimplifyListOrTaggedCallResult(
            code,
            labelCount,
            functions,
            localEntries
        ) ?? code;
        code = LocalizeComputedBinaryMatch(code, labelCount) ?? code;
        byte[]? ownedReceiveFrames = LocalizeGuardedReceiveFrames(
            code,
            labelCount,
            functions,
            localEntries
        );
        if (ownedReceiveFrames is not null)
        {
            code = OrderSafeEnumerationTailArguments(ownedReceiveFrames, labelCount) ?? ownedReceiveFrames;
        }
        code = AnnotateDominatedAtomComparisons(code, labelCount) ?? code;
        code = AnnotateProvenAtomEntryOperands(code, labelCount, localEntries) ?? code;
        code = OrderTypeOperandsByFirstUse(code, labelCount);
        code = TrimOwnedCallResultBranches(code, labelCount) ?? code;
        code = ClearDeadRecordCallSlots(code, labelCount) ?? code;
        code = ClearProjectedTerminalBranchRoots(code, labelCount) ?? code;
        code = OrderCallArgumentAfterKills(code, labelCount) ?? code;
        code = TrimNestedTupleCallReturns(code, labelCount) ?? code;
        code = OrderOwnedRecordCallFrameSlots(code, labelCount) ?? code;
        code = LocalizeJoinedRecordCallFrame(code, labelCount, functions) ?? code;
        code = RestoreCapturedArgumentAfterListHead(code, labelCount) ?? code;
        code = PreferCopiedTupleSourcesInX(code, labelCount) ?? code;
        code = FrameOnlyContinuingRecordBranch(code, labelCount) ?? code;
        code = ReserveHeapBeforeStackTupleProjection(code, labelCount) ?? code;
        code = CombineTerminalProjectedListHeap(code, labelCount) ?? code;
        code = ForwardTerminalCallProjections(code, labelCount) ?? code;
        code = CompactCompletelyTrimmedFrames(code, labelCount) ?? code;
        code = ShareCheckedCallResultPayload(code, labelCount) ?? code;
        code = ShareCommonCheckedTupleProjection(code, labelCount) ?? code;
        code = PackTerminalHeaderListHeap(code, labelCount) ?? code;
        code = ClearConsumedProjectedCallArguments(code, labelCount) ?? code;
        code = FoldProjectedCallArgumentSwap(code, labelCount) ?? code;
        code = TrimOwnedConstructionCallFrames(code, labelCount) ?? code;
        var mixedTupleReturns = ShareMixedTupleCaseReturns(code, labelCount);
        if (mixedTupleReturns is { } sharedMixedDecision)
        {
            (code, labelCount) = sharedMixedDecision;
            code = PreferCopiedTupleSourcesInX(code, labelCount) ?? code;
            code = OrderTypeOperandsByFirstUse(code, labelCount);
        }
        if (removedInteriorRecordFailure)
        {
            code = RebuildLineTable(code.ToArray(), labelCount);
        }

        if (RemoveProvenNonemptyListTests(code, labelCount) is { } nonemptyLists)
        {
            code = RebuildLineTable(nonemptyLists, labelCount);
        }

        code = MoveCaughtListHeadChecksBeforeFrame(code, labelCount) ?? code;
        code = ShareClosedCatchPairPayload(code, labelCount) ?? code;
        code = DirectTerminalCallResultArguments(code, labelCount) ?? code;
        code = CompactClosedCatchArgumentFrame(code, labelCount) ?? code;
        code = CompactCaughtListTailFrames(code, labelCount) ?? code;
        code = ClearClosedCatchArgumentRoots(code, labelCount) ?? code;
        if (ClearConsumedOutgoingRoots(code, labelCount) is { } outgoing)
        {
            // The late argument schedule can expose new trim sites after the
            // original frame pass has fixed the existing ones.
            code = TrimLinearStackFrames(outgoing.Code, labelCount, outgoing.Initializations) ?? outgoing.Code;
        }
        code = TrimClosedCatchPayloadReturns(code, labelCount) ?? code;
        code = FoldStagedListConstructions(code, labelCount) ?? code;
        code = FoldTerminalClosureFrames(code, labelCount) ?? code;
        code = FoldHeadTupleReturnFrames(code, labelCount) ?? code;
        code = ProjectEntryListDirectlyIntoFrame(code, labelCount) ?? code;
        code = ProjectCheckedEntryTupleIntoFrame(code, labelCount) ?? code;
        code = PlaceSingletonListCheckBeforeFrame(code, labelCount) ?? code;
        code = SinkFallthroughOwnedProjections(code, labelCount) ?? code;

        // Match the entry definitions after transformations that use the
        // unassigned handler slot to recognize the original frame shape.
        if (!ModuleOptimizationDisabled && code.Span.Contains(InitYRegisters)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } initializedEntry)
        {
            code = SimplifyFrameInitialization(code, initializedEntry.Body, includeNonAllocatingDefinitions: true) ?? code;
        }

        code = KeepTerminalRecordOperandsInX(code, labelCount) ?? code;
        code = ClearDeadProjectedBranchRoots(code, labelCount) ?? code;
        code = ScheduleNestedRecordBranchCopies(code, labelCount) ?? code;

        if (CanonicalizeCheckedIntegerOrderTests(code, labelCount) is { } integerOrder)
        {
            code = OrderTypeOperandsByFirstUse(integerOrder, labelCount);
        }

        code = OrderCheckedEntryFrameRoots(code, labelCount) ?? code;
        code = MoveColdExactMatchFailures(code, labelCount) ?? code;
        code = ReturnExactMatchPredicates(code, labelCount) ?? code;
        code = ForwardLiveCallResultToArgument(code, labelCount) ?? code;
        code = OrderTrimmedCallResultArguments(code, labelCount) ?? code;
        code = MoveColdBooleanCaseFailures(code, labelCount, standaloneOnly: true) ?? code;
        code = DelayCheckedEntryCallResultCopies(code, labelCount) ?? code;
        code = ShareCheckedCallResultSpills(code, labelCount) ?? code;
        code = ScheduleEntryTupleFunctionCall(code, labelCount) ?? code;
        code = RemoveClearedCallProjectionCopies(code, labelCount) ?? code;
        code = EliminatePreCallFrameAliases(code, labelCount) ?? code;
        code = RedirectPreCallClosureDestinations(code, labelCount) ?? code;
        code = EliminatePostCallResultAliases(code, labelCount) ?? code;
        code = RetainPostCallConstructorInputs(code, labelCount) ?? code;
        code = ScheduleCheckedRecordTupleReturns(code, labelCount) ?? code;
        code = ShareConsGuardNilReturns(code, labelCount) ?? code;
        if (OrderFinalPhysicalLabels(code, labelCount) is { } ordered)
        {
            (code, labelCount) = ordered;
        }

        code = RemoveRepeatedBinaryContextLoads(code, labelCount) ?? code;

        code = RetainNestedCheckedCallResult(code, labelCount) ?? code;
        code = ReuseRetiredCheckedCallSlot(code, labelCount) ?? code;
        code = ClearRootsBeforeSharedSavedReturns(code, labelCount) ?? code;

        code = CompactNestedRecordCallOperands(code, labelCount) ?? code;

        code = RetainCheckedTupleConstructorCarriers(code, labelCount) ?? code;
        code = ForwardOwnedBranchProjectionCopies(code, labelCount) ?? code;
        code = KeepOwnedListArithmeticInRegisters(code, labelCount) ?? code;
        code = ForwardHeapArithmeticDestination(code, labelCount) ?? code;
        code = TrimTerminalCallPayloadFrames(code, labelCount) ?? code;

        // Late root initialization can follow the last outgoing argument.
        code = OrderCallArgumentAfterKills(
            code,
            labelCount,
            finalPass: true
        ) ?? code;

        if ((ownedReceiveFrames is not null || mixedTupleReturns is not null)
            && OrderRelocatedEntryTables(code, labelCount) is { } orderedTables)
        {
            code = orderedTables;
        }
        else
        {
            code = CompactFinalAtoms(code, labelCount);
        }
        code = CoalescePartialTrimListTails(code, labelCount) ?? code;
        code = CoalescePartialTrimCallExchanges(code, labelCount) ?? code;
        code = RemoveOverwrittenHandlerSlotCopies(code, labelCount) ?? code;
        code = RemoveUnusedTryLandingCopies(code, labelCount) ?? code;
        code = RemoveTerminalTryLandingCopies(code, labelCount) ?? code;

        if (KeepCaughtResultInTailArguments(code, labelCount) is { } caughtArguments)
        {
            (code, labelCount) = caughtArguments;
        }

        code = KeepCallResultThroughCheckedProjection(code, labelCount) ?? code;
        code = OrderFinalSplitStackArguments(code, labelCount) ?? code;
        if (!ModuleOptimizationDisabled)
        {
            // The checked fields are already in X. Late branch and root scheduling
            // can now expose an atomic reuse without changing the lifted prefix.
            code = ReuseLateAtomicProjectionSlots(code, labelCount, nestedCheckedEntriesOnly: true);
        }

        code = PromoteCheckedHeapProjectionSlots(code, labelCount) ?? code;
        code = FoldPureCallArgumentCycles(code, labelCount) ?? code;
        code = RemoveDeadTupleProjections(code, labelCount) ?? code;
        code = KeepTupleCarrierAcrossTagTest(code, labelCount) ?? code;
        code = SinkIndependentTupleReadsPastHeap(code, labelCount) ?? code;
        code = ExchangeOwnedCallArgument(code, labelCount) ?? code;
        code = RetainDictionaryResultThroughCons(code, labelCount) ?? code;
        code = KeepCheckedCallResultThroughCons(code, labelCount) ?? code;
        code = EmitUnusedMapFieldPresence(code, labelCount) ?? code;
        code = AnnotateClosedEntryProducts(code, labelCount) ?? code;
        if (UseIncomingListLengthForArithmetic(code, labelCount, functions) is { } boundedHelper)
        {
            code = OrderTypeOperandsByFirstUse(boundedHelper, labelCount);
        }

        code = TrimRecursiveAppendFrames(code, labelCount, functions) ?? code;

        var tupleFilterResults = ReadLocalResultEntries(
            functions,
            localEntries,
            provenTupleFiltersOnly: true
        );
        WriteCode(
            output,
            code,
            labelCount,
            tupleFilterResults
        );
        lateProjectionDecode = null;
    }

    private void WriteCode(Stream output, ReadOnlyMemory<byte> code, int labelCount) =>
        WriteCode(
            output,
            code,
            labelCount,
            []
        );

    private void WriteCode(
        Stream output,
        ReadOnlyMemory<byte> code,
        int labelCount,
        Dictionary<int, int> tupleFilterResults
    )
    {
        if (!ModuleOptimizationDisabled && hasTupleProjections)
        {
            code = OrderAdjacentTupleProjections(code, labelCount) ?? code;
        }

        code = RefineListFallbacksAfterNilTest(code, labelCount) ?? code;
        code = EliminateGuardedEntryInitializations(code, labelCount) ?? code;
        code = BypassRejectedConsRetry(code, labelCount) ?? code;
        if (RemoveFinalLiteralAndNilRetryProjections(code, labelCount) is { } sharedRetries)
        {
            code = KeepTupleCarrierAcrossTagTest(sharedRetries, labelCount) ?? sharedRetries;
        }

        code = ReuseStableRetryTupleProjections(code, labelCount) ?? code;
        code = RemoveProjectedLeafComparisonFrame(code, labelCount) ?? code;
        code = KeepProtectedListResultInX0(code, labelCount) ?? code;
        code = KeepClosedCatchNilResultInX0(code, labelCount, tupleFilterResults) ?? code;
        code = TrimClosedCatchPayloadExits(code, labelCount) ?? code;
        code = ScheduleClosedCatchSuccessFields(code, labelCount) ?? code;
        code = HoistClosedCatchTupleFields(code, labelCount) ?? code;

        // Match beam_z's physical-order cleanup after all branch and layout edits.
        code = RemoveRepeatedCodeLocations(code, labelCount, removeCallLocations: false);

        // The sink operates on the final X-register layout. Frame and carrier
        // lowering must run before matching its branch and collector roles.
        if (!ModuleOptimizationDisabled && SsaTypeInferenceEnabled
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { Body: var body })
        {
            code = SinkCheckedTupleJoinFields(code, body) ?? code;
            if (DecodeLateAtomicProjectionSlots(code, labelCount) is { Body: var current })
            {
                if (ShareCheckedEntryRecordFields(code, current) is { } sharedFields)
                {
                    code = sharedFields;
                    if (DecodeLateAtomicProjectionSlots(code, labelCount) is { } shared)
                    {
                        code = OrderSharedCheckedRecordCallFrames(
                            code,
                            shared.Body,
                            shared.StackOperands
                        ) ?? code;
                    }
                }
            }
        }

        code = PlaceTaggedEntryTailBeforeTryFrame(code, labelCount) ?? code;
        code = RewriteOwnedMapIteratorBranches(code, labelCount) ?? code;
        code = CoalesceFinalMapScratchProjections(code, labelCount) ?? code;
        code = RemoveDominatedMapProjectionTests(code, labelCount) ?? code;
        code = PlaceListHeadArityBranchFrames(code, labelCount) ?? code;
        code = DeferCheckedListHeadConstruction(code, labelCount) ?? code;
        code = FuseTerminalLiteralMapBranches(code, labelCount) ?? code;
        code = OutlineClosedMapCaseFailures(code, labelCount) ?? code;
        code = AllocateClosedMapJoinRegisters(code, labelCount) ?? code;
        code = CompactClosedCatchEntryInputs(code, labelCount) ?? code;
        code = PlaceCheckedCatchArguments(code, labelCount) ?? code;
        code = ShareAritySelectedTupleFields(code, labelCount) ?? code;
        code = ReuseSharedArityPayloadOnRetry(code, labelCount) ?? code;
        code = OrderSharedArityDispatchRegisters(code, labelCount) ?? code;
        if (SelectCheckedListHeadTags(code, labelCount) is { } selectedListHead)
        {
            (code, labelCount) = selectedListHead;
        }

        // Late root cleanup can expose the complete projected argument cycle.
        code = FoldProjectedCallArgumentSwap(code, labelCount) ?? code;
        code = ForwardCheckedRecordUpdatePair(code, labelCount) ?? code;
        code = CompactCheckedRecordPairFrame(code, labelCount) ?? code;
        code = PlaceZeroFramesOnCallingPaths(code, labelCount) ?? code;
        code = TrimUnusedOwnedFrameSuffixes(code, labelCount) ?? code;
        code = RemoveUnchangedCallSpills(code, labelCount) ?? code;
        code = PlaceReturnedTupleCaptureBeforeCheck(code, labelCount) ?? code;
        if (PlaceInnerCheckedReturnBeforeOuterExits(code, labelCount) is { } checkedReturn)
        {
            (code, labelCount) = checkedReturn;
        }

        code = ProjectAliasedTupleGuardElements(code, labelCount) ?? code;
        code = OrderDisjointNilAndIntegerEntries(code, labelCount) ?? code;
        code = ReorderFinalLineLocations(code, labelCount) ?? code;
        code = ReuseCheckedTupleAfterFieldEffect(code, labelCount) ?? code;
        code = ForwardNestedRecordUpdateRelay(code, labelCount) ?? code;
        code = CompactLiteralRecordUpdateLeaf(code, labelCount) ?? code;
        code = CompactCheckedNestedRecordLeaf(code, labelCount) ?? code;
        code = CompactFramesBesideSiblingTrims(code, labelCount) ?? code;
        code = RemoveGuardProvenLiteralMoves(code, labelCount) ?? code;
        code = OrderArgumentsWithRetainedCallResult(code, labelCount) ?? code;
        code = ForwardTerminalJoinArgument(code, labelCount) ?? code;
        code = ReuseDisplacedBranchCallArgument(code, labelCount) ?? code;
        code = ReuseBranchCollectingResultSlot(code, labelCount) ?? code;
        code = ReuseCallResultSlotForSafeBifArgument(code, labelCount) ?? code;
        code = ForwardClearedCallResultToUnaryBif(code, labelCount) ?? code;
        code = ReuseRetiredPairCallArgument(code, labelCount) ?? code;
        code = ReuseCheckedListPrefixOnRetry(code, labelCount) ?? code;
        if (SelectCheckedListDelimiterPrefixes(code, labelCount) is { } delimiterSelection)
        {
            (code, labelCount) = delimiterSelection;
        }

        code = ShareCheckedTaggedTupleGuardRetries(code, labelCount) ?? code;
        // Removing the repeated tag check can leave one qualifying literal-failure edge.
        code = ReuseInterleavedRetryTupleProjections(code, labelCount) ?? code;
        code = ShareCheckedNestedTupleRetryPrefix(code, labelCount) ?? code;
        if (SelectClosedTaggedAtomGuards(code, labelCount) is { } groupedTaggedGuards)
        {
            (code, labelCount) = groupedTaggedGuards;
        }

        code = ClearConsumedTaggedCallArguments(code, labelCount) ?? code;
        code = ReuseConsumedTupleCarrierInCallContinuation(code, labelCount) ?? code;
        code = PlaceReturnedStatusCaptureBeforeCheck(code, labelCount) ?? code;
        code = TrimTerminalStatusCleanup(code, labelCount) ?? code;
        code = MergeAdjacentFrameInitializations(code, labelCount) ?? code;
        code = EliminateOverwrittenProjectionInitializations(code, labelCount) ?? code;
        code = ProjectEntryListDirectlyIntoFrame(
            code,
            labelCount,
            repeatedInitializationOnly: true
        ) ?? code;
        code = ReuseClearedTokenFrameSlot(code, labelCount) ?? code;
        code = ReuseProjectedEntryCallArgument(code, labelCount) ?? code;
        code = PrioritizeCheckedConsResult(code, labelCount) ?? code;
        code = DelayEntryFieldArgument(code, labelCount) ?? code;
        if (PlaceListRetryAfterCheckedCall(code, labelCount) is { } placedListRetry)
        {
            (code, labelCount) = placedListRetry;
        }

        code = RemoveOverwrittenFrameMoves(code, labelCount) ?? code;
        code = ForwardOutgoingClosureRegisters(code, labelCount) ?? code;
        code = ClearClosedStringArmCallRoot(code, labelCount) ?? code;
        code = PlaceCheckedCallProjectionAfterFrame(code, labelCount) ?? code;
        code = KeepReturnedTupleInputInX(code, labelCount) ?? code;
        code = DeferRetainedListField(code, labelCount) ?? code;
        code = DeferConstructionFieldPastCalls(code, labelCount) ?? code;
        code = KeepCheckedListTailInCallInput(code, labelCount) ?? code;
        code = CoalesceSplitConstructorHeaps(code, labelCount) ?? code;
        code = ForwardCheckedPayloadCallArgument(code, labelCount) ?? code;
        code = ForwardProjectedPairCallArguments(code, labelCount) ?? code;
        code = KeepTerminalPairAcrossHeap(code, labelCount) ?? code;
        code = SinkOwnedTupleArgumentFields(code, labelCount) ?? code;
        code = ForwardTerminalConsRecordUpdates(code, labelCount) ?? code;
        code = TrimConsumedTerminalConstructorArgument(code, labelCount) ?? code;
        code = EndFramedEmptyStackThrow(code, labelCount) ?? code;
        code = RemoveProvenLocalPayloadTupleChecks(code, labelCount) ?? code;
        code = MoveColdBooleanCaseFailures(code, labelCount, unannotatedOnly: true) ?? code;
        if (DeferAlternativeTupleArmFrame(code, labelCount) is { } alternativeTupleFrame)
        {
            (code, labelCount) = alternativeTupleFrame;
        }

        code = CoalesceRetiredRecordTailSlots(code, labelCount) ?? code;
        code = DeferCheckedRecordTailArguments(code, labelCount) ?? code;
        code = ReallocateGuardedReturningTailFrames(code, labelCount) ?? code;
        code = RemoveUnusedClosureFrameSlots(code, labelCount) ?? code;
        code = CoalesceCheckedMapTupleFields(code, labelCount) ?? code;
        code = FinishCheckedEqualityProjection(code, labelCount) ?? code;
        code = ReuseTerminalCheckedTupleField(code, labelCount) ?? code;
        code = ReuseCheckedRetryArityCarrier(code, labelCount) ?? code;
        code = CombineProjectedDictionaryHeapAllocation(code, labelCount) ?? code;
        code = ScheduleProjectedListAfterCall(code, labelCount) ?? code;
        code = OrderDictionaryListArguments(code, labelCount) ?? code;
        code = ClearCallSlotRoots(code, labelCount, replacedUnaryOnly: true) ?? code;
        code = ClearProjectedArgumentCallSlotRoots(code, labelCount) ?? code;
        code = BuildRetainedResultSendTuple(code, labelCount) ?? code;
        code = RemoveOwnedSendMessageSlot(code, labelCount) ?? code;
        code = BuildTupleInProtectedSendArgument(code, labelCount) ?? code;
        if (PlaceColdCheckedCaseExit(code, labelCount) is { } coldCheckedExit)
        {
            (code, labelCount) = coldCheckedExit;
        }

        if (PartitionKnownKeyFindReturns(code, labelCount) is { } keyFindPartition)
        {
            (code, labelCount) = keyFindPartition;
        }

        code = SwapClearedStackCallArguments(code, labelCount) ?? code;
        code = FoldTransientArgumentFrameTrims(code, labelCount) ?? code;
        code = ScheduleBoundPairConsTail(code, labelCount) ?? code;
        code = OrderRetainedCheckedEntryFrameSlots(code, labelCount) ?? code;
        code = ScheduleStackFieldBeforeListConstruction(code, labelCount) ?? code;
        code = ScheduleSiblingListProjections(code, labelCount) ?? code;
        code = OrderClosedCatchResultDispatch(code, labelCount) ?? code;
        code = ReuseCheckedTupleLiteralScratch(code, labelCount) ?? code;
        code = EliminateConsumedPreCallRoots(code, labelCount) ?? code;
        code = PromoteTerminalConstructionTemps(code, labelCount) ?? code;
        code = CompactLateUnusedInteriorSlots(code, labelCount) ?? code;
        code = CompactRetainedTrimSiblingFrames(code, labelCount) ?? code;
        code = OrderFinalCheckedPayloadStackArguments(code, labelCount) ?? code;
        code = ReuseRetiredProjectionForStatusCleanup(code, labelCount) ?? code;
        code = RemoveRepeatedConstructionClears(code, labelCount) ?? code;
        code = CompactPreparedMapDefaultLabels(code, ref labelCount);
        code = FoldExhaustiveComparisonBranches(code, ref labelCount);
        code = PlacePositionalArithmeticBeforeCallFrame(code, labelCount) ?? code;
        code = ScheduleTwoResultFrameTrims(code, labelCount) ?? code;
        if (FinalizeCheckedPairCallReturns(code, labelCount) is { } checkedPairReturn)
        {
            code = OrderTypeOperandsByFirstUse(checkedPairReturn, labelCount);
        }

        code = FinalizeAcknowledgedPairReturns(code, ref labelCount);
        code = ScheduleRetainedListClosures(code, labelCount) ?? code;
        code = SplitLiteralGuardedTryFrames(code, labelCount) ?? code;
        code = ReuseTerminalProjectionCarrier(code, labelCount) ?? code;
        code = ScheduleTerminalPairBeforeListConstruction(code, labelCount) ?? code;
        code = ReuseTaggedTailProjectionCarrier(code, labelCount) ?? code;
        code = ScheduleCheckedPairListArguments(code, labelCount) ?? code;
        code = RematerializeEntryLiteralFrames(code, labelCount) ?? code;
        if (ShareClosedShortcutFallback(code, labelCount) is { } shortcutFallback)
        {
            (code, labelCount) = shortcutFallback;
        }

        code = KeepJoinedResultThroughCons(code, labelCount) ?? code;
        code = CompactLoadEntryTrimSiblingFrame(code, labelCount) ?? code;
        if (PlaceColdProtectedProjectionFailures(code, labelCount) is { } protectedProjectionFailures)
        {
            (code, labelCount) = protectedProjectionFailures;
        }

        code = RemoveRepeatedFallthroughFrameWrites(code, labelCount) ?? code;
        code = ClearProtectedSendArgumentRoots(code, labelCount) ?? code;
        code = CompactDisjointTrySlotLifetimes(code, labelCount) ?? code;
        code = AnnotateSavedConsRecordFields(code, labelCount) ?? code;
        code = ScheduleDeclaredNativeFrameEntry(code, labelCount) ?? code;
        code = ClearRetiredConsSlotBeforeCall(code, labelCount) ?? code;
        code = OrderLiteralJoinCopies(code, labelCount) ?? code;
        code = OrderCheckedNestedTupleReads(code, labelCount) ?? code;
        code = ScheduleBoundedTailCounter(code, labelCount) ?? code;
        if (PlaceNestedIdentityCaseExits(code, labelCount) is { } nestedIdentityExits)
        {
            (code, labelCount) = nestedIdentityExits;
        }

        code = ScheduleTrimmedClosureCallArguments(code, labelCount) ?? code;
        if (TypeOwnedListCallback(code, labelCount) is { } typedCallback)
        {
            code = OrderTypeOperandsByFirstUse(typedCallback, labelCount);
        }

        code = LocalizeCheckedLiteralReturnFrame(code, labelCount) ?? code;
        code = ScheduleSavedTupleCallbackArguments(code, labelCount) ?? code;
        if (OrderRetainedNestedStatusChecks(code, labelCount) is { } retainedStatus)
        {
            (code, labelCount) = retainedStatus;
        }

        if (KeepNonemptyStatusRetryInFallthrough(code, labelCount) is { } orderedRetry)
        {
            (code, labelCount) = orderedRetry;
        }

        code = ForwardInitializedCallArgument(code, labelCount) ?? code;
        code = ReleaseReusedListCallSlot(code, labelCount) ?? code;
        code = RemoveOwnedCallProjectionSlot(code, labelCount) ?? code;
        code = ReuseFinalSavedEqualityRetryProjection(code, labelCount) ?? code;
        code = CompactDeadSlotsThroughOwnedTrim(code, labelCount) ?? code;
        code = RemoveCoveredCallFrameInitializations(code, labelCount) ?? code;
        code = PartitionLateSavedEffectfulTupleReturns(code, labelCount) ?? code;
        code = DeferTaggedRecursiveConsHead(code, labelCount) ?? code;
        code = DeferSavedTaggedRecursiveConsHeads(code, labelCount) ?? code;
        code = ForwardTerminalConstructionScratch(code, labelCount) ?? code;
        code = OrderEntryCallbackOperandFrame(code, labelCount) ?? code;
        code = ReuseRetiredInputForCallResult(code, labelCount) ?? code;
        code = ReuseRetiredClosureInputForCallResult(code, labelCount) ?? code;
        code = ScheduleCheckedConsChildCapture(code, labelCount) ?? code;
        code = RematerializeOwnedLiteralSlot(code, labelCount) ?? code;
        code = RematerializePostTrimLiteralSlot(code, labelCount) ?? code;
        code = OrderSoleRetainedClosureFrame(code, labelCount) ?? code;
        code = AdvanceClearedOwnedPrefixTrim(code, labelCount) ?? code;
        code = TrimOwnedCheckedRecordCallFrames(code, labelCount) ?? code;
        code = ScheduleCheckedParentTupleFields(code, labelCount) ?? code;

        if (RetainRetryStatusInFinalSlot(code, labelCount) is { } joinedRetryStatus)
        {
            (code, labelCount) = joinedRetryStatus;
        }

        code = TrimTerminalCallbackCons(code, labelCount) ?? code;
        code = OrderAritySelectedFrameRootCopies(code, labelCount) ?? code;
        code = AnnotateRepeatedNumericInputs(code, labelCount) ?? code;
        code = SplitOwnedConsDecisionFrames(code, labelCount) ?? code;
        code = ForwardRetiredRecordConsCopy(code, labelCount) ?? code;
        code = FoldUnaryTailListScratch(code, labelCount) ?? code;
        code = OrderTerminalListRetryBlocks(code, labelCount) ?? code;
        code = TrimSingleSlotAfterFinalCallSetup(code, labelCount) ?? code;
        code = ForwardOverwrittenReceiverCopies(code, labelCount) ?? code;
        code = ForwardWrappedHeapArithmeticDestination(code, labelCount) ?? code;
        code = ScheduleRepeatedArithmeticEffectFrame(code, labelCount) ?? code;
        code = KeepWrappedArithmeticPayloadInX0(code, labelCount) ?? code;
        code = ShareCheckedTokenRetryPrefix(code, labelCount) ?? code;
        if (SelectCheckedListRetryArities(code, labelCount) is { } arityRetry)
        {
            (code, labelCount) = arityRetry;
        }

        code = ForwardJoinedLiteralCallArguments(code, labelCount) ?? code;
        code = ForwardClosureResultToCheckedArguments(code, labelCount) ?? code;
        code = SinkCheckedTupleFieldAcrossAllocation(code, labelCount) ?? code;
        code = ReserveComparedFieldSlot(code, labelCount) ?? code;
        code = AnnotateCheckedIntegerOrderInputs(code, labelCount) ?? code;
        code = DelayCheckedEntryCarrierFrames(code, labelCount) ?? code;
        if (OrderFinalPhysicalLabelsCore(
            code,
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true,
            shareAdjacentCopyReturns: true
        ) is { } copyReturns)
        {
            (code, labelCount) = copyReturns;
        }

        code = SinkCheckedRecordConstructionFields(code, labelCount) ?? code;
        code = AnnotateLocalAtomCallEquality(code, labelCount) ?? code;
        code = AnnotateLocalTuplePairResults(code, labelCount) ?? code;
        code = SinkSingleOwnedRecordConstructionField(code, labelCount) ?? code;
        code = ScheduleSiblingListProjectionGroup(code, labelCount) ?? code;
        code = ForwardFreshConsRecordBuilders(code, labelCount) ?? code;
        code = DelayFramePastNilTailCall(code, labelCount) ?? code;
        code = DeferOwnedTerminalRecordTail(code, labelCount) ?? code;
        code = AnnotateOriginalAtomGuardInputs(code, labelCount) ?? code;

        // Late stack compaction can leave the mapper result in X0 for the record update.
        code = AnnotateFinalListRecordResults(code, labelCount) ?? code;
        code = ForwardNestedRecordUpdateRelay(code, labelCount, joinedOnly: true) ?? code;
        code = KeepTerminalUpdatedRecordInX(code, labelCount) ?? code;
        code = HoistCheckedNestedRecordAllocations(code, labelCount) ?? code;
        code = ScheduleRetiredCallArgumentRoots(code, labelCount) ?? code;
        code = CompactTransientOutgoingSlots(code, labelCount) ?? code;
        code = ForwardRetiredOutgoingProjections(code, labelCount) ?? code;
        code = OrderFinalAssemblyTables(code, labelCount);
        Word(output, CodeHeaderSize);
        Word(output, BeamWire.FormatVersion);
        Word(output, highestOpcode);
        Word(output, labelCount);
        Word(output, exports.Count + locals.Count);
        output.Write(code.Span);
    }

    private void EmitFunctionTable(Stream output, List<(int Atom, int Arity, int Entry)> entries)
    {
        Word(output, entries.Count);
        foreach (var (atom, arity, entry) in entries.AsEnumerable().Reverse())
        {
            Word(output, MapEmittedAtom(atom));
            Word(output, arity);
            Word(output, MapEmittedLabel(entry));
        }
    }

    private void EmitLines(Stream output)
    {
        Word(output, 0); // Line table version.
        Word(output, 0); // No executable line counters.
        Word(output, lineInstructions);
        Word(output, lines.Count);
        Word(output, filenames.Count);
        int fileIndex = 0;
        foreach (var entry in lines.OrderBy(entry => entry.Value))
        {
            int nextFile = entry.Key.File is null ? 0 : filenames[entry.Key.File];
            if (nextFile != fileIndex)
            {
                BeamCompactWriter.Write(output, AtomTag, nextFile);
                fileIndex = nextFile;
            }
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, entry.Key.Line);
        }

        Span<byte> length = stackalloc byte[sizeof(ushort)];
        foreach (var file in filenames.OrderBy(entry => entry.Value))
        {
            byte[] text = Encoding.UTF8.GetBytes(file.Key);
            BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)text.Length));
            output.Write(length);
            output.Write(text);
        }
    }

    private BeamSourceLocation lastEmittedLine;

    private void EmitLine(BeamSourceLocation location)
    {
        if (location.Line != 0 && location.File is null && defaultLineFile is not null)
        {
            location = location with { File = defaultLineFile };
        }

        lastEmittedLine = location;
        lineInstructions++;
        Op(Line, BeamWire.UnsignedTag, IndexLineLocation(location));
    }

    private int IndexLineLocation(BeamSourceLocation location)
    {
        if (location.Line == 0)
        {
            return 0;
        }

        // OTP reserves filename index zero for the module's unqualified source name.
        var entry = location.File == implicitLineFile ? location with { File = null } : location;
        if (entry.File is not null)
        {
            filenames.TryAdd(entry.File, filenames.Count + 1);
        }

        lines.TryAdd(entry, lines.Count + 1);

        return lines[entry];
    }

    private void Op(byte opcode, int tag, BigInteger value)
    {
        hasTupleProjections |= opcode == GetTupleElement;
        highestOpcode = Math.Max(highestOpcode, opcode);
        instructions.WriteByte(opcode);
        Operand(tag, value);
    }

    private Dictionary<int, int>? receiveScratchRegisters;

    private void Operand(int tag, BigInteger value)
    {
        if (tag == BeamWire.IntegerTag)
        {
            WriteIntegerOperand(instructions, value);

            return;
        }

        if (tag == LabelTag && value < 0)
        {
            deferredLabelOperands.Add((checked((int)instructions.Position), (int)value));
            value = 0;
        }
        if (tag == YRegisterTag && receiveScratchRegisters?.TryGetValue((int)value, out int register) == true)
        {
            tag = XRegisterTag;
            value = register;
        }
        BeamCompactWriter.Write(instructions, tag, value);
    }

    private static byte[] Payload(Action<Stream> write)
    {
        using var output = new MemoryStream();
        write(output);

        return output.ToArray();
    }
}
