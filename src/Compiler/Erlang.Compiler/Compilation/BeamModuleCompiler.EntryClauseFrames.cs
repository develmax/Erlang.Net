using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSeparateEntryFrames(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses.Count < 2)
            {
                return false;
            }

            var sourceFunction = function;
            Clause firstClause = function.Clauses[0];
            bool guardDispatch = firstClause.Guard is not null && CanEmitSeparateEntryGuard(firstClause, firstClause.Guard)
                && function.Clauses.All(HasUnconditionalParameters);
            bool complementaryGuards = guardDispatch && HasComplementaryEntryComparisons(function.Clauses);
            int reorderedListInput = ReadNilBeforeConsInput(function.Clauses, out var reorderedClauses);
            if (reorderedListInput >= 0)
            {
                function = function with { Clauses = reorderedClauses };
            }

            bool listDispatch = function.Clauses.Any(clause => clause.Patterns.Any(pattern => EntryShape(pattern) is Pattern.List));
            bool tupleDispatch = function.Clauses.Any(clause => clause.Patterns.Any(pattern =>
                (listDispatch ? pattern : EntryShape(pattern)) is Pattern.Tuple));
            bool hasRecordUpdate = function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body)
                .Any(expression => expression is Expr.RecordUpdate));
            bool recordReturnDispatch = hasRecordUpdate && function.Clauses is
                [{ Guard: null } recordArm,
                 { Guard: null, Body: Expr.Tuple } recordFallback]
                && ReadEntryRecordCall(recordArm.Body) is not null
                && HasUnconditionalParameters(recordFallback) && recordFallback.Body is Expr.Tuple fallbackTuple
                && fallbackTuple.Items.All(IsPreparedOperand);
            // Other record-update bodies and closures still use their existing entry plans.
            bool tupleDispatchSupported = tupleDispatch && (!hasRecordUpdate || recordReturnDispatch)
                && !function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body)
                    .Any(expression => expression is Expr.Fun));
            bool tupleBodyDispatch = tupleDispatchSupported
                && (recordReturnDispatch || function.Clauses.Any(clause => clause.Body is Expr.Sequence or Expr.Case or Expr.If));
            bool hasArithmeticCall = function.Clauses.Any(clause => clause.Body is Expr.Call call
                && call.Arguments.Any(argument => argument is Expr.Binary));
            bool literalDispatch = !tupleDispatch && !listDispatch
                && function.Clauses.Any(clause => clause.Patterns.Any(pattern => pattern is Pattern.Literal));
            var listReturn = listDispatch ? ReadEntryListReturn(function.Clauses) : null;
            bool useConsResults = listDispatch && (function.Clauses.Count > 2 || listReturn is null);
            int sharedHeadInput = ReadSharedHeadConsInput(function.Clauses);
            int sharedConsInput = sharedHeadInput >= 0 ? sharedHeadInput : ReadSharedEntryConsInput(function.Clauses);
            var twoCellPrefix = sharedConsInput < 0 ? ReadTwoCellConsPrefix(function) : null;
            var mixedPrefix = twoCellPrefix is { } twoCell
                ? (Input: twoCell.Input, Count: twoCell.Keys.Length + 1)
                : sharedConsInput < 0 ? ReadMixedConsPrefix(function) : null;
            if (mixedPrefix is not null && twoCellPrefix is null)
            {
                function = FoldIdentityNilFallback(function, mixedPrefix.Value.Input);
                mixedPrefix = ReadMixedConsPrefix(function);
            }

            int terminalConsInput = ReadTerminalEntryConsInput(function.Clauses);
            bool reuseMixedTail = mixedPrefix is { } mixedEntry && CanReuseMixedConsTail(function, mixedEntry);
            var branches = new List<(Clause Clause, string[] Parameters, List<(int Index, Term Value)> Literals,
                List<(int Input, int Head, int Tail, Term? HeadLiteral)> Cons,
                List<(int Input, int Arity, Atom? Tag, List<(int Index, int Register)> Fields)> Tuples,
                Dictionary<int, (int? Register, Term? Literal)> FieldTests,
                List<(int Input, int Bound)> Equalities,
                bool TupleCall, ConsCallResult? ConsResult, EntryCall? TailCall, EntryCallChain? CallChain,
                (Expr.Call Call, (int? Register, Term? Value)[] Elements)? TailList,
                (Expr.Call Call, CallArgumentConstructions Plan)? TailConstruction,
                EntryConstructedArgument? TupleResult, (Pattern.Variable Variable, int Register)[]? BodyInputs,
                DeferredConsEntry? DeferredCons, bool ArithmeticTail, CallArgumentConstructions? ReturnedConstruction,
                SavedCallSequence? SavedCalls)>();
            foreach (Clause clause in function.Clauses)
            {
                HashSet<string> unusedTupleFields = [];
                if (clause.Patterns.Any(pattern => pattern is Pattern.Tuple))
                {
                    var tupleFields = clause.Patterns.OfType<Pattern.Tuple>()
                        .SelectMany(tuple => tuple.Items.OfType<Pattern.Variable>())
                        .Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
                    unusedTupleFields = clause.Patterns.SelectMany(Semantics.Variables)
                        .GroupBy(name => name, StringComparer.Ordinal)
                        .Where(group => group.Count() == 1 && tupleFields.Contains(group.Key))
                        .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
                    try
                    {
                        unusedTupleFields.ExceptWith(ClosureCaptures.ReadEntryReferences(clause, unusedTupleFields));
                    }
                    catch (NotSupportedException)
                    {
                        unusedTupleFields.Clear();
                    }
                }

                var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, function.Arity).ToList();
                var literals = new List<(int Index, Term Value)>();
                var cons = new List<(int Input, int Head, int Tail, Term? HeadLiteral)>();
                var tuples = new List<(int Input, int Arity, Atom? Tag, List<(int Index, int Register)> Fields)>();
                var fieldTests = new Dictionary<int, (int? Register, Term? Literal)>();
                var equalities = new List<(int Input, int Bound)>();
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < function.Arity; index++)
                {
                    Pattern entryPattern = clause.Patterns[index];
                    bool aliasedCons = false;
                    if (entryPattern is AliasPattern listAlias
                        && TryReadPatternAlias(listAlias, out var aliasBinding, out var aliasShape)
                        && aliasShape is Pattern.List { Items.Count: 1, Tail: Erlang.Pattern.Variable })
                    {
                        aliasedCons = true;
                        entryPattern = aliasShape;
                        if (!BindListVariable(aliasBinding, index))
                        {
                            return false;
                        }
                    }

                    if (twoCellPrefix is { } shared && index == shared.Input && branches.Count < shared.Keys.Length
                        && entryPattern is Pattern.List { Tail: Pattern.Variable suffix })
                    {
                        int headRegister = parameters.Count;
                        parameters.AddRange(Enumerable.Repeat(VariableScopeNames.Wildcard, 3));
                        int suffixRegister = headRegister + 2;
                        cons.Add((index, headRegister, suffixRegister, null));
                        if (!BindListVariable(suffix, suffixRegister))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (mixedPrefix is { } mixed && index == mixed.Input && branches.Count < mixed.Count
                        && entryPattern is Pattern.List { Items: [var mixedHead], Tail: Pattern.Variable mixedTail })
                    {
                        int headRegister = parameters.Count;
                        parameters.Add(VariableScopeNames.Wildcard);
                        int tailRegister = reuseMixedTail ? index : parameters.Count;
                        if (!reuseMixedTail)
                        {
                            parameters.Add(VariableScopeNames.Wildcard);
                        }

                        cons.Add((index, headRegister, tailRegister, (mixedHead as Pattern.Literal)?.Value));
                        if (mixedHead is Pattern.Variable head && !BindListVariable(head, headRegister)
                            || !BindListVariable(mixedTail, tailRegister)
                            || EntryShape(mixedHead) is Pattern.Tuple tuple && !ReadConsTuple(headRegister, mixedHead, tuple))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (function.Arity == 1
                        && clause.Patterns[index] is Pattern.List { Items.Count: > 1, Tail: Erlang.Pattern.Variable } prefix)
                    {
                        if (!ReadListPrefix(index, prefix))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (index == sharedHeadInput && branches.Count < function.Clauses.Count - 1
                        && clause.Patterns[index] is Pattern.List { Items: [var sharedHead], Tail: Pattern.Variable sharedTail })
                    {
                        int headRegister = parameters.Count;
                        parameters.Add(VariableScopeNames.Wildcard);
                        if (sharedHead is Pattern.Variable head && !BindListVariable(head, headRegister)
                            || !BindListVariable(sharedTail, index))
                        {
                            return false;
                        }

                        cons.Add((index, headRegister, index, (sharedHead as Pattern.Literal)?.Value));
                        if (EntryShape(sharedHead) is Pattern.Tuple sharedTuple
                            && !ReadConsTuple(headRegister, sharedHead, sharedTuple))
                        {
                            return false;
                        }
                        continue;
                    }

                    switch (entryPattern)
                    {
                        case Pattern.Variable variable when variable.Name == VariableScopeNames.Wildcard
                            || names.Add(variable.Name):
                            parameters[index] = variable.Name;
                            break;
                        case Pattern.Variable variable when listDispatch && BindListVariable(variable, index):
                            break;
                        case Pattern.Literal literal:
                            literals.Add((index, literal.Value));
                            break;
                        case Pattern.List { Items: [], Tail: null }:
                            literals.Add((index, Nil.Value));
                            break;
                        case Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }:
                            bool repeat = HasRepeatedConsBindings(clause, index);
                            bool terminalInput = (branches.Count == 0 && index == terminalConsInput)
                                || branches.Count == 0 && function.Clauses.Count == 2 && clause.Guard is null
                                    && (clause.Body is Expr.Case || clause.Body is Expr.Call arithmeticCall
                                        && arithmeticCall.Arguments.Any(argument => argument is Expr.Binary))
                                    && ListEntryTest(function.Clauses[1].Patterns[index]) == IsNil
                                    && clause.Patterns.Where((_, position) => position != index).All(pattern => pattern is Pattern.Variable)
                                || clause.Guard is null && clause.Body is Expr.Call terminalCall
                                    && terminalCall.Arguments.Any(argument => argument is Expr.List { Tail: Expr.Variable })
                                    && clause.Patterns.Where((_, position) => position != index).All(pattern => pattern is Pattern.Variable);
                            terminalInput &= !repeat && !aliasedCons;
                            bool reuseHead = terminalInput && tail.Name == VariableScopeNames.Wildcard
                                && head.Name != VariableScopeNames.Wildcard;
                            int headRegister = reuseHead ? index : AddListBinding(head);
                            bool reuseInput = index == sharedConsInput || terminalInput;
                            int tailRegister = tail.Name == VariableScopeNames.Wildcard ? -1
                                : reuseInput ? index : AddListBinding(tail);
                            if (headRegister < -1 || tailRegister < -1)
                            {
                                return false;
                            }

                            if (reuseInput)
                            {
                                if (!BindListVariable(reuseHead ? head : tail, index))
                                {
                                    return false;
                                }
                            }
                            cons.Add((index, headRegister, tailRegister, null));
                            break;
                        case Pattern.List { Items: [Pattern.Literal literalHead], Tail: Pattern.Variable literalTail }
                            when literalTail.Name == VariableScopeNames.Wildcard || names.Add(literalTail.Name):
                            int literalRegister = parameters.Count;
                            parameters.Add(VariableScopeNames.Wildcard);
                            cons.Add((index, literalRegister, AddField(literalTail), literalHead.Value));
                            break;
                        case Pattern.List { Items: [var tupleHead], Tail: Pattern.Variable tupleTail }
                            when EntryShape(tupleHead) is Pattern.Tuple tupleShape
                                && (useConsResults || tupleHead is not AliasPattern):
                            int tupleRegister = parameters.Count;
                            parameters.Add(VariableScopeNames.Wildcard);
                            int tupleTailRegister = AddListBinding(tupleTail);
                            cons.Add((index, tupleRegister, tupleTailRegister, null));
                            if (tupleTailRegister < -1 || !ReadConsTuple(tupleRegister, tupleHead, tupleShape))
                            {
                                return false;
                            }
                            break;
                        case Pattern.Tuple tuple when ReadTuple(index, tuple):
                            break;
                        case AliasPattern alias when (listDispatch || tupleBodyDispatch)
                            && TryReadPatternAlias(alias, out var binding, out var shape)
                            && shape is Pattern.Tuple aliasedTuple && BindListVariable(binding, index)
                            && ReadTuple(index, aliasedTuple):
                            break;
                        default:
                            return false;
                    }
                }

                Clause guardClause = cons.Count == 0 && tuples.Count == 0 && equalities.Count == 0 ? clause : clause with
                {
                    Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                };
                if (literalDispatch && literals.Count == 1 && clause.Guard is null && clause.Body is Expr.Bits
                    && AttributeLiteral.TryNormalizeConstant(clause.Body, out Term bits))
                {
                    guardClause = guardClause with { Body = new Expr.Literal(bits) };
                }

                if (clause.Body is Expr.Tuple tupleConstant && literals is [{ Value: Nil }]
                    && branches.Any(branch => branch.Cons.Count > 0 && branch.Clause.Body is Expr.Case)
                    && AttributeLiteral.TryNormalizeConstant(tupleConstant, out Term emptyResult))
                {
                    guardClause = guardClause with { Body = new Expr.Literal(emptyResult) };
                }

                if (tupleBodyDispatch && clause.Body is Expr.Tuple tupleBody
                    && AttributeLiteral.TryNormalizeConstant(tupleBody, out Term literalResult))
                {
                    guardClause = guardClause with { Body = new Expr.Literal(literalResult) };
                }

                bool emptyAfterCons = cons.Count == 0
                    && branches.Count > 0
                    && literals.Any(literal => literal.Value is Nil
                        && branches.All(branch => branch.Cons.Any(list => list.Input == literal.Index)));
                if (clause.Body is Expr.List { Items: [], Tail: null }
                    && (emptyAfterCons || branches.Any(branch => branch.ConsResult is not null
                        || branch.Cons.Count > 0 && branch.Clause.Body is Expr.Case)))
                {
                    guardClause = guardClause with { Body = new Expr.Literal(Nil.Value) };
                }

                if (parameters.Count > FirstReservedXRegister
                    || !CanEmitSeparateEntryGuard(guardClause, clause.Guard, allowRemainder: function.Arity == 1 && cons.Count > 1))
                {
                    return false;
                }

                if (literals.Count == 0 && cons.Count == 0 && tuples.Count == 0 && equalities.Count == 0
                    && clause.Guard is null && branches.Count + 1 < function.Clauses.Count)
                {
                    return false;
                }
                string[] inputs = parameters.ToArray();
                bool patternDispatch = listDispatch || tupleDispatch || literalDispatch || guardDispatch;
                bool tupleCall = TryEmitEntryTupleCallResult(clause.Body, inputs, validateOnly: true);
                ConsCallResult? consResult = useConsResults ? ReadConsCallResult(clause.Body, inputs) : null;
                EntryCall? tailCall = patternDispatch && clause.Body is Expr.Call call
                    ? ReadEntryCall(call, inputs, allowControlTransfer: listDispatch || literalDispatch) : null;
                bool arithmeticTail = tailCall is null && clause.Body is Expr.Call && TryEmitEntryArithmeticCall(
                    sourceFunction,
                    clause,
                    clause.Body,
                    inputs,
                    -1,
                    null,
                    validateOnly: true
                );
                EntryCallChain? callChain = patternDispatch
                    ? ReadEntryCallChain(clause.Body, inputs, allowDiscardedPrefix: literalDispatch) : null;
                (Expr.Call Call, CallArgumentConstructions Plan)? tailConstruction = null;
                if ((listDispatch || literalDispatch) && !arithmeticTail && tailCall is null && callChain is null
                    && clause.Body is Expr.Call candidate)
                {
                    tailConstruction = ReadEntryTailConstruction(candidate, inputs, literalDispatch || hasArithmeticCall);
                }

                (Expr.Call Call, (int? Register, Term? Value)[] Elements)? tailList = null;
                if ((listDispatch || tupleDispatch) && clause.Body is Expr.Call listCall
                    && ReadRegisterTailList(guardClause, listCall) is { } elements
                    && TryResolveOrdinaryCall(listCall, out string? listModule, preparedArgument: 0))
                {
                    tailList = (listCall with { Module = listModule }, elements);
                }

                if (tailCall is null && tailList is null && clause.Body is Expr.Call constructedCall
                    && ReadEntryCall(constructedCall, inputs, allowConstructedArgument: true) is
                        { ConstructedArgument: { } } constructedTailCall
                    && (constructedTailCall.ConstructedArgument.IsTuple
                        ? cons.Count == 0 && tuples.Count == 1
                        : listDispatch))
                {
                    tailCall = constructedTailCall;
                    var argument = constructedTailCall.ConstructedArgument;
                    if (argument.IsTuple && clause.Guard is null
                        && tuples is [var tuple] && tuple.Input == argument.Register
                        && tuple.Fields.Count > 0 && fieldTests.Count == 0
                        && inputs[tuple.Input] == VariableScopeNames.Wildcard)
                    {
                        var last = tuple.Fields[^1];
                        if (argument.Operands.Any(operand => operand.Source == last.Register)
                            && !constructedTailCall.Moves.Any(move => move.Source == last.Register))
                        {
                            tuple.Fields[^1] = (last.Index, tuple.Input);
                            tailCall = constructedTailCall with
                            {
                                ConstructedArgument = argument with
                                {
                                    Operands = argument.Operands.Select(operand => operand.Source == last.Register
                                        ? (tuple.Input, operand.Constant) : operand).ToArray()
                                }
                            };
                        }
                    }

                    if (argument.IsTuple && clause.Guard is not null
                        && tuples is [var guardedTuple] && guardedTuple.Fields is [var field]
                        && fieldTests.Count == 0 && inputs[guardedTuple.Input] == VariableScopeNames.Wildcard
                        && branches.Count == function.Clauses.Count - 2
                        && function.Clauses[^1] is { Guard: null, Body: Expr.Literal } fallback
                        && fallback.Patterns.All(pattern => pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard }))
                    {
                        string[] remapped = (string[])inputs.Clone();
                        remapped[guardedTuple.Input] = remapped[field.Register];
                        remapped[field.Register] = VariableScopeNames.Wildcard;
                        Clause remappedGuard = guardClause with
                        {
                            Patterns = remapped.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                        };
                        if (CanEmitSeparateEntryGuard(remappedGuard, clause.Guard)
                            && ReadEntryCall(constructedCall, remapped, allowConstructedArgument: true) is { } remappedCall)
                        {
                            guardedTuple.Fields[0] = (field.Index, guardedTuple.Input);
                            inputs = remapped;
                            guardClause = remappedGuard;
                            tailCall = remappedCall;
                        }
                    }

                    if (!argument.IsTuple && argument.Register >= inputs.Length && argument.Operands[0].Source is int headRegister
                        && !constructedCall.Arguments.OfType<Expr.Variable>().Any(variable => variable.Name == inputs[headRegister]))
                    {
                        tailCall = constructedTailCall with
                        {
                            ConstructedArgument = argument with { Register = headRegister },
                            Moves = constructedTailCall.Moves
                                .Select(move => move.Source == argument.Register ? (move.Destination, headRegister) : move)
                                .ToList()
                        };
                    }
                }

                if (!arithmeticTail && tailConstruction is null && tailCall is null && tailList is null && callChain is null
                    && cons.Count != 0 && clause.Body is Expr.Call consCall && consCall.Arguments.Any(argument => argument is Expr.List))
                {
                    tailConstruction = ReadEntryTailConstruction(consCall, inputs, allowOrdinary: true);
                }

                EntryConstructedArgument? tupleResult = (literalDispatch || mixedPrefix is not null || emptyAfterCons
                    || branches.Any(branch => branch.TupleCall && branch.Clause.Body is Expr.Sequence))
                    && cons.Count == 0 && tuples.Count == 0
                    && guardClause.Body is Expr.Tuple ? ReadEntryConstructedArgument(guardClause.Body, 0, inputs) : null;
                int sharedListInput = reuseMixedTail ? mixedPrefix!.Value.Input : sharedHeadInput;
                bool sharedNilReturn = sharedListInput >= 0 && cons.Count == 0 && clause.Guard is null
                    && literals is [var nil] && nil.Index == sharedListInput && nil.Value is Nil;
                CallArgumentConstructions? returnedConstruction = (hasArithmeticCall || sharedNilReturn || guardDispatch || tupleBodyDispatch)
                    && tupleResult is null
                    && guardClause.Body is Expr.List or Expr.Tuple
                    ? ReadCallArgumentConstructions(
                        [guardClause.Body],
                        -1,
                        new Dictionary<string, int>(),
                        inputs: inputs.Select((name, register) => (name, register))
                            .Where(binding => binding.name != VariableScopeNames.Wildcard)
                            .ToDictionary(binding => binding.name, binding => binding.register, StringComparer.Ordinal)
                    )
                    : null;
                DeferredConsEntry? deferredCons = null;
                SavedCallSequence? savedCalls = reuseMixedTail && cons.Count == 1 && tailCall is null
                    && callChain is null && tailConstruction is null && clause.Body is Expr.Call computedCons
                    ? ReadComputedConsTail(computedCons, inputs) : null;
                if (callChain is null && consResult is null && !tupleCall
                    && clause.Body is Expr.Sequence && cons is [var list]
                    && list.HeadLiteral is null && tuples.Count == 0 && equalities.Count == 0)
                {
                    bool reuseTail = clause.Guard is null && mixedPrefix is null && sharedConsInput < 0
                        && list.Tail >= function.Arity
                        && inputs[list.Input] == VariableScopeNames.Wildcard
                        && inputs[list.Tail] != VariableScopeNames.Wildcard;
                    if (reuseTail && list.Head >= function.Arity && inputs[list.Head] != VariableScopeNames.Wildcard)
                    {
                        var extraction = new CallListExtraction(list.Input, list.Head, list.Tail);
                        callChain = ReadStraightCallSequence(clause.Body, inputs, extraction);
                        if (callChain is null)
                        {
                            savedCalls = ReadSavedCallSequence(clause.Body, inputs, extraction);
                        }
                    }

                    if (callChain is null && savedCalls is null)
                    {
                        var callInputs = inputs;
                        if (reuseTail)
                        {
                            callInputs = (string[])inputs.Clone();
                            callInputs[list.Input] = callInputs[list.Tail];
                            callInputs[list.Tail] = VariableScopeNames.Wildcard;
                        }

                        callChain = ReadStraightCallSequence(clause.Body, callInputs);
                        if (callChain is null)
                        {
                            savedCalls = ReadSavedCallSequence(clause.Body, callInputs);
                        }

                        if (reuseTail && (callChain is not null || savedCalls is not null))
                        {
                            inputs = callInputs;
                            cons[0] = (list.Input, list.Head, list.Input, null);
                        }
                    }
                }

                if (!tupleCall && consResult is null && clause.Body is Expr.Sequence && clause.Guard is null
                    && callChain?.ListExtraction is null && savedCalls is null && mixedPrefix is null && sharedConsInput < 0
                    && cons is [var direct] && direct.HeadLiteral is null
                    && tuples.Count == 0 && equalities.Count == 0)
                {
                    var bindings = inputs.Select((name, register) => (Variable: new Pattern.Variable(name), Register: register))
                        .Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard).ToArray();
                    deferredCons = ReadDeferredConsEntry(clause.Body, bindings, direct.Input, direct.Head, direct.Tail);
                    if (deferredCons is not null)
                    {
                        callChain = null;
                    }
                }

                if (tupleBodyDispatch && tuples.Count != 0 && clause.Guard is null && savedCalls is null
                    && callChain is null && !tupleCall && clause.Body is Expr.Sequence { Items: [_, .., Expr.Tuple] })
                {
                    savedCalls = ReadSavedCallSequenceTuple(clause.Body, inputs);
                }

                (Pattern.Variable Variable, int Register)[]? bodyInputs = null;
                if (!tupleCall && consResult is null && tailCall is null && tailList is null && callChain is null && savedCalls is null
                    && tailConstruction is null && tupleResult is null && !arithmeticTail && returnedConstruction is null
                    && (!patternDispatch || guardClause.Body is not Expr.Literal
                    && (clause.Body is not Expr.Variable returned || !names.Contains(returned.Name))))
                {
                    bool canPlanBody = recordReturnDispatch && ReadEntryRecordCall(clause.Body) is not null
                        || (listDispatch || literalDispatch || guardDispatch || tupleBodyDispatch)
                        && clause.Body is Expr.Sequence or Expr.Case or Expr.If
                        || (literalDispatch || guardDispatch) && clause.Body is Expr.Try or Expr.Call
                        || guardDispatch && clause.Body is Expr.List { Items: [Expr.Call], Tail: null };
                    if (!canPlanBody)
                    {
                        return false;
                    }

                    var used = ReadUsedEntryParameters(guardClause with
                    {
                        Patterns = inputs.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                    });
                    bodyInputs = inputs.Select((name, register) => (Variable: new Pattern.Variable(name), Register: register))
                        .Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard
                            && (used is null || used.Contains(binding.Variable.Name))).ToArray();
                }

                if (tailList is { } constructed && clause.Guard is null && cons.Count == 0 && tuples is [var listTuple]
                    && listTuple.Fields.Count > 0 && fieldTests.Count == 0
                    && parameters[listTuple.Input] == VariableScopeNames.Wildcard)
                {
                    var tuple = tuples[0];
                    var last = tuple.Fields[^1];
                    parameters[tuple.Input] = parameters[last.Register];
                    parameters[last.Register] = VariableScopeNames.Wildcard;
                    inputs = parameters.ToArray();
                    tuple.Fields[^1] = (last.Index, tuple.Input);
                    tailList = (constructed.Call, constructed.Elements
                        .Select(element => element.Register == last.Register ? (tuple.Input, element.Value) : element)
                        .ToArray());
                }

                if (tailCall is { ConstructedArgument: null } nestedCall && clause.Guard is null
                    && fieldTests.Count == 0 && equalities.Count == 0
                    && cons is [var listPattern] && tuples is [var tuplePattern]
                    && tuplePattern.Input == listPattern.Head && tuplePattern.Fields.Count > 0
                    && inputs[tuplePattern.Input] == VariableScopeNames.Wildcard)
                {
                    var last = tuplePattern.Fields[^1];
                    bool reuseSharedHead = listPattern.Input == sharedHeadInput;
                    int register = reuseSharedHead ? listPattern.Head : listPattern.Input;
                    if (reuseSharedHead || nestedCall.Moves.Contains((register, last.Register)))
                    {
                        tuplePattern.Fields[^1] = (last.Index, register);
                        tailCall = nestedCall with
                        {
                            Moves = nestedCall.Moves
                                .Select(move => move.Source == last.Register ? (move.Destination, Source: register) : move)
                                .Where(move => move.Destination != move.Source)
                                .ToList()
                        };
                    }
                }

                if (tailCall is { ConstructedArgument: null } checkedTail && equalities.Count != 0)
                {
                    tailCall = checkedTail with
                    {
                        Moves = checkedTail.Moves.Where(move => !equalities.Any(test =>
                            (test.Input == move.Destination && test.Bound == move.Source)
                            || (test.Bound == move.Destination && test.Input == move.Source))).ToList()
                    };
                }

                if (consResult is not null && fieldTests.Count == 0 && equalities.Count == 0 && cons is [var sourceList]
                    && tuples is [var sourceTuple] && sourceTuple.Input == sourceList.Head && sourceTuple.Fields.Count > 0)
                {
                    var last = sourceTuple.Fields[^1];
                    if (mixedPrefix is not null && consResult.HeadCall is { ExtractTail: false } computed
                        && computed.Call.Call.Arguments is [Expr.Variable argument] && argument.Name == inputs[last.Register]
                        && !consResult.Saved.Contains(last.Register)
                        && (clause.Guard is null || !LocalFunctionEntries.Walk(clause.Guard).OfType<Expr.Variable>()
                            .Any(variable => variable.Name == argument.Name)))
                    {
                        sourceTuple.Fields.RemoveAt(sourceTuple.Fields.Count - 1);
                        consResult = consResult with
                        {
                            Saved = computed.Kept.Union(consResult.Saved).ToArray(),
                            HeadCall = computed with
                            {
                                Call = computed.Call with { Moves = [] },
                                DeferredArgument = (sourceTuple.Input, last.Index)
                            }
                        };
                    }
                    else if (clause.Guard is null && consResult.HeadCall is null && consResult.Saved is [var saved] && saved == last.Register
                        && !consResult.Call.Call.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == inputs[saved]))
                    {
                        sourceTuple.Fields.RemoveAt(sourceTuple.Fields.Count - 1);
                        consResult = consResult with { Saved = [sourceTuple.Input], DeferredField = last.Index };
                    }
                    else if (clause.Guard is null && inputs[sourceTuple.Input] == VariableScopeNames.Wildcard)
                    {
                        inputs[sourceTuple.Input] = inputs[last.Register];
                        inputs[last.Register] = VariableScopeNames.Wildcard;
                        sourceTuple.Fields[^1] = (last.Index, sourceTuple.Input);
                        consResult = ReadConsCallResult(clause.Body, inputs);
                    }
                }

                if (consResult?.HeadCall is not null && sharedConsInput < 0 && mixedPrefix is null
                    && clause.Guard is null && tuples.Count == 0 && equalities.Count == 0
                    && cons is [var directList] && directList.Head >= 0 && directList.Tail >= 0
                    && clause.Patterns[directList.Input] is Pattern.List { Items: [Erlang.Pattern.Variable] })
                {
                    var directInputs = (string[])inputs.Clone();
                    directInputs[directList.Input] = inputs[directList.Tail];
                    directInputs[directList.Tail] = VariableScopeNames.Wildcard;
                    if (ReadConsCallResult(clause.Body, directInputs, directList.Head, directList.Input)
                        is { HeadCall.ExtractTail: true } directResult)
                    {
                        inputs = directInputs;
                        consResult = directResult;
                    }
                }

                branches.Add((guardClause, inputs, literals, cons, tuples, fieldTests, equalities,
                    tupleCall, consResult, tailCall, callChain, tailList, tailConstruction, tupleResult, bodyInputs, deferredCons,
                    arithmeticTail, returnedConstruction, savedCalls));

                bool BindListVariable(Pattern.Variable variable, int register)
                {
                    if (variable.Name == VariableScopeNames.Wildcard)
                    {
                        return true;
                    }

                    if (names.Add(variable.Name))
                    {
                        parameters[register] = variable.Name;
                        return true;
                    }

                    int bound = parameters.IndexOf(variable.Name);
                    if (bound < 0)
                    {
                        return false;
                    }

                    equalities.Add((register, bound));
                    return true;
                }

                int AddListBinding(Pattern.Variable variable)
                {
                    if (variable.Name == VariableScopeNames.Wildcard)
                    {
                        return -1;
                    }

                    int register = parameters.Count;
                    parameters.Add(VariableScopeNames.Wildcard);
                    return BindListVariable(variable, register) ? register : -2;
                }

                bool ReadConsTuple(int register, Pattern head, Pattern.Tuple shape)
                {
                    if (head is AliasPattern alias
                        && (!TryReadPatternAlias(alias, out var name, out _) || !BindListVariable(name, register)))
                    {
                        return false;
                    }

                    return ReadTuple(register, shape);
                }

                bool ReadListPrefix(int input, Pattern.List prefix)
                {
                    int current = input;
                    for (int offset = 0; offset < prefix.Items.Count; offset++)
                    {
                        Pattern head = prefix.Items[offset];
                        string headName = (head as Pattern.Variable)?.Name ?? VariableScopeNames.Wildcard;
                        if (headName != VariableScopeNames.Wildcard && !names.Add(headName))
                        {
                            return false;
                        }

                        int headRegister = parameters.Count;
                        parameters.Add(headName);
                        bool last = offset == prefix.Items.Count - 1;
                        var tail = last ? (Pattern.Variable)prefix.Tail! : null;
                        if (tail is { Name: not VariableScopeNames.Wildcard } && !names.Add(tail.Name))
                        {
                            return false;
                        }

                        int tailRegister = offset == 0 && input == sharedHeadInput ? input : parameters.Count;
                        if (tailRegister == parameters.Count)
                        {
                            parameters.Add(tail?.Name ?? VariableScopeNames.Wildcard);
                        }

                        cons.Add((current, headRegister, tailRegister, (head as Pattern.Literal)?.Value));
                        if (head is not (Erlang.Pattern.Literal or Erlang.Pattern.Variable) && (head is not Pattern.Tuple tuple
                            || !ReadTuple(headRegister, tuple)))
                        {
                            return false;
                        }

                        current = tailRegister;
                    }

                    return true;
                }

                bool ReadTuple(int input, Pattern.Tuple tuple)
                {
                    var fields = new List<(int Index, int Register)>();
                    Atom? tag = HasTupleTag(tuple, out Atom tupleTag) ? tupleTag : null;
                    for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
                    {
                        Term? literal = tuple.Items[index] switch
                        {
                            Pattern.Literal value => value.Value,
                            Pattern.List { Items.Count: 0, Tail: null } => Nil.Value,
                            _ => null
                        };
                        if (literal is not null && (listDispatch ? cons.Count != 0 : tupleBodyDispatch))
                        {
                            int register = parameters.Count;
                            parameters.Add(VariableScopeNames.Wildcard);
                            fields.Add((index, register));
                            fieldTests.Add(register, (null, literal));
                            continue;
                        }

                        if (tuple.Items[index] is not Pattern.Variable variable)
                        {
                            return false;
                        }

                        if (variable.Name == VariableScopeNames.Wildcard || unusedTupleFields.Contains(variable.Name))
                        {
                            continue;
                        }

                        if (!names.Add(variable.Name))
                        {
                            int bound = parameters.IndexOf(variable.Name);
                            if (!listDispatch || cons.Count == 0 || bound < 0)
                            {
                                return false;
                            }

                            int register = parameters.Count;
                            parameters.Add(VariableScopeNames.Wildcard);
                            fields.Add((index, register));
                            fieldTests.Add(register, (bound, null));
                            continue;
                        }

                        fields.Add((index, AddField(variable)));
                    }

                    tuples.Add((input, tuple.Items.Count, tag, fields));
                    return true;
                }

                int AddField(Pattern.Variable field)
                {
                    if (field.Name == VariableScopeNames.Wildcard)
                    {
                        return -1;
                    }

                    parameters.Add(field.Name);
                    return parameters.Count - 1;
                }
            }

            bool hasConsResult = branches.Any(branch => branch.ConsResult is not null);
            bool sharedListTail = sharedHeadInput >= 0 && branches[^2].TailCall is not null
                && (reorderedListInput >= 0 || !branches.Any(branch => branch.Equalities.Any(test =>
                    test.Bound < function.Arity && test.Bound != sharedHeadInput
                    && ReadPrivateIntegerBounds(sourceFunction, test.Bound) is not null)));
            int[] order = Enumerable.Range(0, branches.Count).ToArray();
            int[]? dispatchLabels = null;
            int dispatchedCount = 0;
            if (literalDispatch && branches.Count >= 2)
            {
                var arms = branches.TakeWhile(branch => branch.Clause.Guard is null && branch.Literals.Count == 1).ToArray();
                if (arms.Length >= 2 && arms[0].Literals[0] is { Index: var input, Value: Atom or Integer }
                    && arms.All(branch => branch.Literals[0].Index == input
                        && branch.Literals[0].Value.GetType() == arms[0].Literals[0].Value.GetType())
                    && arms.Select(branch => branch.Literals[0].Value).Distinct().Count() == arms.Length)
                {
                    var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                    int[] sorted = order.Take(arms.Length).OrderBy(index => arms[index].Literals[0].Value, comparer).ToArray();
                    bool hasFallback = arms.Length < branches.Count;
                    var finite = !hasFallback && arms[0].Literals[0].Value is Atom
                        && arms.All(arm => arm.Equalities.Count == 0)
                        ? ReadFiniteAtomArguments(sourceFunction, input) : null;
                    if (finite is not null
                        && finite.All(atom => arms.Any(arm => arm.Literals[0].Value.Equals(atom))))
                    {
                        dispatchedCount = arms.Length;
                        sorted = sorted.Where(index => finite.Contains((Atom)arms[index].Literals[0].Value)).ToArray();
                        order = sorted.Reverse().ToArray();
                        if (order.Length > 1)
                        {
                            dispatchLabels = new int[arms.Length];
                            foreach (int index in order)
                            {
                                dispatchLabels[index] = Label();
                            }

                            int failure = dispatchLabels[sorted[0]];
                            if (order.Length == 2)
                            {
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, input);
                                owner.EmitPatternOperand(arms[sorted[1]].Literals[0].Value);
                            }
                            else
                            {
                                owner.EmitMembershipSelect(
                                    input,
                                    failure,
                                    sorted.Skip(1).Select(index => (arms[index].Literals[0].Value, dispatchLabels[index])),
                                    AtomTypeBits
                                );
                            }
                        }
                    }
                    else if (branches.Count >= 3)
                    {
                        dispatchedCount = arms.Length;
                        if (branches.Skip(dispatchedCount).Any(branch => branch.Clause.Guard is not null))
                        {
                            sorted.Reverse().ToArray().CopyTo(order, 0);
                        }

                        dispatchLabels = new int[dispatchedCount + (hasFallback ? 1 : 0)];
                        foreach (int index in order.Take(dispatchLabels.Length))
                        {
                            dispatchLabels[index] = Label();
                        }

                        int failure = hasFallback ? dispatchLabels[^1] : functionInfo;
                        int selectorType = arms[0].Literals[0].Value is Atom ? ReadAtomEntryType(sourceFunction, input) : 0;
                        owner.EmitMembershipSelect(
                            input,
                            failure,
                            sorted.Select(index => (arms[index].Literals[0].Value, dispatchLabels[index])),
                            selectorType
                        );
                    }
                }
            }

            EntryTupleSelection?[]? tupleSelections = null;
            if (tupleDispatchSupported && !listDispatch)
            {
                tupleSelections = new EntryTupleSelection?[branches.Count];
                for (int start = 0; start < branches.Count; start++)
                {
                    if (ReadTupleSelectionArm(start) is not { } first)
                    {
                        continue;
                    }

                    var arms = new List<(int Branch, int Arity, Term Key)> { (start, first.Arity, first.Key) };
                    var keys = new HashSet<(int Arity, Term Key)> { (first.Arity, first.Key) };
                    int end = start + 1;
                    while (end < branches.Count && ReadTupleSelectionArm(end) is { } next
                        && next.Input == first.Input && next.SelectsTag == first.SelectsTag
                        && (first.SelectsTag || next.Arity == first.Arity && Equals(next.Tag, first.Tag))
                        && next.Selector == first.Selector && next.Fields.SequenceEqual(first.Fields)
                        && next.Key.GetType() == first.Key.GetType() && keys.Add((next.Arity, next.Key)))
                    {
                        arms.Add((end++, next.Arity, next.Key));
                    }

                    if (arms.Count == 1)
                    {
                        continue;
                    }

                    var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                    var groups = new List<EntryTupleAritySelection>();
                    int position = start;
                    foreach (var group in arms.GroupBy(arm => arm.Arity).OrderByDescending(group => group.Key))
                    {
                        var sorted = group.OrderBy(arm => arm.Key, comparer).ToArray();
                        groups.Add(new EntryTupleAritySelection(position, group.Key, sorted.Select(arm => arm.Key).ToArray()));
                        foreach (var arm in sorted.Reverse())
                        {
                            order[position++] = arm.Branch;
                        }
                    }

                    var selection = new EntryTupleSelection(start, end, first, groups.ToArray());
                    Array.Fill(tupleSelections, selection, start, end - start);
                    start = end - 1;
                }
            }

            EntryTupleSelectionArm? ReadTupleSelectionArm(int index)
            {
                var branch = branches[index];
                if (branch.Literals.Count != 0 || branch.Equalities.Count != 0
                    || branch.Tuples.Any(tuple => tuple.Fields.Any(field => field.Register < function.Arity)))
                {
                    return null;
                }

                if (branch.FieldTests.Count == 0 && function.Arity < FirstReservedXRegister
                    && branch.Tuples.FirstOrDefault(tuple => tuple.Tag is not null) is { Tag: { } tag } tagged)
                {
                    return new EntryTupleSelectionArm(tagged.Input, tagged.Arity, null, [(0, function.Arity)],
                        function.Arity, tag, SelectsTag: true);
                }

                if (!tupleBodyDispatch || branch.FieldTests.Count != 1 || branch.Tuples is not [var tuple])
                {
                    return null;
                }

                var test = branch.FieldTests.Single();
                var (register, literal) = test.Value;
                return register is null && literal is Atom or Integer && tuple.Fields.Any(field => field.Register == test.Key)
                    ? new EntryTupleSelectionArm(tuple.Input, tuple.Arity, tuple.Tag, tuple.Fields, test.Key, literal, SelectsTag: false)
                    : null;
            }

            if (terminalConsInput < 0 && !sharedListTail && (listDispatch || tupleDispatch)
                && tupleSelections?.Any(selection => selection is not null) != true
                && !hasConsResult && !branches.Any(branch => branch.TupleCall || branch.CallChain is not null
                    || branch.SavedCalls is not null || branch.BodyInputs is not null)
                && !branches.Any(branch => branch.TailCall is not null
                    && (branch.Clause.Guard is not null || branch.Cons.Any(cons => cons.HeadLiteral is not null)
                        || branch.Cons.Count > 0 && branch.Tuples.Count > 0
                        || IsEntryControlTransfer(branch.TailCall.Call, branch.TailCall.Module)))
                && !branches.Any(branch => branch.TailList is not null || branch.TailConstruction is not null
                    || branch.TailCall?.ConstructedArgument is not null || branch.ArithmeticTail))
            {
                return false;
            }

            int arityCount = 0;
            if (mixedPrefix is { } tuplePrefix && branches.Take(tuplePrefix.Count).All(branch =>
                    branch.Cons is [var cons] && branch.Tuples is [var tuple] && tuple.Input == cons.Head
                        && branch.Parameters.Length < FirstReservedXRegister)
                && branches.Take(tuplePrefix.Count).Select(branch => branch.Tuples[0].Arity).Distinct().Count() == tuplePrefix.Count)
            {
                arityCount = tuplePrefix.Count;
            }

            bool splitTupleAndAtom = reuseMixedTail && mixedPrefix is { Count: > 2 }
                && branches[0] is
                {
                    Clause.Guard: null,
                    Cons: [var tupleList],
                    Tuples: [{ Tag: not null, Fields.Count: 0 } headTupleShape],
                    Equalities.Count: 0
                }
                && headTupleShape.Input == tupleList.Head
                && branches[1] is { Cons: [{ HeadLiteral: Atom }], Tuples.Count: 0 };

            var literalGroups = mixedPrefix is { } prefixPlan && arityCount == 0 ? ReadConsLiteralGroups(function, prefixPlan) : [];
            var groupedBranches = new ConsLiteralGroup?[branches.Count];
            if (twoCellPrefix is { } prefixKeys)
            {
                Array.Sort(order, 0, prefixKeys.Keys.Length, Comparer<int>.Create((left, right) =>
                    TermOrder.Compare(prefixKeys.Keys[right], prefixKeys.Keys[left], exact: true)));
            }
            else if (arityCount > 0)
            {
                Array.Sort(order, 0, arityCount, Comparer<int>.Create((left, right) =>
                    branches[right].Tuples[0].Arity.CompareTo(branches[left].Tuples[0].Arity)));
            }

            foreach (var group in literalGroups)
            {
                for (int index = group.Start; index < group.End; index++)
                {
                    groupedBranches[index] = group;
                    order[index] = group.Order[index - group.Start];
                }
            }

            int[]? mixedLabels = mixedPrefix is null ? null : new int[branches.Count];
            int[]? literalLabels = mixedPrefix is null ? null : new int[branches.Count];
            if (mixedLabels is not null)
            {
                for (int index = 0; index < branches.Count; index++)
                {
                    if (index > 0 || arityCount > 0 || twoCellPrefix is not null)
                    {
                        mixedLabels[index < arityCount || index < twoCellPrefix?.Keys.Length ? order[index] : index] = Label();
                    }

                    if (groupedBranches[index] is { } group)
                    {
                        foreach (int branch in group.Order)
                        {
                            literalLabels![branch] = Label();
                        }

                        index = group.End - 1;
                    }
                }
            }

            int[]? sharedGuardFailures = sharedConsInput < 0
                ? null
                : Enumerable.Range(0, branches.Count - 2).Select(_ => Label()).ToArray();
            int nonCons = mixedPrefix is { } sharedPrefix ? mixedLabels![sharedPrefix.Count]
                : sharedGuardFailures is null ? 0 : Label();
            int tupleFailure = arityCount > 0 && arityCount + 1 == branches.Count
                && ListEntryTest(function.Clauses[^1].Patterns[mixedPrefix!.Value.Input]) == IsNil
                ? functionInfo : nonCons;
            var emptyReturn = hasConsResult && nonCons != 0 && mixedPrefix is null ? listReturn : null;

            int atomSuffixStart = -1;
            int atomSuffixInput = -1;
            int atomSuffixEnd = branches.Count - 1;
            int[]? atomSuffixLabels = null;
            const int atomSuffixFallback = int.MinValue;
            int atomSuffixOperandStart = 0;
            if (tupleDispatchSupported && !listDispatch && branches.Count >= 4
                && tupleSelections?.Any(selection => selection is not null) != true
                && branches[^1].Clause.Guard is null && HasUnconditionalParameters(branches[^1].Clause)
                && AttributeLiteral.TryNormalizeConstant(branches[^1].Clause.Body, out _))
            {
                var keys = new HashSet<Term>();
                for (int index = atomSuffixEnd - 1; index > 0; index--)
                {
                    var candidate = branches[index];
                    if (candidate.Clause.Guard is not null || candidate.Literals is not [{ Index: var input, Value: Atom key }]
                        || candidate.Tuples.Count != 1 || candidate.Equalities.Count != 0
                        || (atomSuffixInput >= 0 && atomSuffixInput != input) || !keys.Add(key))
                    {
                        break;
                    }

                    atomSuffixInput = input;
                    atomSuffixStart = index;
                }

                if (atomSuffixStart >= 0 && atomSuffixEnd - atomSuffixStart >= 2)
                {
                    atomSuffixLabels = new int[branches.Count];
                }
            }

            var bodyFrames = new Dictionary<int, int>();
            var savedCallFailures = new List<Action>();
            var tupleMatchFailures = new List<(int Label, BeamSourceLocation Location)>();
            var previousConsCall = location;
            bool extractedListCase = false;
            int tupleSelectionFailure = 0;
            int[]? tupleSelectionLabels = null;
            int[]? tupleArityLabels = null;
            int tupleArityIndex = 0;
            if (twoCellPrefix is { } sharedCells)
            {
                int input = sharedCells.Input;
                int head = function.Arity;
                int key = head + 1;
                int fallback = mixedLabels![sharedCells.Keys.Length];
                owner.EmitNonemptyList(input, nonCons);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    head,
                    XRegisterTag,
                    input
                );
                owner.Op(ExactEqualTest, LabelTag, fallback);
                owner.Operand(XRegisterTag, head);
                owner.LiteralOperand(sharedCells.Head);
                owner.EmitNonemptyList(input, fallback);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    key,
                    XRegisterTag,
                    key + 1
                );
                owner.EmitMembershipSelect(
                    key,
                    fallback,
                    order.Take(sharedCells.Keys.Length).Reverse()
                        .Select(branch => (sharedCells.Keys[branch], mixedLabels[branch]))
                );
            }

            BeamSourceLocation? tupleTailLine = null;
            for (int position = 0; position < order.Length; position++)
            {
                var previousTupleTailLine = tupleTailLine;
                tupleTailLine = null;
                int branch = order[position];
                bool selectedAtom = atomSuffixLabels is not null && position >= atomSuffixStart && position < atomSuffixEnd;
                if (selectedAtom && position == atomSuffixStart)
                {
                    for (int index = atomSuffixStart; index < atomSuffixEnd; index++)
                    {
                        atomSuffixLabels![order[index]] = Label();
                    }

                    atomSuffixOperandStart = owner.deferredLabelOperands.Count;
                    owner.EmitMembershipSelect(
                        atomSuffixInput,
                        atomSuffixFallback,
                        Enumerable.Range(atomSuffixStart, atomSuffixEnd - atomSuffixStart)
                            .OrderBy(index => branches[index].Literals[0].Value,
                                Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                            .Select(index => (branches[index].Literals[0].Value, atomSuffixLabels![index]))
                    );
                }

                if (selectedAtom)
                {
                    Mark(atomSuffixLabels![branch]);
                }
                else if (atomSuffixLabels is not null && position == atomSuffixEnd)
                {
                    int fallback = Label();
                    for (int index = atomSuffixOperandStart; index < owner.deferredLabelOperands.Count; index++)
                    {
                        var branchOperand = owner.deferredLabelOperands[index];
                        if (branchOperand.Target == atomSuffixFallback)
                        {
                            owner.deferredLabelOperands[index] = (branchOperand.Position, fallback);
                        }
                    }

                    Mark(fallback);
                }

                bool arityBranch = branch < arityCount;
                if (arityBranch && position == 0)
                {
                    var (input, head, tail, _) = branches[branch].Cons[0];
                    owner.EmitNonemptyList(input, nonCons);
                    owner.EmitListParts(input, XRegisterTag, head, XRegisterTag, tail);
                    var cases = order.Take(arityCount).Reverse()
                        .Select(index => (branches[index].Tuples[0].Arity, mixedLabels![index])).ToArray();
                    owner.EmitTupleArityDispatch(tupleFailure, cases, head);
                }

                var literalGroup = groupedBranches[branch];
                bool dispatchEntry = literalGroup is not null && literalGroup.Order[0] == branch;
                bool selectedLiteral = branch < dispatchedCount;
                var tupleSelection = tupleSelections?[position];
                if (tupleSelection is { } selection)
                {
                    if (position == selection.Start)
                    {
                        tupleArityIndex = 0;
                        bool mixedArities = selection.Groups.Length > 1;
                        tupleArityLabels = mixedArities ? new int[selection.Groups.Length] : null;
                        tupleSelectionLabels = new int[selection.End - selection.Start];
                        for (int index = 0; index < selection.Groups.Length; index++)
                        {
                            var group = selection.Groups[index];
                            if (tupleArityLabels is not null)
                            {
                                tupleArityLabels[index] = Label();
                            }

                            if (!mixedArities || group.Keys.Length > 1)
                            {
                                for (int arm = 0; arm < group.Keys.Length; arm++)
                                {
                                    tupleSelectionLabels[group.Start - selection.Start + arm] = Label();
                                }
                            }
                        }

                        tupleSelectionFailure = selection.End < branches.Count ? Label() : functionInfo;
                        if (tupleArityLabels is { } arityLabels)
                        {
                            owner.EmitTupleArityDispatch(tupleSelectionFailure,
                                selection.Groups.Select((group, index) => (group.Arity, arityLabels[index])).Reverse().ToArray(),
                                selection.Shape.Input);
                        }
                    }

                    var currentArity = selection.Groups[tupleArityIndex];
                    if (position == currentArity.Start)
                    {
                        var shape = selection.Shape;
                        if (tupleArityLabels is not null)
                        {
                            Mark(tupleArityLabels[tupleArityIndex]);
                        }
                        else if (shape.Tag is { } tag)
                        {
                            EmitDetailRecordTest(shape.Input, currentArity.Arity, tag, tupleSelectionFailure);
                        }
                        else
                        {
                            EmitTupleShapeTest(XRegisterTag, shape.Input, currentArity.Arity, tupleSelectionFailure);
                        }

                        foreach (var (field, register) in shape.Fields)
                        {
                            owner.EmitTuplePart(shape.Input, field, register);
                        }

                        if (currentArity.Keys is [var single])
                        {
                            owner.Op(ExactEqualTest, LabelTag, tupleSelectionFailure);
                            owner.Operand(XRegisterTag, shape.Selector);
                            owner.LiteralOperand(single);
                        }
                        else
                        {
                            int last = currentArity.Start - selection.Start + currentArity.Keys.Length - 1;
                            int[] labels = tupleSelectionLabels!;
                            owner.EmitMembershipSelect(shape.Selector, tupleSelectionFailure,
                                currentArity.Keys.Select((key, index) => (key, labels[last - index])));
                        }
                    }

                    int armLabel = tupleSelectionLabels![position - selection.Start];
                    if (armLabel != 0)
                    {
                        Mark(armLabel);
                    }

                    if (position + 1 == currentArity.Start + currentArity.Keys.Length)
                    {
                        tupleArityIndex++;
                    }
                }
                else if (dispatchLabels is not null && branch < dispatchLabels.Length)
                {
                    Mark(dispatchLabels[branch]);
                }
                else if (mixedLabels is not null && literalGroup is null && (branch > 0 || arityBranch || twoCellPrefix is not null))
                {
                    Mark(mixedLabels[branch]);
                }
                else if (dispatchEntry && literalGroup!.Start > 0)
                {
                    Mark(mixedLabels![literalGroup.Start]);
                }

                if (branch == 1 && terminalConsInput >= 0
                    && branches[1].Clause.Body is Expr.Literal { Value: Atom { Name: "true" } }
                    && branches[2].Clause.Body is Expr.Literal { Value: Atom { Name: "false" } })
                {
                    owner.Op(BifTwo, LabelTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
                    owner.Operand(XRegisterTag, terminalConsInput);
                    owner.LiteralOperand(Nil.Value);
                    owner.Operand(XRegisterTag, 0);
                    owner.instructions.WriteByte(Return);
                    return true;
                }

                var (clause, parameters, literals, cons, tuples, fieldTests, equalities,
                    tupleCall, consResult, tailCall, callChain, tailList, tailConstruction, tupleResult, bodyInputs, deferredCons,
                    arithmeticTail, returnedConstruction, savedCalls) = branches[branch];
                var entryPatterns = function.Clauses[branch].Patterns;
                var recordUpdate = recordReturnDispatch && bodyInputs is not null && clause.Guard is null
                    && tuples.Count > 0 && cons.Count == 0 && fieldTests.Count == 0 && equalities.Count == 0
                    ? ReadEntryRecordUpdate(clause.Body, entryPatterns, tuples.SelectMany(tuple =>
                        tuple.Fields.Select(field => (parameters[field.Register], tuple.Input, field.Index))))
                    : null;
                bool hasNext = branch + 1 < branches.Count;
                (int Input, List<(int Index, int Register)> Fields)? protectedEntryTuple = null;
                if (selectedAtom && bodyInputs is not null && tuples is [var protectedTuple]
                    && protectedTuple.Tag is null && fieldTests.Count == 0
                    && TryEmitProtectedTaggedResult(
                        clause.Body,
                        parameters,
                        (protectedTuple.Input, protectedTuple.Fields),
                        validateOnly: true
                    ))
                {
                    protectedEntryTuple = (protectedTuple.Input, protectedTuple.Fields);
                }

                EntryConstructedArgument? projectedReturn = null;
                if (selectedAtom && clause.Body is Expr.Tuple
                    && tuples is [{ Tag: null, Fields: [var projectedField] }]
                    && fieldTests.Count == 0
                    && ReadEntryConstructedArgument(clause.Body, 0, parameters) is { } projectedResult
                    && projectedResult.Operands.All(operand => operand.Source is null || operand.Source == projectedField.Register))
                {
                    projectedReturn = projectedResult with
                    {
                        Operands = projectedResult.Operands.Select(operand =>
                            operand.Source.HasValue ? (Source: (int?)0, operand.Constant) : operand).ToArray()
                    };
                }
                var elementCase = bodyInputs is not null && cons.Count != 0
                    ? ReadListElementCase(clause.Body, parameters) : null;
                if (elementCase is { } elementPlan)
                {
                    LabelListElementCase(elementPlan.Branch);
                }

                int failure = selectedAtom ? atomSuffixFallback : tupleSelection is not null ? tupleSelectionFailure
                    : arityBranch ? tupleFailure : !hasNext || selectedLiteral ? functionInfo : mixedLabels is not null
                    ? mixedLabels[literalGroup?.End ?? (branch + 1)]
                    : sharedGuardFailures is null
                    ? Label() : branch < sharedGuardFailures.Length ? sharedGuardFailures[branch] : nonCons;
                EntryRegisterCase? registerCase = null;
                int? listCaseFailure = null;
                int fieldLimit = int.MaxValue;
                bool returnedTupleField = false;
                int tupleCheckFailure = failure;
                (int Register, int Field)? deferredTupleCall = null;
                if (tupleCall && clause.Guard is null && fieldTests.Count == 0 && equalities.Count == 0
                    && tuples is [var callTuple] && callTuple.Fields.Count > 0
                    && clause.Body is Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Value: Expr.Call { Arguments: [Expr.Variable operand] } },
                            Expr.Tuple result
                        ]
                    })
                {
                    int last = callTuple.Fields.Count - 1;
                    var field = callTuple.Fields[last];
                    if (field.Register == parameters.Length - 1
                        && parameters[field.Register] == operand.Name
                        && !result.Items.OfType<Expr.Variable>().Any(item => item.Name == operand.Name))
                    {
                        fieldLimit = last;
                        deferredTupleCall = (callTuple.Input, field.Index);
                    }
                }
                if (elementCase is null && bodyInputs is not null
                    && ReadCallSelectorCase(clause.Body, out _) is { Value: Expr.Call entrySelectorCall })
                {
                    var entryTupleFields = tuples.SelectMany(tuple => tuple.Fields).Select(item => item.Register).ToHashSet();
                    var prepared = clause with
                    {
                        Guard = null,
                        Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                    };
                    if (entrySelectorCall.Arguments is [Expr.Variable firstSelectorArgument, ..]
                        && clause.Guard is null && fieldTests.Count == 0 && equalities.Count == 0
                        && cons is [var listHead] && tuples is [var headTuple] && headTuple.Input == listHead.Head)
                    {
                        int index = headTuple.Fields.FindIndex(item => parameters[item.Register] == firstSelectorArgument.Name);
                        if (index >= 0)
                        {
                            var candidate = headTuple.Fields[index];
                            registerCase = ReadEntryRegisterCase(
                                prepared,
                                (headTuple.Input, candidate.Index, candidate.Register),
                                (listHead.Head, listHead.Tail),
                                entryTupleFields: entryTupleFields
                            );
                            if (registerCase is { Deferred: { } projection } deferredCase)
                            {
                                var savedFields = headTuple.Fields.Where(item => deferredCase.Selector.Saved.Contains(item.Register))
                                    .ToArray();
                                var omitted = savedFields.Select(item => item.Register).Append(candidate.Register).ToHashSet();
                                omitted.UnionWith(headTuple.Fields.Where(item => !bodyInputs.Any(binding => binding.Register == item.Register))
                                    .Select(item => item.Register));
                                int lastLive = entrySelectorCall.Arguments.Skip(1).OfType<Expr.Variable>()
                                    .Select(argument => Array.IndexOf(parameters, argument.Name))
                                    .Concat(deferredCase.Selector.Saved.Where(register => !omitted.Contains(register)))
                                    .Append(projection.Tuple).Max();
                                if (lastLive >= omitted.Min() || projection.Tuple == deferredCase.ProjectionDestination)
                                {
                                    registerCase = null;
                                }
                                else
                                {
                                    registerCase = deferredCase with { SavedTupleFields = savedFields };
                                    if (branches.Count == 2 && listHead.Input != listHead.Head && listHead.Input != listHead.Tail
                                        && ListEntryTest(function.Clauses[^1].Patterns[listHead.Input]) == IsNil)
                                    {
                                        tupleCheckFailure = functionInfo;
                                    }
                                }
                            }
                            else
                            {
                                registerCase = null;
                            }
                        }
                    }

                    if (registerCase is null && clause.Guard is null && tuples.Count == 0 && equalities.Count == 0
                        && mixedPrefix is null && sharedConsInput < 0
                        && cons is [{ HeadLiteral: null, Head: >= 0, Tail: >= 0 } list]
                        && list.Head != list.Input
                        && (list.Tail == list.Input || parameters[list.Input] == VariableScopeNames.Wildcard))
                    {
                        var extraction = new CallListExtraction(list.Input, list.Head, list.Tail);
                        registerCase = ReadEntryRegisterCase(prepared, extraction: extraction);
                    }

                    registerCase ??= ReadEntryRegisterCase(
                        prepared,
                        entryTupleFields: entryTupleFields
                    );
                    if (registerCase is not null)
                    {
                        // Only the call-sequence body emits its deferred extraction.
                        deferredCons = null;
                    }

                    if (hasNext && registerCase is { Extraction: not null, Case: { Opcode: ExactEqualTest, Tests: null } })
                    {
                        listCaseFailure = failure;
                        failure = Label();
                    }
                }

                if (!hasNext && emptyReturn is { } empty)
                {
                    EmitEmptyListReturn(empty, nonCons, functionInfo);
                    break;
                }

                if (nonCons != 0 && !hasNext && mixedLabels is null)
                {
                    Mark(nonCons);
                }

                if (reuseMixedTail && mixedPrefix is { } emptyPrefix && branch == emptyPrefix.Count)
                {
                    owner.EmitNilTest(XRegisterTag, emptyPrefix.Input, functionInfo);
                }

                foreach (var (index, literal) in selectedLiteral || selectedAtom ? [] : literals)
                {
                    if (literal is Nil)
                    {
                        if (reuseMixedTail && mixedPrefix is { } listPrefix && index == listPrefix.Input
                            && branch >= listPrefix.Count)
                        {
                            continue;
                        }

                        if (reorderedListInput == index && branch == 1 && branches[0].Tuples.Count == 0
                            && HasProperListEntry(sourceFunction, index))
                        {
                            continue;
                        }

                        owner.Op(IsNil, LabelTag, failure);
                        if (reuseMixedTail && mixedPrefix is { } sharedList && branch >= sharedList.Count
                            && index != sharedList.Input && HasProperListEntry(sourceFunction, index, followForwarding: true))
                        {
                            owner.EmitTypedRegister(index, ProperListTypeBits);
                        }
                        else
                        {
                            owner.Operand(XRegisterTag, index);
                        }

                        continue;
                    }

                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, index);
                    owner.LiteralOperand(literal);
                }

                int heapWords = tailList is { } listPlan
                    ? (Array.FindLastIndex(listPlan.Elements, element => element.Register.HasValue) + 1) * ConsHeapWords
                    : tailConstruction is { } constructedPlan && equalities.Count == 0
                        ? constructedPlan.Plan.Constructions.Sum(item => item.HeapWords)
                        : tailCall?.ConstructedArgument?.HeapWords ?? 0;
                bool heapChecked = false;
                int checkedTagRegister = -1;
                bool nestedPrefix = function.Arity == 1 && cons.Count > 1;
                // In a prefix, X0 may be reused for the first tail. Only the first test can branch to the non-cons case.
                for (int depth = 0; depth < cons.Count; depth++)
                {
                    var (input, head, tail, headLiteral) = cons[depth];
                    bool shared = (!nestedPrefix || depth == 0) && (input == sharedConsInput
                        || mixedPrefix is { } prefix && input == prefix.Input && branch < prefix.Count);
                    bool firstCons = twoCellPrefix is null && (mixedPrefix is null ? branch == 0 : position == 0);
                    if (!arityBranch && (!shared || firstCons))
                    {
                        if ((nestedPrefix && depth != 0) || !firstCons
                            || !HasProvenConsEntry(reorderedListInput >= 0 ? sourceFunction : function, input))
                        {
                            owner.Op(IsNonemptyList, LabelTag, shared ? nonCons : failure);
                            owner.Operand(XRegisterTag, input);
                        }
                        if (heapWords > 0 && headLiteral is null && clause.Guard is null && tuples.Count == 0 && cons.Count == 1)
                        {
                            owner.Op(TestHeap, BeamWire.UnsignedTag, heapWords);
                            owner.Operand(BeamWire.UnsignedTag, function.Arity);
                            heapChecked = true;
                        }

                        if (deferredCons is not null || consResult?.HeadCall is { ExtractTail: true }
                            || callChain?.ListExtraction is not null || savedCalls?.ListExtraction is not null
                            || registerCase?.Extraction is not null)
                        {
                            // The result frame receives the tail during its own extraction.
                        }
                        else if (head >= 0 && tail >= 0)
                        {
                            owner.EmitListParts(input, XRegisterTag, head, XRegisterTag, tail);
                        }
                        else if (head >= 0 || tail >= 0)
                        {
                            owner.Op(head >= 0 ? GetHead : GetTail, XRegisterTag, input);
                            owner.Operand(XRegisterTag, Math.Max(head, tail));
                        }
                    }

                    if (literalGroup is not null)
                    {
                        if (dispatchEntry)
                        {
                            var entries = literalGroup.Order.OrderBy(index => index,
                                Comparer<int>.Create((left, right) => TermOrder.Compare(
                                    branches[left].Cons[0].HeadLiteral!, branches[right].Cons[0].HeadLiteral!, exact: true)))
                                .Select(index => (branches[index].Cons[0].HeadLiteral!, literalLabels![index]));
                            owner.EmitMembershipSelect(head, mixedLabels![literalGroup.End], entries);
                        }

                        Mark(literalLabels![branch]);
                    }
                    else if (headLiteral is not null)
                    {
                        owner.Op(ExactEqualTest, LabelTag, failure);
                        owner.Operand(XRegisterTag, head);
                        owner.LiteralOperand(headLiteral);
                    }

                    if (nestedPrefix)
                    {
                        foreach (var tuple in tuples.Where(tuple => tuple.Input == head))
                        {
                            EmitTuple(tuple);
                        }
                    }
                }

                foreach (var tuple in tupleSelection is null || tupleSelection.Shape.SelectsTag
                    ? tuples.Where(tuple => !nestedPrefix || cons.All(item => item.Head != tuple.Input)) : [])
                {
                    EmitTuple(tuple);
                }

                foreach (var (input, bound) in equalities)
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, input);
                    owner.Operand(XRegisterTag, bound);
                }

                void EmitTuple((int Input, int Arity, Atom? Tag, List<(int Index, int Register)> Fields) tuple)
                {
                    var (input, arity, tag, fields) = tuple;
                    bool shapeChecked = tupleSelection?.Shape.Input == input;
                    if (!shapeChecked && arityBranch && tag is not null)
                    {
                        int tagRegister = fields.Count == 0 && parameters[input] == VariableScopeNames.Wildcard
                            ? input : parameters.Length;
                        if (consResult?.HeadCall?.DeferredArgument is not null && fields.Count > 0 && fields[0].Register != input)
                        {
                            tagRegister = fields[0].Register;
                        }

                        owner.EmitTuplePart(input, 0, tagRegister);
                        owner.Op(ExactEqualTest, LabelTag, failure);
                        owner.Operand(XRegisterTag, tagRegister);
                        owner.LiteralOperand(tag);
                        checkedTagRegister = tagRegister;
                    }
                    else if (!shapeChecked && !arityBranch && tag is null)
                    {
                        EmitTupleShapeTest(XRegisterTag, input, arity, tupleCheckFailure);
                    }
                    else if (!shapeChecked && !arityBranch)
                    {
                        if (splitTupleAndAtom && branch == 0)
                        {
                            owner.Op(IsTuple, LabelTag, mixedLabels![1]);
                            owner.Operand(XRegisterTag, input);
                        }

                        EmitDetailRecordTest(
                            input,
                            arity,
                            tag!,
                            splitTupleAndAtom && branch == 0 ? mixedLabels![groupedBranches[1]?.End ?? 2] : tupleCheckFailure,
                            splitTupleAndAtom && branch == 0 ? TupleTypeBits : 0
                        );
                    }


                    if (heapWords > 0 && clause.Guard is null && cons.Count == 0 && tuples.Count == 1)
                    {
                        owner.Op(TestHeap, BeamWire.UnsignedTag, heapWords);
                        owner.Operand(BeamWire.UnsignedTag, function.Arity);
                        heapChecked = true;
                    }

                    int resultRegister = reorderedListInput >= 0 && sharedHeadInput == reorderedListInput
                        && tuples.Count == 1 && equalities.Count == 0 && clause.Guard is null
                        && clause.Body is Expr.Variable result
                        ? Array.IndexOf(parameters, result.Name)
                        : -1;
                    int resultIndex = resultRegister >= function.Arity && fieldTests.Count != 0
                        && fieldTests.Count == fields.Count - 1 && !fieldTests.ContainsKey(resultRegister)
                        && fieldTests.Values.All(test => test.Register is not int bound || bound < function.Arity)
                        ? fields.FindIndex(field => field.Register == resultRegister)
                        : -1;
                    foreach (var (index, register) in fields.Take(fieldLimit))
                    {
                        if (recordUpdate is not null || protectedEntryTuple is not null || projectedReturn is not null
                            || (resultIndex >= 0 && register == resultRegister)
                            || registerCase?.Deferred is { } projection && projection.Tuple == input
                                && (projection.Field == index || registerCase.SavedTupleFields?.Any(item => item.Field == index) == true
                                    || !bodyInputs!.Any(binding => binding.Register == register)))
                        {
                            continue;
                        }

                        bool hasTest = fieldTests.TryGetValue(register, out var expected);
                        int destination = hasTest && !arityBranch && clause.Body is Expr.Variable && fields.Count == 1 && input >= function.Arity
                            && parameters[input] == VariableScopeNames.Wildcard && expected.Register != input
                            && cons.Any(item => item.Head == input)
                            && !equalities.Any(test => test.Input == input || test.Bound == input)
                            && !branches.Skip(branch + 1).Any(later =>
                                later.Tuples.Any(shape => shape.Input == input)
                                || later.Cons.Any(item => item.Input == input || item.Head == input && item.HeadLiteral is not null)
                                || later.Equalities.Any(test => test.Input == input || test.Bound == input)
                                || later.FieldTests.Values.Any(test => test.Register == input)
                                || ReadsEntryRegister(later.Clause, later.Parameters, input))
                            ? input : register;
                        if (resultIndex >= 0)
                        {
                            destination = Math.Min(register, resultRegister);
                        }

                        owner.Op(GetTupleElement, XRegisterTag, input);
                        owner.Operand(BeamWire.UnsignedTag, index);
                        owner.Operand(XRegisterTag, destination);
                        if (hasTest && expected.Literal is Nil)
                        {
                            owner.EmitNilTest(XRegisterTag, destination, failure);
                        }
                        else if (hasTest)
                        {
                            owner.Op(ExactEqualTest, LabelTag, failure);
                            owner.Operand(XRegisterTag, destination);
                            if (expected.Register is int bound)
                            {
                                owner.Operand(XRegisterTag, bound);
                            }
                            else
                            {
                                owner.LiteralOperand(expected.Literal!);
                            }
                        }
                    }
                    if (resultIndex >= 0)
                    {
                        owner.EmitTuplePart(input, fields[resultIndex].Index, 0);
                        returnedTupleField = true;
                    }
                }

                if (nestedPrefix && ReadEntryRemainderGuard(clause, clause.Guard) is { } arithmetic)
                {
                    EmitEntryGuardPrefix(clause, arithmetic.Prefix, failure, ReadGuardIntegerBounds);
                    int scratch = parameters.Length;
                    owner.Op(GcBifTwo, LabelTag, failure);
                    owner.Operand(BeamWire.UnsignedTag, scratch);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Remainder, 2));
                    owner.Operand(XRegisterTag, arithmetic.Left);
                    owner.Operand(XRegisterTag, arithmetic.Right);
                    owner.Operand(XRegisterTag, scratch);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.EmitTypedRegister(scratch, IntegerTypeBits);
                    owner.LiteralOperand(arithmetic.Expected);
                }
                else if (!complementaryGuards || branch == 0)
                {
                    EmitEntryGuardPrefix(clause, clause.Guard, failure, ReadGuardIntegerBounds);
                }

                (long Lower, long Upper)? ReadGuardIntegerBounds(string variable)
                {
                    int parameter = Array.FindIndex(function.Clauses[branch].Patterns.ToArray(),
                        pattern => pattern is Pattern.Variable argument && argument.Name == variable);
                    return parameter >= 0 && !owner.IsDeclaredNative((function.Name, function.Arity))
                        ? ReadPrivateIntegerBounds(function, parameter)
                        : null;
                }

                if (tupleCall)
                {
                    TryEmitEntryTupleCallResult(clause.Body, parameters, deferred: deferredTupleCall);
                }
                else if (consResult is not null)
                {
                    int live = Math.Max(EntryCallLiveCount(consResult.Call, parameters),
                        consResult.Saved.DefaultIfEmpty(-1).Max() + 1);
                    if (consResult.HeadCall is { } headCall)
                    {
                        live = Math.Max(live, headCall.DeferredArgument is { } deferred
                            ? deferred.Register + 1 : EntryCallLiveCount(headCall.Call, parameters));
                    }

                    bool extractTail = consResult.HeadCall is { ExtractTail: true };
                    EmitConsCallResult(consResult, extractTail ? function.Arity : live,
                        listInput: extractTail ? cons[0].Input : 0,
                        previous: literalGroup is null ? null : previousConsCall);
                    previousConsCall = CallLocation(consResult.Call.Call, previousConsCall);
                }
                else if (recordUpdate is not null)
                {
                    recordUpdate();
                }
                else if (callChain is not null)
                {
                    EmitEntryCallChain(
                        callChain.First,
                        parameters,
                        callChain.Continuation,
                        callChain.RelocateBeforeTrim,
                        extraction: callChain.ListExtraction
                    );
                }
                else if (savedCalls is not null)
                {
                    EmitSavedCallSequence(savedCalls, tuples.Count != 0 ? savedCallFailures : null);
                }
                else if (tailList is { } list)
                {
                    EmitRegisterTailList(list.Call, list.Elements, heapChecked);
                }
                else if (tailConstruction is { } construction)
                {
                    EmitCallArgumentConstructions(construction.Plan, -1, heapChecked: heapChecked);
                    EmitRegisterTailTarget(construction.Call, construction.Call.Arguments.Count);
                }
                else if (tailCall is not null)
                {
                    if (checkedTagRegister >= 0 && clause.Guard is null && tailCall.ConstructedArgument is null
                        && tuples is [var tagged] && tagged.Tag is { } tag)
                    {
                        tailCall = tailCall with
                        {
                            Constants = tailCall.Constants.Where(move =>
                                move.Destination != checkedTagRegister || !move.Value.Equals(tag)).ToList()
                        };
                    }

                    if (!heapChecked && tailCall.ConstructedArgument is { } argument)
                    {
                        owner.Op(TestHeap, BeamWire.UnsignedTag, argument.HeapWords);
                        int live = argument.IsTuple
                            ? Math.Max(EntryCallLiveCount(tailCall, parameters),
                                argument.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1)
                            : parameters.Length;
                        owner.Operand(BeamWire.UnsignedTag, live);
                    }

                    EmitRegisterTailCall(tailCall, previousTupleTailLine);
                }
                else if (projectedReturn is not null)
                {
                    var projection = tuples[0];
                    owner.Op(TestHeap, BeamWire.UnsignedTag, projectedReturn.HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, projection.Input + 1);
                    owner.EmitTuplePart(projection.Input, projection.Fields[0].Index, 0);
                    EmitEntryConstruction(projectedReturn);
                    owner.instructions.WriteByte(Return);
                }
                else if (tupleResult is { } constructed)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, constructed.HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, constructed.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1);
                    EmitEntryConstruction(constructed);
                    owner.instructions.WriteByte(Return);
                }
                else if (arithmeticTail)
                {
                    TryEmitEntryArithmeticCall(sourceFunction, clause, clause.Body, parameters, -1, null);
                }
                else if (returnedConstruction is not null)
                {
                    EmitCallArgumentConstructions(returnedConstruction, -1);
                    owner.instructions.WriteByte(Return);
                }
                else if (protectedEntryTuple is not null)
                {
                    TryEmitProtectedTaggedResult(clause.Body, parameters, protectedEntryTuple);
                }
                else if (bodyInputs is not null)
                {
                    if (elementCase is { } element)
                    {
                        EmitListElementCase(element.Source, element.Branch);
                    }
                    else if (registerCase is not null)
                    {
                        EmitEntryRegisterCase(registerCase, listCaseFailure);
                        extractedListCase |= registerCase.Extraction is not null;
                    }
                    else
                    {
                        var reusedTail = cons is [var extracted] && extracted.Tail == extracted.Input && extracted.Head >= function.Arity
                            ? (Register: extracted.Tail, extracted.Head)
                            : ((int Register, int Head)?)null;
                        bool registerTupleTail = IsSimpleTupleMatchTail(clause.Body)
                            && TryEmitEntryTupleMatchResult(
                                clause.Body,
                                parameters,
                                out tupleTailLine,
                                tupleMatchFailures
                            );
                        if (!registerTupleTail && (clause.Guard is not null || !TryEmitProtectedTaggedResult(clause.Body, parameters))
                            && !TryEmitPrefixedCallResultCase(clause.Body, parameters)
                            && (cons.Count == 0 || !TryEmitListEntryComparisonCase(
                                sourceFunction,
                                clause,
                                bodyInputs,
                                parameters,
                                branches.Count + 2 * branch,
                                bodyFrames,
                                reusedTail
                            )))
                        {
                            EmitEntryClauseBody(clause with { Patterns = entryPatterns }, bodyInputs, branch, bodyFrames, reusedTail, deferredCons, literalDispatch);
                        }
                    }
                }
                else
                {
                    if (clause.Body is Expr.Variable returned)
                    {
                        int input = Array.IndexOf(parameters, returned.Name);
                        if (input != 0 && !returnedTupleField)
                        {
                            owner.MoveRegister(XRegisterTag, input, XRegisterTag, 0);
                        }
                    }
                    else if ((!literalDispatch || clause.Body is not Expr.Literal returnedLiteral
                            || !literals.Contains((0, returnedLiteral.Value)))
                        && (arityCount == 0 && !extractedListCase || clause.Body is not Expr.Literal { Value: Nil }
                            || !literals.Contains((0, Nil.Value))))
                    {
                        owner.EmitConstant(((Expr.Literal)clause.Body).Value, 0);
                    }

                    owner.instructions.WriteByte(Return);
                }
                if (tupleSelection is { } dispatchedTuple)
                {
                    if (position + 1 == dispatchedTuple.End && dispatchedTuple.End < branches.Count)
                    {
                        Mark(tupleSelectionFailure);
                    }
                }
                else if (!selectedAtom && !selectedLiteral && mixedLabels is null && hasNext && (nonCons == 0 || clause.Guard is not null
                    || sharedHeadInput >= 0 && branch < branches.Count - 2))
                {
                    Mark(failure);
                }
            }

            foreach (Action emitFailure in savedCallFailures)
            {
                emitFailure();
            }
            EmitRegisterTupleMatchFailures(tupleMatchFailures);

            if (bodyFrames.Count > 0)
            {
                TupleFrames = bodyFrames;
            }

            return true;

        }

        private sealed record EntryTupleSelectionArm(int Input, int Arity, Atom? Tag,
            List<(int Index, int Register)> Fields, int Selector, Term Key, bool SelectsTag);

        private sealed record EntryTupleAritySelection(int Start, int Arity, Term[] Keys);

        private sealed record EntryTupleSelection(int Start, int End, EntryTupleSelectionArm Shape, EntryTupleAritySelection[] Groups);

        private static int ReadNilBeforeConsInput(IReadOnlyList<Clause> clauses, out IReadOnlyList<Clause> reordered)
        {
            reordered = clauses;
            if (clauses.Count > 2 && clauses[0] is { Guard: null } first
                && first.Patterns.Any(pattern => ListEntryTest(pattern) == IsNil))
            {
                Clause[] candidates = [.. clauses.Skip(1), first];
                int sharedInput = ReadSharedHeadConsInput(candidates);
                if (sharedInput >= 0 && ListEntryTest(first.Patterns[sharedInput]) == IsNil)
                {
                    reordered = candidates;

                    return sharedInput;
                }
            }

            if (clauses is not [{ Guard: null } empty, { Guard: null } nonempty])
            {
                return -1;
            }

            int list = -1;
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < empty.Patterns.Count; index++)
            {
                if (list < 0 && ListEntryTest(empty.Patterns[index]) == IsNil
                    && nonempty.Patterns[index] is Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }
                    && Add(head) && Add(tail))
                {
                    list = index;
                }
                else if (empty.Patterns[index] is not Erlang.Pattern.Variable
                    || !ReadNonemptyParameter(nonempty.Patterns[index]))
                {
                    return -1;
                }
            }

            if (list >= 0)
            {
                reordered = [nonempty, empty];
            }

            return list;

            bool Add(Pattern.Variable variable) => variable.Name == VariableScopeNames.Wildcard || bindings.Add(variable.Name);

            bool ReadNonemptyParameter(Pattern pattern) => pattern switch
            {
                Pattern.Variable variable => Add(variable),
                Pattern.Tuple tuple => tuple.Items.All(item => item is Pattern.Variable field && Add(field)),
                AliasPattern alias when TryReadPatternAlias(alias, out var name, out var shape)
                    && shape is Pattern.Tuple tuple => Add(name) && tuple.Items.All(item => item is Pattern.Variable field && Add(field)),
                _ => false
            };
        }

        private static bool HasComplementaryEntryComparisons(IReadOnlyList<Clause> clauses)
        {
            return clauses is [var first, var second]
                && Read(first) is { } left && Read(second) is { } right
                && left.Input == right.Input && left.Limit.Equals(right.Limit)
                && left.Less != right.Less && left.Inclusive != right.Inclusive;

            static (int Input, Term Limit, bool Less, bool Inclusive)? Read(Clause clause)
            {
                if (!HasUnconditionalParameters(clause)
                    || clause.Guard is not Expr.Binary
                    {
                        Operator: ErlangOperators.Less or ErlangOperators.LessOrEqual
                            or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
                    } comparison)
                {
                    return null;
                }

                bool reversed = comparison.Left is not Expr.Variable;
                Expr operand = reversed ? comparison.Right : comparison.Left;
                Expr limit = reversed ? comparison.Left : comparison.Right;
                if (operand is not Expr.Variable variable || !AttributeLiteral.TryNormalizeConstant(limit, out Term value)
                    || value is not (Integer or FloatTerm))
                {
                    return null;
                }

                int input = Array.FindIndex(clause.Patterns.ToArray(), pattern =>
                    pattern is Pattern.Variable parameter && parameter.Name == variable.Name);
                bool less = comparison.Operator is ErlangOperators.Less or ErlangOperators.LessOrEqual;
                bool inclusive = comparison.Operator is ErlangOperators.LessOrEqual or ErlangOperators.GreaterOrEqual;

                return input < 0 ? null : (input, value, less != reversed, inclusive);
            }
        }

        private static bool IsEntryControlTransfer(Expr.Call call, string? module) =>
            module == ErlangModuleNames.Module && IsControlTransfer(call.Function, call.Arguments.Count);

        private static bool ReadsEntryRegister(Clause clause, string[] parameters, int register)
        {
            if (register >= parameters.Length || parameters[register] == VariableScopeNames.Wildcard)
            {
                return false;
            }

            string name = parameters[register];
            var namedClause = clause with
            {
                Patterns = parameters.Select(parameter => (Pattern)new Pattern.Variable(parameter)).ToArray()
            };
            return ReadUsedEntryParameters(namedClause)?.Contains(name) != false
                || clause.Guard is { } guard && LocalFunctionEntries.Walk(guard).OfType<Expr.Variable>()
                    .Any(variable => variable.Name == name);
        }

        private (Expr.Call Call, CallArgumentConstructions Plan)? ReadEntryTailConstruction(
            Expr.Call call,
            string[] inputs,
            bool allowOrdinary
        )
        {
            var target = owner.ResolveImportedCall(call, entries);
            var resolved = target with { Arguments = target.Arguments.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() };
            if (!TryResolveOrdinaryCall(resolved, out string? module, allowControlTransfer: true)
                || !allowOrdinary && !IsEntryControlTransfer(target, module))
            {
                return null;
            }

            var registers = inputs.Select((name, register) => (name, register))
                .Where(binding => binding.name != VariableScopeNames.Wildcard)
                .ToDictionary(binding => binding.name, binding => binding.register, StringComparer.Ordinal);
            return ReadCallArgumentConstructions(call.Arguments, -1, new Dictionary<string, int>(), inputs: registers)
                is { } construction ? (target with { Module = module }, construction) : null;
        }

        private void EmitEntryClauseBody(
            Clause clause,
            (Pattern.Variable Variable, int Register)[] inputs,
            int branch,
            Dictionary<int, int> frames,
            (int Register, int Head)? reusedTail,
            DeferredConsEntry? deferred,
            bool literalDispatch
        )
        {
            if (clause.Body is Expr.List { Items: [Expr.Call call], Tail: null })
            {
                var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard,
                    inputs.Select(input => input.Register + 1).DefaultIfEmpty(0).Max()).ToArray();
                foreach (var (variable, register) in inputs)
                {
                    parameters[register] = variable.Name;
                }

                if (ReadEntryCall(call, parameters) is { } plan)
                {
                    EmitFrameAllocation(EntryCallLiveCount(plan, parameters), 0);
                    EmitEntryCall(plan);
                    owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.instructions.WriteByte(PutList);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(Nil.Value);
                    owner.Operand(XRegisterTag, 0);
                    owner.EmitFrameReturn();

                    return;
                }
            }

            var previousVariables = variables;
            int previousSlot = nextSlot;
            int previousPeak = peakSlot;
            int previousFrame = frameSize;
            var previousCaptureOrder = entryCaptureOrder;

            variables = new(StringComparer.Ordinal);
            var previousRecords = checkedRecordBindings;
            checkedRecordBindings = new(StringComparer.Ordinal);
            nextSlot = 0;
            peakSlot = 0;
            frameSize = plannedTupleFrames?.GetValueOrDefault(branch, previousFrame) ?? previousFrame;
            if (reusedTail is { } tail)
            {
                // The tail is a new pattern binding even when extraction overwrites the list argument.
                entryCaptureOrder = inputs.OrderBy(binding => binding.Register == tail.Register ? tail.Head : binding.Register)
                    .ThenBy(binding => binding.Register == tail.Register)
                    .Select((binding, order) => (binding.Variable.Name, Order: order))
                    .ToDictionary(binding => binding.Name, binding => binding.Order, StringComparer.Ordinal);
            }

            var sequence = deferred?.Sequence ?? ReadEntrySequenceBody(clause.Body, inputs);
            if (sequence is not null)
            {
                EmitEntrySequenceBody(sequence, inputs, deferred, literalDispatch, clause.Guard, clause.Patterns);
            }
            else
            {
                EmitConsEntryBody(clause, inputs, reuseCallSlots: true);
            }

            frames.Add(branch, peakSlot);

            variables = previousVariables;
            nextSlot = previousSlot;
            peakSlot = Math.Max(previousPeak, peakSlot);
            frameSize = previousFrame;
            checkedRecordBindings = previousRecords;
            entryCaptureOrder = previousCaptureOrder;
        }

        private sealed record EntrySequenceBody(
            EntryCall Call, Expr Continuation, Pattern.Variable? Binding, HashSet<string> Used, int Live);

        private sealed record DeferredConsEntry(int Input, int Tail, EntrySequenceBody Sequence);

        private DeferredConsEntry? ReadDeferredConsEntry(Expr body, (Pattern.Variable Variable, int Register)[] inputs,
            int input, int head, int tail)
        {
            if (head < 0 || tail < 0 || tail == input
                || ReadEntrySequenceBody(body, inputs) is not { } sequence)
            {
                return null;
            }

            string? headName = inputs.FirstOrDefault(binding => binding.Register == head).Variable?.Name;
            string? tailName = inputs.FirstOrDefault(binding => binding.Register == tail).Variable?.Name;
            var callInputs = sequence.Call.Call.Arguments.OfType<Expr.Variable>();
            if (headName is null || tailName is null || sequence.Used.Contains(headName) || !sequence.Used.Contains(tailName)
                || !callInputs.Any(argument => argument.Name == headName) || callInputs.Any(argument => argument.Name == tailName))
            {
                return null;
            }

            var remapped = inputs.Select(binding => (binding.Variable, binding.Register == head ? input : binding.Register)).ToArray();
            return ReadEntrySequenceBody(body, remapped) is { } direct ? new DeferredConsEntry(input, tail, direct) : null;
        }

        private EntrySequenceBody? ReadEntrySequenceBody(Expr body, (Pattern.Variable Variable, int Register)[] inputs)
        {
            if (body is not Expr.Sequence { Items: [var first, var next, ..] } sequence)
            {
                return null;
            }

            Pattern.Variable? binding = null;
            if (first is Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call call })
            {
                if (inputs.Any(input => input.Variable.Name == result.Name))
                {
                    return null;
                }

                binding = result;
                first = call;
            }

            var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard,
                inputs.Select(input => input.Register + 1).DefaultIfEmpty(0).Max()).ToArray();
            foreach (var (variable, register) in inputs)
            {
                parameters[register] = variable.Name;
            }

            if (first is not Expr.Call read || ReadEntryCall(read, parameters) is not { } plan)
            {
                return null;
            }

            Expr continuation = sequence.Items.Count == 2 ? next : new Expr.Sequence(sequence.Items.Skip(1).ToArray());
            Pattern[] bindings = inputs.Select(input => (Pattern)input.Variable).ToArray();
            if (binding is { Name: not VariableScopeNames.Wildcard })
            {
                bindings = [.. bindings, binding];
            }

            if (ReadUsedEntryParameters(new Clause(bindings, null, continuation)) is not { } used)
            {
                return null;
            }

            return new EntrySequenceBody(plan, continuation, binding, used, EntryCallLiveCount(plan, parameters));
        }

        private void EmitEntrySequenceBody(EntrySequenceBody sequence, (Pattern.Variable Variable, int Register)[] inputs,
            DeferredConsEntry? deferred, bool literalDispatch, Expr? entryGuard, IReadOnlyList<Pattern> entryPatterns)
        {
            var measurement = ReadEntryResultMeasurement(sequence, inputs);
            if (measurement is { } measured)
            {
                sequence = sequence with { Continuation = measured.Continuation, Used = measured.Used };
            }

            var survivors = inputs.Where(input => sequence.Used.Contains(input.Variable.Name));
            bool savesResult = sequence.Binding is { } bindingResult && sequence.Used.Contains(bindingResult.Name);
            // Keep the measured pair at the bottom of the frame so terminal
            // branches can discard it or move its surviving members before trim.
            bool measuredListFrame = deferred is null && !literalDispatch && savesResult
                && measurement is { Call.Function: AutoImportNames.ByteSize or AutoImportNames.BitSize }
                && sequence.Continuation is Expr.If conditionalResult
                && conditionalResult.Clauses.All(clause =>
                    (clause.Body is Expr.Sequence body ? body.Items[^1] : clause.Body) is Expr.List);
            var saved = deferred is null && !literalDispatch && !measuredListFrame ? survivors.ToArray()
                : survivors.OrderByDescending(binding => binding.Register).ToArray();
            int live = saved.Where(binding => binding.Register != deferred?.Tail)
                .Select(binding => binding.Register + 1).DefaultIfEmpty(0).Max();
            live = Math.Max(sequence.Live, Math.Max(live, deferred is null ? 0 : deferred.Input + 1));
            EntryCall? tail = measurement is null && sequence.Binding is { } result && sequence.Used.Contains(result.Name)
                && sequence.Continuation is Expr.Call continuation
                ? ReadEntryCall(continuation, [result.Name], saved.Select(binding => binding.Variable.Name).ToArray(),
                    allowControlTransfer: true) : null;
            int resultSlot = measuredListFrame || literalDispatch && savesResult && tail is null ? Slot() : -1;
            int measurementSlot = measuredListFrame ? Slot() : -1;
            EmitFrameAllocation(live, frameSize);
            if (measuredListFrame)
            {
                EmitFrameInitialization(0, nextSlot);
                EmitFrameInitialization(nextSlot + saved.Length, frameSize);
            }
            else
            {
                EmitFrameInitialization(saved.Length + (resultSlot >= 0 ? 1 : 0), frameSize, resultSlot >= 0 ? resultSlot : null);
            }
            int tailSlot = -1;
            foreach (var (variable, register) in saved)
            {
                int slot = Slot();
                if (register == deferred?.Tail)
                {
                    tailSlot = slot;
                }
                else
                {
                    owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
                }
                variables.Add(variable.Name, slot);
            }

            if (deferred is not null)
            {
                owner.EmitListParts(deferred.Input, XRegisterTag, deferred.Input, YRegisterTag, tailSlot);
            }

            foreach (Pattern pattern in entryPatterns)
            {
                RememberCheckedRecord(pattern);
            }

            EmitEntryCall(sequence.Call);
            if (tail is not null)
            {
                owner.EmitOrderedTailMoves(tail.Moves, tail.Constants, tail.SavedInputs);
                EmitPreparedFrameTail(tail, CallLocation(sequence.Call.Call, location), frameSize);
                return;
            }

            if (measurement is { } measuredResult && resultSlot < 0 && sequence.Continuation is Expr.Tuple measuredTuple)
            {
                int destination = savesResult ? 1 : 0;
                var registers = new Dictionary<string, int>(StringComparer.Ordinal) { [measuredResult.Result.Name] = destination };
                if (savesResult)
                {
                    registers.Add(sequence.Binding!.Name, 0);
                }

                if (ReadCallArgumentConstructions([measuredTuple], -1, variables, inputs: registers) is { } construction)
                {
                    EmitEntryMeasurement(measuredResult.Call, sequence.Call.Call, XRegisterTag, destination);
                    EmitCallArgumentConstructions(construction, -1);
                    owner.EmitFrameReturn(frameSize);
                    return;
                }
            }

            if (measurement is null && resultSlot < 0 && savesResult && sequence.Continuation is Expr.Tuple tuple
                && TryReadBoundTupleOperands(tuple, sequence.Binding!.Name, variables, out var operands))
            {
                EmitTupleWithX0(operands);
                owner.EmitFrameReturn(frameSize);
                return;
            }

            if (savesResult && resultSlot < 0)
            {
                resultSlot = Slot();
            }

            if (measurement is { } operation)
            {
                int destination = measuredListFrame ? measurementSlot : Slot();
                EmitEntryMeasurement(operation.Call, sequence.Call.Call, YRegisterTag, destination);
                variables.Add(operation.Result.Name, destination);
            }

            if (savesResult)
            {
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, resultSlot);
                variables.Add(sequence.Binding!.Name, resultSlot);
            }

            if (sequence.Continuation is Expr.If conditional && TryEmitTerminalListConditional(conditional,
                measuredListFrame ? measurement?.Result.Name : null, entryGuard))
            {
                return;
            }

            if (!TailLocal(sequence.Continuation, reuseCallSlots: true))
            {
                EmitReturnExpression(sequence.Continuation, reuseCallSlots: true);
                owner.EmitFrameReturn(frameSize);
            }
        }

        private void EmitEntryMeasurement(Expr.Call measurement, Expr.Call producer, int destinationTag, int destination)
        {
            owner.EmitCallLine(measurement, CallLocation(producer, location));
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, measurement.Function, 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(destinationTag, destination);
        }

        private (Expr.Call Call, Pattern.Variable Result, Expr Continuation, HashSet<string> Used)? ReadEntryResultMeasurement(
            EntrySequenceBody sequence,
            (Pattern.Variable Variable, int Register)[] inputs
        )
        {
            if (sequence.Binding is not { Name: not VariableScopeNames.Wildcard } binding
                || sequence.Continuation is not Expr.Sequence
                {
                    Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                            Value: Expr.Call { Arguments: [Expr.Variable source] } call
                        },
                        var next, ..
                    ]
                } body
                || source.Name != binding.Name || result.Name == binding.Name
                || inputs.Any(input => input.Variable.Name == result.Name)
                || ReadTailBifOpcode(call) != GcBifOne)
            {
                return null;
            }

            Expr continuation = body.Items.Count == 2 ? next : new Expr.Sequence(body.Items.Skip(1).ToArray());
            Pattern[] bindings = [.. inputs.Select(input => (Pattern)input.Variable), binding, result];
            if (ReadUsedEntryParameters(new Clause(bindings, null, continuation)) is not { } used || !used.Contains(result.Name))
            {
                return null;
            }

            return (call, result, continuation, used);
        }

        private bool HasProvenConsEntry(FunctionDefinition function, int input) =>
            !owner.IsDeclaredNative((function.Name, function.Arity))
            && HasOnlyLocalCalls(function, function.Arity, (_, call) =>
                call.Arguments[input] is Expr.List { Items.Count: > 0 } or Expr.Literal { Value: Cons });

        private bool TryEmitProvenConsTail(FunctionDefinition function, Clause clause)
        {
            if (clause is not { Guard: null, Body: Expr.Call call }
                || function.Arity >= FirstReservedXRegister)
            {
                return false;
            }

            int input = -1;
            string? head = null;
            var names = new string[function.Arity + 1];
            for (int index = 0; index < function.Arity; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable variable)
                {
                    names[index] = variable.Name;
                }
                else if (input < 0 && clause.Patterns[index] is Pattern.List
                    { Items: [Pattern.Variable headVariable], Tail: Pattern.Variable tail })
                {
                    input = index;
                    head = headVariable.Name;
                    names[index] = tail.Name;
                }
                else
                {
                    return false;
                }
            }

            if (input < 0 || head == VariableScopeNames.Wildcard || names[input] == VariableScopeNames.Wildcard)
            {
                return false;
            }

            names[^1] = head!;
            var bindings = names.Where(name => name != VariableScopeNames.Wildcard).ToArray();
            if (bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Length
                || ReadEntryCall(call, names) is not { } tailCall || !HasProvenConsEntry(function, input))
            {
                return false;
            }

            owner.EmitListParts(input, XRegisterTag, function.Arity, XRegisterTag, input);
            owner.EmitOrderedTailMoves(tailCall.Moves, tailCall.Constants);
            EmitRegisterTailTarget(call with { Module = tailCall.Module }, call.Arguments.Count);

            return true;
        }

        private bool TryEmitUnguardedTupleFieldCall(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Variable argument, Pattern.Variable retained] }],
                        Body: Expr.Tuple { Items: [Expr.Call { Arguments: [Expr.Variable input] } call, Expr.Variable output] }
                    },
                    { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable original }
                ]
                || argument.Name != input.Name || retained.Name != output.Name || fallback.Name != original.Name
                || !DistinctBindings(argument, retained)
                || ReadEntryCall(call, [VariableScopeNames.Wildcard, argument.Name]) is not { } plan)
            {
                return false;
            }

            int failure = Label();
            EmitTupleShapeTest(XRegisterTag, 0, 2, failure);
            EmitFrameAllocation(1, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            EmitEntryCall(plan);
            EmitTupleWithX0([(null, null), (0, null)]);
            owner.EmitFrameReturn(1);
            Mark(failure);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static int ReadTerminalEntryConsInput(IReadOnlyList<Clause> clauses)
        {
            if (clauses is not [{ Guard: null, Body: Expr.Call { Module: null } } cons,
                { Guard: null } empty, { Guard: null } fallback]
                || !HasUnconditionalParameters(fallback))
            {
                return -1;
            }

            for (int input = 0; input < cons.Patterns.Count; input++)
            {
                if (cons.Patterns[input] is Pattern.List
                    { Items: [Erlang.Pattern.Variable], Tail: Erlang.Pattern.Variable }
                    && ListEntryTest(empty.Patterns[input]) == IsNil
                    && cons.Patterns.Where((_, index) => index != input).All(pattern => pattern is Pattern.Variable)
                    && empty.Patterns.Where((_, index) => index != input).All(pattern => pattern is Pattern.Variable))
                {
                    return input;
                }
            }

            return -1;
        }

        private static int ReadSharedHeadConsInput(IReadOnlyList<Clause> clauses)
        {
            if (clauses.Count < 3 || clauses[^2].Guard is not null || clauses[^1].Guard is not null)
            {
                return -1;
            }

            for (int input = 0; input < clauses[0].Patterns.Count; input++)
            {
                bool nested = clauses[0].Patterns.Count == 1
                    && clauses.Any(clause => clause.Patterns[input] is Pattern.List { Items.Count: > 1 });
                if (!nested && clauses.Any(clause => clause.Guard is not null))
                {
                    continue;
                }

                if (clauses[^1].Patterns[input] is Erlang.Pattern.Variable or Pattern.List { Items: [], Tail: null }
                    && clauses[^2].Patterns[input] is Pattern.List { Items: [Erlang.Pattern.Variable], Tail: Erlang.Pattern.Variable }
                    && !HasRepeatedConsBindings(clauses[^2], input)
                    && clauses.Take(clauses.Count - 2).All(clause =>
                        clause.Patterns[input] is Pattern.List { Items: [var head], Tail: Erlang.Pattern.Variable }
                            && (EntryShape(head) is Erlang.Pattern.Literal or Erlang.Pattern.Tuple
                                || head is Erlang.Pattern.Variable && HasRepeatedConsBindings(clause, input))
                        || nested && clause.Patterns[input] is Pattern.List
                            { Items.Count: > 1, Tail: Erlang.Pattern.Variable })
                    && clauses.All(clause => clause.Patterns.Where((_, index) => index != input)
                        .All(pattern => pattern is Pattern.Variable)))
                {
                    return input;
                }
            }

            return -1;
        }

        private static bool HasRepeatedConsBindings(Clause clause, int input)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Pattern pattern in clause.Patterns)
            {
                if (IsRepeat(pattern))
                {
                    return true;
                }
            }

            return clause.Patterns[input] is Pattern.List { Items: [var head], Tail: { } tail }
                && (IsRepeat(head) || IsRepeat(tail));

            bool IsRepeat(Pattern pattern) =>
                pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable && !names.Add(variable.Name);
        }

        private static int ReadSharedEntryConsInput(IReadOnlyList<Clause> clauses)
        {
            if (clauses.Count < 3 || clauses[^2].Guard is not null)
            {
                return -1;
            }

            for (int input = 0; input < clauses[0].Patterns.Count; input++)
            {
                if (clauses[0].Patterns[input] is Pattern.List
                    { Items: [Pattern.Variable { Name: not VariableScopeNames.Wildcard }],
                        Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } }
                    && clauses[^1].Patterns[input] is Erlang.Pattern.List { Items: [], Tail: null } or Erlang.Pattern.Literal { Value: Nil }
                    && !HasRepeatedConsBindings(clauses[^2], input)
                    && clauses.All(clause => clause.Patterns.Where((_, index) => index != input).All(pattern => pattern is Erlang.Pattern.Variable))
                    && clauses.Take(clauses.Count - 1).All(clause => clause.Patterns[input] is Pattern.List
                        { Items: [Erlang.Pattern.Variable], Tail: Erlang.Pattern.Variable })
                    && clauses.Take(clauses.Count - 2).All(clause => clause.Guard is not null))
                {
                    return input;
                }
            }

            return -1;
        }

        private bool CanEmitSeparateEntryGuard(Clause clause, Expr? guard, bool allowRemainder = false) => guard switch
        {
            null => true,
            _ when allowRemainder && ReadEntryRemainderGuard(clause, guard) is { } remainder =>
                CanEmitSeparateEntryGuard(clause, remainder.Prefix),
            Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction =>
                CanEmitSeparateEntryGuard(clause, conjunction.Left) && CanEmitSeparateEntryGuard(clause, conjunction.Right),
            Expr.Binary comparison => ReadEntryComparisonOpcode(clause, comparison).HasValue,
            _ => ReadEntryTypeGuard(clause, guard).HasValue || ReadEntryFunctionArityGuard(clause, guard).HasValue
        };

        private static (Expr? Prefix, int Left, int Right, Integer Expected)? ReadEntryRemainderGuard(Clause clause, Expr? guard)
        {
            var conjunction = guard as Expr.Binary;
            bool hasPrefix = conjunction?.Operator == ErlangOperators.AndAlso;
            if ((hasPrefix ? conjunction!.Right : guard) is not Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Binary
                    {
                        Operator: ErlangOperators.Remainder,
                        Left: Expr.Variable left, Right: Expr.Variable right
                    },
                    Right: Expr.Literal { Value: Integer expected }
                } || clause.Patterns.Count >= FirstReservedXRegister)
            {
                return null;
            }

            var inputs = clause.Patterns.ToArray();
            int dividend = Array.FindIndex(inputs, pattern => pattern is Pattern.Variable variable && variable.Name == left.Name);
            int divisor = Array.FindIndex(inputs, pattern => pattern is Pattern.Variable variable && variable.Name == right.Name);
            return dividend >= 0 && divisor >= 0
                ? (hasPrefix ? conjunction!.Left : null, dividend, divisor, expected) : null;
        }

        private bool TryEmitEntryTupleCallResult(
            Expr body,
            string[] parameters,
            bool validateOnly = false,
            (int Register, int Field)? deferred = null
        )
        {
            Expr.Tuple tuple;
            Expr.Call call;
            string result;
            if (body is Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call producer },
                        Expr.Tuple continuation
                    ]
                })
            {
                if (binding.Name == VariableScopeNames.Wildcard
                    || parameters.Contains(binding.Name, StringComparer.Ordinal))
                {
                    return false;
                }

                tuple = continuation;
                call = producer;
                result = binding.Name;
            }
            else if (body is Expr.Tuple inline && inline.Items.OfType<Expr.Call>().ToArray() is [var embedded])
            {
                result = "\0entry_result";
                call = embedded;
                tuple = inline with
                {
                    Items = inline.Items.Select(item => ReferenceEquals(item, embedded) ? new Expr.Variable(result) : item)
                        .ToArray()
                };
            }
            else if (body is Expr.Tuple nested
                && FlattenCallArguments([], nested) is
                    [Expr.Match { Pattern: Pattern.Variable nestedBinding, Value: Expr.Call nestedProducer }, Expr.Tuple nestedResult])
            {
                tuple = nestedResult;
                call = nestedProducer;
                result = nestedBinding.Name;
            }
            else
            {
                return false;
            }

            if (ReadEntryCall(call, parameters) is not { } plan)
            {
                return false;
            }

            var tupleInputs = LocalFunctionEntries.Walk(tuple).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            var copies = new List<(int Source, int Slot)>();
            for (int input = parameters.Length - 1; input >= 0; input--)
            {
                string name = parameters[input];
                if (name != VariableScopeNames.Wildcard && tupleInputs.Contains(name))
                {
                    saved.Add(name, saved.Count);
                    copies.Add((input, saved.Count - 1));
                }
            }

            CallArgumentConstructions? construction = null;
            if (!TryReadBoundTupleOperands(tuple, result, saved, out var operands)
                && (construction = ReadCallArgumentConstructions(
                    [tuple],
                    -1,
                    saved,
                    inputs: new Dictionary<string, int>(StringComparer.Ordinal) { [result] = 0 }
                )) is null)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            int callLive = deferred is { } argument ? argument.Register + 1 : EntryCallLiveCount(plan, parameters);
            int live = Math.Max(callLive, copies.Select(copy => copy.Source + 1).DefaultIfEmpty(0).Max());
            EmitFrameAllocation(live, saved.Count);
            foreach (var (source, slot) in copies)
            {
                owner.MoveRegister(XRegisterTag, source, YRegisterTag, slot);
            }

            if (deferred is { } field)
            {
                owner.EmitTuplePart(field.Register, field.Field, 0);
                plan = plan with { Moves = [] };
            }

            EmitEntryCall(plan);
            if (construction is null)
            {
                EmitTupleWithX0(operands);
            }
            else
            {
                EmitCallArgumentConstructions(construction, -1);
            }

            owner.EmitFrameReturn(saved.Count);
            return true;
        }
    }
}
