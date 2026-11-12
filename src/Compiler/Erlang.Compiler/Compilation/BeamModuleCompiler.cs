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
    bool maybeEnabled = true
)
{
    private string? OnLoadFunction => sourceModule?.OnLoadFunction;

    private readonly Dictionary<string, int> atoms = new(StringComparer.Ordinal) { [name] = 1 };
    private readonly List<(int Atom, int Arity, int Entry)> exports = [];
    private readonly List<(int Atom, int Arity, int Entry)> locals = [];
    private readonly Dictionary<(int Module, int Function, int Arity), int> imports = [];
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
        functions = functions.Select(FoldLiteralAtomConversions).ToArray();
        (functions, sourceLines) = ExtractClosedComprehensions(functions, sourceLines);
        using var code = instructions;
        var localEntries = new Dictionary<(string Name, int Arity), int>();
        var reachable = new HashSet<(string Name, int Arity)>();
        var byName = functions.ToDictionary(function => (function.Name, function.Arity));
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

        var wxMappingBoundaries = new Dictionary<int, List<int>>();
        for (int index = 0; index < functions.Count; index++)
        {
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
                    if (!wxMappingBoundaries.TryGetValue(closures.Count, out var group))
                    {
                        group = [];
                        wxMappingBoundaries.Add(closures.Count, group);
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
                EmitCapturedListFilter(filter, localEntries[(function.Name, function.Arity)], localEntries, line);
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
                new GeneralEmitter(this, localEntries, byName, line, plan.Slots, plan.Labels, plan.ListFailures, plannedTupleFrames: plan.TupleFrames)
                    .Function(function);
            }
            else
            {
                try
                {
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
                EmitColumnMappingHelper(helper, columns, location, localEntries[(helper, 1)]);
            }
            else if (map is PairConsMapping pairs)
            {
                EmitPairConsHelper(helper, pairs, location, localEntries[(helper, 1)]);
            }
            else
            {
                EmitMappingHelper(helper, (ListMapping)map, location, localEntries);
            }
        }
        foreach (var (helper, mapping, location) in tupleMappingHelpers)
        {
            EmitTupleMappingHelper(helper, mapping, location, localEntries[(helper, 2)]);
        }
        foreach (var (helper, flow, location) in discardedHelpers)
        {
            EmitDiscardedListHelper(helper, flow, location, localEntries);
        }
        foreach (var (helper, flow, location) in capturedTableHelpers)
        {
            EmitCapturedTableHelper(helper, flow, location, localEntries[(helper, 2)]);
        }

        foreach (var (helper, flow, location) in pageHelpers)
        {
            EmitPageComprehensionHelper(helper, flow, location, localEntries);
        }

        var guardFailures = new Dictionary<string, int>(StringComparer.Ordinal);
        void EmitDeferredProjection(int index)
        {
            var function = functions[index];
            var line = sourceLines[index];
            StartFunction(function.Name, function.Arity, line, exported: false);
            deferredEntries.Add(-index, nextLabel - 1);
            var plan = general[(function.Name, function.Arity)];
            new GeneralEmitter(this, localEntries, byName, line, plan.Slots, plan.Labels, plan.ListFailures, plannedTupleFrames: plan.TupleFrames).Function(function);
        }

        void EmitWxMappingsAtBoundary(int boundary)
        {
            if (!wxMappingBoundaries.TryGetValue(boundary, out var group))
            {
                return;
            }

            for (int index = group.Count - 1; index >= 0; index--)
            {
                EmitDeferredProjection(group[index]);
            }
        }

        EmitLiftedClosures(localEntries, byName, guardFailures, emitAtBoundary: EmitWxMappingsAtBoundary);
        int emittedClosures = closures.Count;
        for (int index = declaredCount; index < functions.Count; index++)
        {
            if (deferredProjections.Contains(functions[index].Name) && !deferredEntries.ContainsKey(-index))
            {
                EmitDeferredProjection(index);
            }
        }
        EmitLiftedClosures(localEntries, byName, guardFailures, emittedClosures);
        EmitLiftedGuardFailures(guardFailures);
        if (callbackInfo is not null)
        {
            EmitBehaviourInfoFailure();
        }
        ResolveDeferredLabelOperands();
        code.WriteByte(EndCode);
        (string Id, byte[] Payload)[] essentials =
        [
            (BeamWire.AtomsChunk, Payload(EmitAtoms)),
            (BeamWire.CodeChunk, Payload(EmitCode)),
            ("StrT", stringTable.ToArray()),
            (BeamWire.ImportsChunk, Payload(output =>
            {
                Word(output, imports.Count);
                foreach (var entry in imports.OrderBy(entry => entry.Value))
                {
                    Word(output, entry.Key.Module);
                    Word(output, entry.Key.Function);
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

    private static Dictionary<string, Term>? ReadConstantBindings(Clause clause, Expr.Sequence sequence)
    {
        var bindings = new Dictionary<string, Term>(StringComparer.Ordinal);
        var parameters = clause.Patterns.OfType<Pattern.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
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
        Word(output, legacy ? atoms.Count : -atoms.Count);
        foreach (var entry in atoms.OrderBy(entry => entry.Value))
        {
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

    private void EmitCode(Stream output)
    {
        using var projected = hasTupleProjections ? new MemoryStream() : null;
        if (projected is not null)
        {
            WriteCodeInstructions(projected);
        }

        var stream = projected ?? instructions;
        ReadOnlyMemory<byte> code = stream.GetBuffer().AsMemory(0, checked((int)stream.Length));
        byte[]? shared = ShareReturnBlocks(code);
        Word(output, CodeHeaderSize);
        Word(output, BeamWire.FormatVersion);
        Word(output, highestOpcode);
        Word(output, nextLabel - sharedReturnLabelCount);
        Word(output, exports.Count + locals.Count);
        output.Write(shared is null ? code.Span : shared.AsSpan());
    }

    private void EmitFunctionTable(Stream output, List<(int Atom, int Arity, int Entry)> entries)
    {
        Word(output, entries.Count);
        foreach (var (atom, arity, entry) in entries.AsEnumerable().Reverse())
        {
            Word(output, atom);
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
        int index = 0;
        if (location.Line != 0)
        {
            if (location.File is null && defaultLineFile is not null)
            {
                location = location with { File = defaultLineFile };
            }
            if (location.File is not null)
            {
                filenames.TryAdd(location.File, filenames.Count + 1);
            }
            lines.TryAdd(location, lines.Count + 1);
            index = lines[location];
        }
        lastEmittedLine = location;
        lineInstructions++;
        Op(Line, BeamWire.UnsignedTag, index);
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
