using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    internal static bool HasSupportedClauses(FunctionDefinition function)
    {
        if (ReadOctetMapping(function.Clauses) is not null || ReadTaggedEncoding(function.Clauses) is not null
            || ReadTaggedListJoin(function.Clauses) is not null || ReadTupleListFormatting(function.Clauses) is not null)
        {
            return true;
        }
        if (ReadTagDecoding(function.Clauses) is not null)
        {
            return true;
        }
        if (ReadFieldEncoding(function.Clauses) is not null || ReadFieldDecoding(function.Clauses) is not null)
        {
            return true;
        }
        if (ReadChoiceSearch(function.Clauses) is not null || ReadChoiceAcceptance(function.Clauses) is not null)
        {
            return true;
        }
        if (ReadBerTag(function.Clauses) is not null || ReadBerContinuation(function.Clauses) is not null)
        {
            return true;
        }
        if (ReadTaggedChoice(function.Clauses) is not null || ReadTaggedField(function.Clauses) is not null
            || function.Clauses is [var single]
                && (ReadIdentityGuard(single) is not null || ReadBinaryCapture(single) is not null || ReadSignedWriter(single) is not null))
        {
            return true;
        }
        if (IsFiniteLiteralDispatch(function.Clauses))
        {
            return HasLiteralDispatchKeys(function.Clauses);
        }
        if (function.Clauses is [{ Guard: null } clause])
        {
            return HasUnconditionalParameters(clause) || HasNilParameter(clause);
        }
        if (ReadPreservedCallCase(function.Clauses) is not null)
        {
            return true;
        }
        if (function.Arity != 1 || function.Clauses[^1] is not { Guard: null, Patterns: [Pattern.Variable] } fallback)
        {
            return false;
        }
        bool constantFallback = AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term fallbackValue);
        if (!constantFallback && !TryReadTupleError(fallback, out _, out _) && ReadArgumentListError(fallback) is null)
        {
            return false;
        }
        if (constantFallback && fallback.Body is Expr.Literal
            && function.Clauses.Count == 2 && TryDeconstruction(function.Clauses[0], out _))
        {
            return true;
        }
        return HasLiteralDispatchKeys(function.Clauses.SkipLast(1), constantFallback ? fallbackValue : null);
    }

    private static bool IsFiniteLiteralDispatch(IReadOnlyList<Clause> clauses) =>
        clauses[^1].Patterns is [Pattern.Literal { Value: Atom or Integer }];

    private static bool HasLiteralDispatchKeys(IEnumerable<Clause> clauses, Term? fallbackValue = null)
    {
        var keys = new HashSet<Term>();
        foreach (var branch in clauses)
        {
            if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom or Integer } key] }
                || !AttributeLiteral.TryNormalizeConstant(branch.Body, out Term result)
                || !keys.Add(key.Value) || result.Equals(fallbackValue))
            {
                return false;
            }
        }

        return keys.All(key => key is Atom) || keys.All(key => key is Integer);
    }

    private static Expr.Call? FindCall(Expr body) => body switch
    {
        Expr.Call call => call,
        Expr.Sequence { Items: [Expr.Call call, _] } => call,
        _ => null
    };

    private void EmitFunctionBody(
        IReadOnlyList<Clause> clauses,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        int selectorType = 0,
        FiniteAtomLiteralDispatch? finiteAtomDispatch = null
    )
    {
        Clause clause = clauses[0];
        if (clauses.Count == 1 && (TryEmitSelfArgumentList(clause, location) || TryEmitPairArgumentList(clause, location)
            || TryEmitLoggedCallReply(clause, location) || TryEmitComputedPairArgumentList(clause, location)
            || TryEmitNestedTupleArgument(clause, location)))
        {
            return;
        }
        if (ReadAllocatorRows(clauses) is { } allocator)
        {
            EmitAllocatorRows(allocator, location, entries);

            return;
        }
        if (ReadColumnMapping(clauses) is { } columns)
        {
            EmitColumnMapping(columns, location, entries);

            return;
        }
        if (ReadSplitListJoin(clauses) is { } splitJoin)
        {
            EmitSplitListJoin(splitJoin, location, entries);

            return;
        }
        if (ReadTableProjection(clauses) is { } table)
        {
            EmitTableProjection(table, location, entries);

            return;
        }
        if (clauses.Count == 1 && TryEmitComputedListField(clause, location, entries))
        {
            return;
        }
        if (TryEmitDictionaryCall(clauses, location, entries))
        {
            return;
        }

        if (ReadOctetMapping(clauses) is { } octets)
        {
            EmitOctetMapping(octets, location, entries);

            return;
        }
        if (ReadTaggedEncoding(clauses) is { } encodeTags)
        {
            EmitTaggedEncoding(encodeTags, location, entries);

            return;
        }
        if (ReadTaggedListJoin(clauses) is { } join)
        {
            EmitTaggedListJoin(join, location, entries);

            return;
        }
        if (ReadTupleListFormatting(clauses) is { } formatting)
        {
            EmitTupleListFormatting(formatting, location);

            return;
        }
        if (ReadTagDecoding(clauses) is { } decodeTags)
        {
            EmitTagDecoding(decodeTags, location, entries);

            return;
        }
        if (ReadFieldDecoding(clauses) is { } decodeFields)
        {
            EmitFieldDecoding(decodeFields, location, entries);

            return;
        }
        if (ReadFieldEncoding(clauses) is { } fields)
        {
            EmitFieldEncoding(fields, location, entries);

            return;
        }
        if (ReadChoiceSearch(clauses) is { } search)
        {
            EmitChoiceSearch(search, location, entries);

            return;
        }
        if (ReadChoiceAcceptance(clauses) is { } acceptance)
        {
            EmitChoiceAcceptance(acceptance, location, entries);

            return;
        }
        if (ReadBerTag(clauses) is { } berTag)
        {
            EmitBerTag(berTag, location, entries);

            return;
        }
        if (ReadBerContinuation(clauses) is { } berContinuation)
        {
            EmitBerContinuation(berContinuation, location, entries);

            return;
        }
        if (ReadTaggedChoice(clauses) is { } taggedChoice)
        {
            EmitTaggedChoice(taggedChoice, location, entries);

            return;
        }
        if (ReadTaggedField(clauses) is { } taggedField)
        {
            EmitTaggedField(taggedField, location, entries);

            return;
        }
        if (clauses.Count == 1 && ReadSignedWriter(clause) is { } writer)
        {
            EmitSignedWriter(writer, location, entries);

            return;
        }
        if (clauses.Count == 1 && ReadIdentityGuard(clause) is byte guard)
        {
            Op(guard, LabelTag, nextLabel - 2);
            Operand(XRegisterTag, 0);
            instructions.WriteByte(Return);

            return;
        }
        if (clauses.Count == 1 && ReadBinaryCapture(clause) is { } binary)
        {
            EmitBinaryCapture(binary);

            return;
        }
        if (finiteAtomDispatch is not null)
        {
            EmitFiniteAtomLiteralDispatch(finiteAtomDispatch);

            return;
        }

        if (IsFiniteLiteralDispatch(clauses))
        {
            int functionInfo = nextLabel - 2;
            if (clauses.Count == 1)
            {
                EmitLiteralTest(clause, functionInfo);
                EmitBody(clause);
                instructions.WriteByte(Return);
            }
            else
            {
                EmitLiteralDispatch(
                    clauses,
                    location,
                    functionInfo,
                    selectorType: selectorType
                );
            }

            return;
        }
        if (HasNilParameter(clause))
        {
            Op(IsNil, LabelTag, nextLabel - 2);
            Operand(XRegisterTag, 0);
            if (clauses.Count == 1 && (TryEmitStackedTupleCalls(clause, location) || TryEmitCallListResult(clause, location, entries)))
            {
                return;
            }
        }
        if (clauses.Count == 2)
        {
            if (ReadPreservedCallCase(clauses) is { } preserved)
            {
                EmitPreservedCallCase(preserved, location);
            }
            else if (clauses[1].Body is Expr.Literal && TryDeconstruction(clause, out var deconstruction))
            {
                EmitDeconstruction(deconstruction, clauses[1]);
            }
            else
            {
                EmitLiteralPattern(clause, clauses[1], location);
            }
            return;
        }
        if (clauses.Count > 2)
        {
            EmitLiteralDispatch(clauses, location, selectorType: selectorType);

            return;
        }
        if (ReadWrappedCallCase(clause) is { } wrapped)
        {
            EmitWrappedCallCase(wrapped, clause, location, entries);
            return;
        }
        if (ReadCallCase(clause) is { } callCase)
        {
            EmitCallCase(callCase, location);
            return;
        }
        if (ReadSignedInteger(clause) is { } signed)
        {
            EmitSignedInteger(signed, location, entries);

            return;
        }
        if (ReadSignedWidth(clause) is { } signedWidth)
        {
            EmitSignedWidth(signedWidth, location, entries);

            return;
        }
        if (ReadSizedCall(clause) is { } sizedCall)
        {
            EmitSizedCall(sizedCall, location, entries);

            return;
        }
        if (ReadProtectedCall(clause) is { } protectedCall)
        {
            EmitProtectedCall(protectedCall, location, entries);

            return;
        }
        if (ReadLocalBinding(clause) is { } binding)
        {
            EmitLocalBinding(binding, location, entries);

            return;
        }
        if (ReadCallChain(clause) is { } chain)
        {
            EmitCallChain(chain, location, entries);
            return;
        }
        if ((ReadExternalFunctionLiteral(clause.Body) ?? ReadAutoImportedFunctionLiteral(clause.Body, entries)) is { } functionLiteral)
        {
            EmitConstant(functionLiteral, 0);
            instructions.WriteByte(Return);

            return;
        }
        if (TryEmitConstantCallSequence(clause, location, entries) || TryEmitCallResultPair(clause, location) || TryEmitAppendPair(clause, location)
            || TryEmitLoggedApplyCatch(clause, location, entries) || TryEmitClockDelta(clause, location)
            || TryEmitComputedMap(clause, location, entries) || TryEmitArgumentTupleMap(clause)
            || TryEmitLocalCallTupleTail(clause, location, entries) || TryEmitTupleArgumentTail(clause, location)
            || TryEmitLocalFunctionContainer(clause, entries))
        {
            return;
        }

        clause = FoldConstantCallBindings(clause);
        if (TryEmitTaggedArgumentError(clause, location) || TryEmitDirectAppend(clause, location, entries))
        {
            return;
        }

        if (FindCall(clause.Body) is { } call)
        {
            if (!TryEmitBif(clause, call, entries))
            {
                EmitCall(clause, location, call, entries);
            }
            return;
        }

        EmitBody(clause);
        instructions.WriteByte(Return);
    }

    private FunctionDefinition FoldLiteralAtomConversions(FunctionDefinition function)
    {
        if (!function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body).Any(expression => ReadLiteralAtomConversion(expression) is not null)))
        {
            return function;
        }

        return function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(clause.Body, Fold, rewriteComprehensions: true)
            }).ToArray()
        };

        Atom? ReadLiteralAtomConversion(Expr expression) =>
            expression is Expr.Call
            {
                Function: AutoImportNames.AtomToList, DynamicRemote: false,
                Arguments: [Expr.Literal { Value: Atom atom }]
            } call
            && (call.Module == ErlangModuleNames.Module || call.Module is null
                && (function.Name, function.Arity) != (AutoImportNames.AtomToList, 1) && ResolvesErlangBif(call))
                ? atom : null;

        Expr Fold(Expr expression) => ReadLiteralAtomConversion(expression) is { } atom
            ? new Expr.Literal(Term.String(atom.Name))
            : expression;
    }

    private static Clause FoldConstantCallBindings(Clause clause)
    {
        if (clause is not
            {
                Guard: null, Body: Expr.Sequence
                {
                    Items: [_, .., Expr.Call
                    { Module: not null, DynamicRemote: false } call]
                } sequence
            }
            || !(HasUnconditionalParameters(clause) || HasNilParameter(clause))
            || ReadConstantBindings(clause, sequence) is not { } bindings)
        {
            return clause;
        }

        var arguments = new Expr[call.Arguments.Count];
        for (int index = 0; index < arguments.Length; index++)
        {
            Expr argument = call.Arguments[index];
            if (AttributeLiteral.TryNormalizeConstant(argument, out Term value, bindings))
            {
                arguments[index] = new Expr.Literal(value);
            }
            else if (argument is Expr.Variable variable
                && clause.Patterns.Any(pattern => pattern is Pattern.Variable parameter && parameter.Name == variable.Name))
            {
                arguments[index] = argument;
            }
            else
            {
                return clause;
            }
        }

        return clause with { Body = call with { Arguments = arguments } };
    }

    private void EmitLiteralPattern(Clause first, Clause fallback, BeamSourceLocation location)
    {
        int failure = nextLabel++;
        EmitLiteralTest(first, failure);
        EmitBody(first);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, failure);
        EmitDispatchFallback(fallback, location);
    }

    private void EmitLiteralTest(Clause clause, int failure)
    {
        Op(ExactEqualTest, LabelTag, failure);
        Operand(XRegisterTag, 0);
        EmitPatternOperand(((Pattern.Literal)clause.Patterns[0]).Value);
    }

    private void EmitLiteralDispatch(
        IReadOnlyList<Clause> clauses,
        BeamSourceLocation location,
        int? functionInfo = null,
        bool reuseMatchingInput = false,
        Action? emitComputedFallback = null,
        bool hasFallbackClause = true,
        int selectorType = 0
    )
    {
        var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
        var tested = functionInfo.HasValue || !hasFallbackClause ? clauses : clauses.SkipLast(1);
        var branches = tested
            .Select(clause => (Key: ((Pattern.Literal)clause.Patterns[0]).Value, Result: DispatchResult(clause)))
            .OrderByDescending(branch => branch.Key, comparer).ToArray();
        // OTP merges equal return blocks into the one for the smallest matching key.
        var labels = new Dictionary<Term, int>();
        foreach (var branch in branches.Reverse().DistinctBy(branch => branch.Result).Reverse())
        {
            labels.Add(branch.Result, nextLabel++);
        }
        int failure = functionInfo ?? nextLabel++;

        instructions.WriteByte(SelectValue);
        if (selectorType != 0 && branches.All(branch => branch.Key is Atom))
        {
            EmitTypedRegister(0, selectorType);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }

        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, branches.Length * 2);
        for (int index = branches.Length - 1; index >= 0; index--)
        {
            EmitPatternOperand(branches[index].Key);
            Operand(LabelTag, labels[branches[index].Result]);
        }
        foreach (var (result, label) in labels)
        {
            Op(Label, BeamWire.UnsignedTag, label);
            if (!reuseMatchingInput || branches.Any(branch => branch.Result.Equals(result) && !branch.Key.Equals(result)))
            {
                EmitConstant(result, 0);
            }
            instructions.WriteByte(Return);
        }
        if (!functionInfo.HasValue)
        {
            Op(Label, BeamWire.UnsignedTag, failure);
            if (emitComputedFallback is null)
            {
                EmitDispatchFallback(clauses[^1], location);
            }
            else
            {
                emitComputedFallback();
            }
        }
    }

    private void EmitFiniteAtomLiteralDispatch(FiniteAtomLiteralDispatch plan)
    {
        var branches = plan.Branches;
        if (plan.LabelCount == 0)
        {
            EmitLiteralReturn(branches[0].Result);

            return;
        }

        if (branches.Length == 2)
        {
            int alternative = nextLabel++;
            Op(ExactEqualTest, LabelTag, alternative);
            Operand(XRegisterTag, 0);
            EmitPatternOperand(branches[0].Key);
            EmitLiteralReturn(branches[0].Result);
            Op(Label, BeamWire.UnsignedTag, alternative);
            EmitLiteralReturn(branches[1].Result);

            return;
        }

        var labels = new Dictionary<Term, int>();
        foreach (var branch in branches.Reverse().DistinctBy(branch => branch.Result).Reverse())
        {
            labels.Add(branch.Result, nextLabel++);
        }

        Term fallback = branches[^1].Result;
        EmitMembershipSelect(
            0,
            labels[fallback],
            branches.Reverse().Where(branch => !branch.Result.Equals(fallback))
                .Select(branch => (branch.Key, labels[branch.Result])),
            AtomTypeBits
        );
        foreach (var (result, label) in labels)
        {
            Op(Label, BeamWire.UnsignedTag, label);
            EmitConstant(result, 0);
            instructions.WriteByte(Return);
        }
    }

    private static Term DispatchResult(Clause clause) =>
        AttributeLiteral.TryNormalizeConstant(clause.Body, out Term literal)
            ? literal
            : throw new NotSupportedException(Unsupported);

    private static bool TryReadTupleError(Clause clause, out Expr.Call? call, out Atom? tag)
    {
        if (clause is
            {
                Patterns: [Pattern.Variable parameter],
                Body: Expr.Call
                {
                    Module: ErlangModuleNames.Module,
                    Function: "error",
                    DynamicRemote: false,
                    Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom reason }, Expr.Variable value] }]
                } error
            }
            && parameter.Name != VariableScopeNames.Wildcard && parameter.Name == value.Name)
        {
            call = error;
            tag = reason;

            return true;
        }

        call = null;
        tag = null;

        return false;
    }

    private void EmitDispatchFallback(Clause clause, BeamSourceLocation location)
    {
        if (ReadArgumentListError(clause) is { } error)
        {
            EmitArgumentListError(error, location);
            return;
        }

        if (!TryReadTupleError(clause, out var call, out var tag))
        {
            EmitBody(clause);
            instructions.WriteByte(Return);

            return;
        }

        EmitTaggedArgumentError(call!, tag!, [0], location);
    }

    private bool TryEmitTaggedArgumentError(Clause clause, BeamSourceLocation location)
    {
        if (clause.Body is not Expr.Call
            {
                Module: null or ErlangModuleNames.Module,
                Function: "error",
                DynamicRemote: false,
                Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, _, ..] } tuple]
            } call || !HasUnconditionalParameters(clause) || !ResolvesErlangBif(call))
        {
            return false;
        }

        var registers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < clause.Patterns.Count; index++)
        {
            if (clause.Patterns[index] is Pattern.Variable parameter && parameter.Name != VariableScopeNames.Wildcard)
            {
                registers.Add(parameter.Name, index);
            }
        }

        var fields = new int[tuple.Items.Count - 1];
        for (int index = 0; index < fields.Length; index++)
        {
            if (tuple.Items[index + 1] is not Expr.Variable variable
                || !registers.TryGetValue(variable.Name, out fields[index]))
            {
                return false;
            }
        }

        EmitTaggedArgumentError(call, tag, fields, location);
        return true;
    }

    private void EmitTaggedArgumentError(Expr.Call call, Atom tag, int[] fields, BeamSourceLocation location)
    {
        int arity = fields.Length + 1;
        Op(TestHeap, BeamWire.UnsignedTag, arity + 1);
        Operand(BeamWire.UnsignedTag, fields.Max() + 1);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, arity);
        Operand(AtomTag, Intern(tag.Name));
        foreach (int register in fields)
        {
            Operand(XRegisterTag, register);
        }

        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, call.Function, 1));
    }

    private void EmitPatternOperand(Term key)
    {
        switch (key)
        {
            case Atom atom:
                Operand(AtomTag, Intern(atom.Name));
                break;
            case Integer integer:
                Operand(BeamWire.IntegerTag, integer.Value);
                break;
            default:
                throw new NotSupportedException(Unsupported);
        }
    }

    private void EmitCall(
        Clause clause,
        BeamSourceLocation functionLocation,
        Expr.Call call,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        bool keepFrame = false,
        bool orderTailMoves = false
    )
    {
        call = ResolveImportedCall(call, entries);
        IReadOnlyList<Expr> arguments = call.Arguments;
        int arity = arguments.Count;
        if (call.DynamicRemote && call.Arguments is [var module, var function, Expr.List { Tail: null } values])
        {
            arity = values.Items.Count;
            arguments = [.. values.Items, module, function];
        }
        int target = 0;
        bool makeFunction = call.Module == ErlangModuleNames.Module && !call.DynamicRemote
            && call.Function == FunctionReferenceLoweringNames.MakeFunction && arity == 3;
        if ((call.Module == ErlangModuleNames.Module && !call.DynamicRemote && !makeFunction && !IsConstantErrorCall(call)
                && (call.Function is not (ErlangOperators.Append or ErlangOperators.SubtractList) || arity != 2)
                && (call.Function != DisplayFunction || arity != 1))
            || (call.Module is null && !entries.TryGetValue((call.Function, arity), out target)))
        {
            throw new NotSupportedException(Unsupported);
        }
        var registers = clause.Patterns.Select((pattern, index) => (Variable: pattern as Pattern.Variable, Index: index))
            .Where(entry => entry.Variable is not null && entry.Variable.Name != VariableScopeNames.Wildcard)
            .ToDictionary(entry => entry.Variable!.Name, entry => entry.Index, StringComparer.Ordinal);
        var moves = new List<(int Destination, int Source)>();
        var constants = new List<(int Destination, Term Value)>();
        (int Destination, int Head)? consArgument = null;
        int live = 0;
        for (int index = 0; index < arguments.Count; index++)
        {
            Expr argument = arguments[index];
            if (AttributeLiteral.TryNormalizeConstant(argument, out Term literal))
            {
                constants.Add((index, literal));
                continue;
            }
            if (argument is Expr.List { Items: [Expr.Variable head], Tail: null }
                && index >= clause.Patterns.Count && consArgument is null
                && registers.TryGetValue(head.Name, out int headRegister))
            {
                consArgument = (index, headRegister);
                live = Math.Max(live, headRegister + 1);
                continue;
            }
            if (argument is not Expr.Variable variable || !registers.TryGetValue(variable.Name, out int source))
            {
                throw new NotSupportedException(Unsupported);
            }
            live = Math.Max(live, source + 1);
            if (index != source)
            {
                moves.Add((index, source));
            }
        }

        Expr? continuation = clause.Body is Expr.Sequence sequence ? sequence.Items[1] : null;
        bool ordinary = keepFrame || continuation is not null;
        int? saved = null;
        if (continuation is Expr.Variable preserved)
        {
            if (!registers.TryGetValue(preserved.Name, out int register))
            {
                throw new NotSupportedException(Unsupported);
            }
            saved = register;
            live = Math.Max(live, register + 1);
        }
        int stackSlots = saved.HasValue ? 1 : 0;
        if (consArgument is { } cons)
        {
            if (ordinary || call.DynamicRemote)
            {
                throw new NotSupportedException(Unsupported);
            }
            Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            Operand(BeamWire.UnsignedTag, live);
            Op(PutList, XRegisterTag, cons.Head);
            Operand(AtomTag, NilAtomIndex);
            Operand(XRegisterTag, cons.Destination);
        }
        if (ordinary || call.DynamicRemote)
        {
            Op(Allocate, BeamWire.UnsignedTag, stackSlots);
            Operand(BeamWire.UnsignedTag, live);
        }
        if (saved is int sourceRegister)
        {
            Op(Move, XRegisterTag, sourceRegister);
            Operand(YRegisterTag, 0);
        }
        if (orderTailMoves)
        {
            EmitOrderedTailMoves(moves, constants);
        }
        else if (arity != 0 && constants.Count == arity && !ordinary && !call.DynamicRemote)
        {
            EmitIndependentCallMoves(constants);
        }
        else
        {
            // The independent x1 move precedes the x0-to-x2 forwarding chain.
            if (makeFunction && !ordinary && moves is [(2, 0)]
                && constants is [(0, _), (1, var functionName)])
            {
                EmitConstant(functionName, 1);
                constants.RemoveAt(1);
            }
            // A local tail call can populate the new argument register before forwarding x1.
            if (!ordinary && call.Module is null && !call.DynamicRemote && consArgument is null
                && clause.Patterns.Count == 2 && moves is [(0, 1)]
                && constants is [(1, _), (2, var trailing)])
            {
                EmitConstant(trailing, 2);
                constants.RemoveAt(1);
            }
            EmitArgumentMoves(moves, constants);
        }
        if (ordinary || call.Module is not null)
        {
            var callLocation = new BeamSourceLocation(call.PreprocessorFile, call.SourceLine);
            if (callLocation.Line != 0 && callLocation != functionLocation)
            {
                EmitLine(callLocation);
            }
        }
        if (call.DynamicRemote)
        {
            Op(ordinary ? DynamicApply : TailDynamicApply, BeamWire.UnsignedTag, arity);
            if (!ordinary)
            {
                Operand(BeamWire.UnsignedTag, stackSlots);
            }
        }
        else if (call.Module is null)
        {
            Op(ordinary ? LocalCall : TailLocalCall, BeamWire.UnsignedTag, arity);
            Operand(LabelTag, target);
        }
        else
        {
            Op(ordinary ? ExternalCall : TailExternalCall, BeamWire.UnsignedTag, arity);
            Operand(BeamWire.UnsignedTag, Import(call.Module, call.Function, arity));
        }

        if (continuation is not null)
        {
            if (saved.HasValue)
            {
                MoveToX0(YRegisterTag, 0);
            }
            else
            {
                EmitConstant(continuation, 0);
            }
            Op(Deallocate, BeamWire.UnsignedTag, stackSlots);
            instructions.WriteByte(Return);
        }
    }

    private static bool HasNilParameter(Clause clause) =>
        clause.Patterns is [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }];

    private Expr.Call ResolveImportedCall(Expr.Call call, IReadOnlyDictionary<(string Name, int Arity), int> entries) =>
        call.Module is null && !entries.ContainsKey((call.Function, call.Arguments.Count))
        && sourceModule?.CallBindings.ImportedModule(call.Function, call.Arguments.Count) is { } imported
            ? call with { Module = imported }
            : call;

    private string? ResolveExternalModule(string name, int arity) =>
        SourceCallResolution.ExternalModule(sourceModule, name, arity);

    private static bool IsConstantErrorCall(Expr.Call call) =>
        call is { Module: ErlangModuleNames.Module, Function: AutoImportNames.Error, DynamicRemote: false, Arguments: [var reason] }
        && AttributeLiteral.TryNormalizeConstant(reason, out _);

    private void EmitIndependentCallMoves(List<(int Destination, Term Value)> constants)
    {
        foreach (int index in IndependentCallMoveOrder(constants.Count))
        {
            var (destination, value) = constants[index];
            EmitConstant(value, destination);
        }
    }

    private static int[] IndependentCallMoveOrder(int count)
    {
        // OTP collect_chain reverses the remaining independent moves after each selection.
        // order_moves prepends each selected move, leaving the X0 move next to the call.
        var order = new int[count];
        int first = 0;
        int last = count - 1;
        bool fromFirst = true;
        for (int index = order.Length - 1; index >= 0; index--)
        {
            order[index] = fromFirst ? first++ : last--;
            fromFirst = !fromFirst;
        }

        return order;
    }

    private void EmitOrderedTailMoves(
        List<(int Destination, int Source)> registerMoves,
        List<(int Destination, Term Value)> constants,
        List<(int Destination, int Source)>? stackMoves = null,
        int[]? expiredSlots = null,
        bool deferX0Constant = false,
        bool deferX0StackLoad = false
    )
    {
        var moves = registerMoves.Select(move => (move.Destination, Source: (int?)move.Source, Tag: XRegisterTag, Value: (Term?)null))
            .Concat(constants.Select(move => (move.Destination, Source: (int?)null, Tag: XRegisterTag, Value: (Term?)move.Value)))
            .Concat((stackMoves ?? []).Select(move => (move.Destination, Source: (int?)move.Source, Tag: YRegisterTag, Value: (Term?)null)))
            .OrderBy(move => move.Destination)
            .ToList();
        var chains = new List<List<(int Destination, int? Source, int Tag, Term? Value, byte Opcode)>>();
        while (moves.Count != 0)
        {
            var chain = new List<(int Destination, int? Source, int Tag, Term? Value, byte Opcode)>();
            var move = moves[0];
            moves.RemoveAt(0);
            chain.Add((move.Destination, move.Source, move.Tag, move.Value, Move));
            while (true)
            {
                int next = move.Tag == XRegisterTag && move.Source is int source
                    ? moves.FindIndex(candidate => candidate.Destination == source)
                    : -1;
                if (next < 0)
                {
                    // OTP retains the reverse scan order when a chain has no predecessor.
                    moves.Reverse();
                    break;
                }

                move = moves[next];
                moves.RemoveAt(next);
                int cycleStart = move.Tag == XRegisterTag ? chain.FindIndex(candidate => candidate.Destination == move.Source) : -1;
                if (cycleStart >= 0)
                {
                    chain.RemoveAt(cycleStart);
                    for (int index = cycleStart; index < chain.Count; index++)
                    {
                        var step = chain[index];
                        chain[index] = (step.Destination, step.Source, step.Tag, step.Value, Swap);
                    }

                    chain.Add((move.Destination, move.Source, move.Tag, move.Value, Swap));
                    break;
                }

                chain.Add((move.Destination, move.Source, move.Tag, move.Value, Move));
            }

            chains.Add(chain);
        }

        var stackLoads = new List<(int Destination, int Source)>();
        bool pendingKill = expiredSlots is { Length: > 0 };
        for (int index = chains.Count - 1; index >= 0; index--)
        {
            var chain = chains[index];
            for (int step = 0; step < chain.Count; step++)
            {
                var (destination, source, tag, value, opcode) = chain[step];
                if (opcode == Move && tag == YRegisterTag && destination != 0 && source is int stackSlot)
                {
                    stackLoads.Add((destination, stackSlot));
                    continue;
                }

                EmitStackLoads();

                // The caller emits the final X0 assignment after trimming killed slots.
                if (destination == 0 && (deferX0Constant && value is not null || deferX0StackLoad && tag == YRegisterTag))
                {
                    continue;
                }

                // OTP moves the final X0 assignment past a kill unless it reads a killed slot.
                if (pendingKill && index == 0 && step == chain.Count - 1 && destination == 0 && opcode == Move
                    && !(tag == YRegisterTag && source is int slot && expiredSlots!.Contains(slot)))
                {
                    EmitSlotKills(expiredSlots!);
                    pendingKill = false;
                }

                if (source is int register)
                {
                    Op(opcode, tag, register);
                    Operand(XRegisterTag, destination);
                }
                else
                {
                    EmitConstant(value!, destination);
                }
            }
        }

        EmitStackLoads();

        if (pendingKill)
        {
            EmitSlotKills(expiredSlots!);
        }

        void EmitStackLoads()
        {
            // OTP sorts consecutive stack loads by Y slot and keeps the final X0 move in place.
            if (stackLoads.Count == 0)
            {
                return;
            }

            foreach (var (destination, slot) in stackLoads.AsEnumerable().Reverse().OrderBy(move => move.Source))
            {
                Op(Move, YRegisterTag, slot);
                Operand(XRegisterTag, destination);
            }

            stackLoads.Clear();
        }
    }

    private void EmitSlotKills(int[] slots)
    {
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, slots.Length);
        foreach (int slot in slots)
        {
            Operand(YRegisterTag, slot);
        }
    }

    private void EmitArgumentMoves(List<(int Destination, int Source)> moves, List<(int Destination, Term Value)> constants)
    {
        while (moves.Count != 0 || constants.Count != 0)
        {
            int ready = moves.FindIndex(move => !moves.Exists(other => other.Source == move.Destination));
            if (ready >= 0)
            {
                var move = moves[ready];
                Op(Move, XRegisterTag, move.Source);
                Operand(XRegisterTag, move.Destination);
                moves.RemoveAt(ready);
                continue;
            }

            int constant = constants.FindLastIndex(item => !moves.Exists(move => move.Source == item.Destination));
            if (constant >= 0)
            {
                var (destination, value) = constants[constant];
                EmitConstant(value, destination);
                constants.RemoveAt(constant);
                continue;
            }

            var cycle = new List<int>();
            int first = moves[0].Destination;
            int current = first;
            do
            {
                cycle.Add(current);
                int index = moves.FindIndex(move => move.Destination == current);
                current = moves[index].Source;
                moves.RemoveAt(index);
            }
            while (current != first);

            int pivot = cycle[^1];
            for (int index = cycle.Count - 2; index >= 0; index--)
            {
                int register = cycle[index];
                if (cycle.Count == 3)
                {
                    Op(Swap, XRegisterTag, index == 0 ? register : pivot);
                    Operand(XRegisterTag, index == 0 ? pivot : register);
                }
                else
                {
                    Op(Swap, XRegisterTag, Math.Min(register, pivot));
                    Operand(XRegisterTag, Math.Max(register, pivot));
                }
            }
        }
    }
}
