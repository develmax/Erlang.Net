using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool IsBooleanPatternPredicate(IReadOnlyList<Clause> clauses) =>
        clauses is [
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }], Body: Expr.Literal { Value: Atom { Name: "true" } } },
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }], Body: Expr.Literal { Value: Atom { Name: "true" } } },
        { Guard: null, Patterns: [Pattern.Variable], Body: Expr.Literal { Value: Atom { Name: "false" } } }
        ];

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalPredicateCalls(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse,
                        Left: Expr.Call left, Right: Expr.Call right
                    } logical
                }
                || input.Name == VariableScopeNames.Wildcard
                || function.Clauses.Count > 1 && !HasLiteralDispatchKeys(function.Clauses.SkipLast(1))
                || ReadEntryCall(left, [input.Name]) is not { } first
                || ReadEntryCall(right, [], [input.Name]) is not { } second)
            {
                return false;
            }

            var proofs = new Dictionary<(string, int), bool>();
            if (!ReturnsBoolean(first.Call))
            {
                return false;
            }

            if (function.Clauses.Count == 1)
            {
                EmitFallback();
            }
            else
            {
                owner.EmitLiteralDispatch(function.Clauses, location, emitComputedFallback: EmitFallback);
            }

            return true;

            bool ReturnsBoolean(Expr expression)
            {
                if (expression is Expr.Literal { Value: Atom { Name: "true" or "false" } })
                {
                    return true;
                }
                if (expression is Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse } pair)
                {
                    return ReturnsBoolean(pair.Left) && ReturnsBoolean(pair.Right);
                }
                if (expression is not Expr.Call { Module: null, DynamicRemote: false } call)
                {
                    return false;
                }

                var key = (call.Function, call.Arguments.Count);
                if (proofs.TryGetValue(key, out bool known))
                {
                    return known;
                }
                if (!localFunctions.TryGetValue(key, out var target))
                {
                    return false;
                }

                proofs.Add(key, false);

                return proofs[key] = target.Clauses.All(clause => ReturnsBoolean(clause.Body));
            }

            void EmitFallback()
            {
                int slots = second.SavedInputs is { Count: > 0 } ? 1 : 0;
                EmitFrameAllocation(slots == 0 ? EntryCallLiveCount(first, [input.Name]) : 1, slots);
                if (slots != 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        0
                    );
                }
                EmitEntryCall(first);
                int evaluate = Label();
                owner.EmitAtomTest(0, logical.Operator == ErlangOperators.AndAlso ? "false" : "true", evaluate);
                owner.EmitFrameReturn(slots);
                Mark(evaluate);
                owner.EmitOrderedTailMoves(second.Moves, second.Constants, second.SavedInputs);
                EmitPreparedFrameTail(second, CallLocation(first.Call, location), slots);
            }
        }

        private bool TryEmitRegisterPredicate(FunctionDefinition function)
        {
            if (TryEmitSharedTaggedArityPredicate(function))
            {
                return true;
            }

            if (IsBooleanPatternPredicate(function.Clauses))
            {
                EmitUnaryPredicate(AutoImportNames.IsBoolean);

                return true;
            }

            if (function.Clauses is not [
                { Guard: { } guard, Body: Expr.Literal { Value: Atom { Name: "true" } } } accepted,
                { Guard: null, Body: Expr.Literal { Value: Atom { Name: "false" } } } rejected
            ] || !HasUnconditionalParameters(accepted) || !HasUnconditionalParameters(rejected))
            {
                return false;
            }

            var registers = accepted.Patterns.Cast<Pattern.Variable>()
                .Select((variable, index) => (variable.Name, index))
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard)
                .ToDictionary(parameter => parameter.Name, parameter => parameter.index, StringComparer.Ordinal);
            var tests = new List<int>();
            var comparisons = new List<(Expr Left, Expr Right)>();
            foreach (Expr item in PredicateConjuncts(guard))
            {
                if (comparisons.Count == 0 && item is Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsInteger, Arguments: [Expr.Variable variable]
                    } test && (test.Module is not null || !entries.ContainsKey((test.Function, 1)))
                    && registers.TryGetValue(variable.Name, out int register))
                {
                    tests.Add(register);
                }
                else if (item is Expr.Binary { Operator: ErlangOperators.GreaterOrEqual or ErlangOperators.LessOrEqual } comparison
                    && IsIntegerOperand(comparison.Left) && IsIntegerOperand(comparison.Right))
                {
                    comparisons.Add(comparison.Operator == ErlangOperators.GreaterOrEqual
                        ? (comparison.Left, comparison.Right) : (comparison.Right, comparison.Left));
                }
                else
                {
                    return false;
                }
            }

            bool IsIntegerOperand(Expr value) => value is Expr.Literal { Value: Integer }
                || value is Expr.Variable variable && registers.TryGetValue(variable.Name, out int index) && tests.Contains(index);

            if (tests is [0] && function.Arity == 1 && comparisons.Count == 0)
            {
                EmitUnaryPredicate(AutoImportNames.IsInteger);

                return true;
            }

            if (tests.Count == 0 || comparisons.Count == 0)
            {
                return false;
            }

            int failure = Label();
            foreach (int register in tests)
            {
                owner.Op(IsInteger, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
            }

            for (int index = 0; index < comparisons.Count; index++)
            {
                bool last = index == comparisons.Count - 1;
                owner.Op(last ? BifTwo : IsGreaterOrEqual, LabelTag, last ? 0 : failure);
                if (last)
                {
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.GreaterOrEqual, 2));
                }

                var (left, right) = comparisons[index];
                EmitOperand(left);
                EmitOperand(right);
                if (last)
                {
                    owner.Operand(XRegisterTag, 0);
                }
            }
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(Term.A("false"), 0);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitOperand(Expr expression)
            {
                if (expression is Expr.Variable variable)
                {
                    owner.EmitTypedRegister(registers[variable.Name], IntegerTypeBits);
                }
                else
                {
                    owner.LiteralOperand(((Expr.Literal)expression).Value);
                }
            }
        }

        private static IEnumerable<Expr> PredicateConjuncts(Expr expression) =>
            expression is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                ? PredicateConjuncts(conjunction.Left).Concat(PredicateConjuncts(conjunction.Right))
                : [expression];

        private void EmitUnaryPredicate(string name)
        {
            owner.Op(BifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, name, 1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
        }

        private bool TryEmitAtomCheckedTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call } success,
                { Guard: null, Body: Expr.Literal { Value: Atom result } } fallback
            ] || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            int register = -1;
            Atom? key = null;
            var variables = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < success.Patterns.Count; index++)
            {
                switch (success.Patterns[index])
                {
                    case Pattern.Literal { Value: Atom atom } when key is null:
                        key = atom;
                        register = index;
                        break;
                    case Pattern.Variable variable when variable.Name == VariableScopeNames.Wildcard || variables.Add(variable.Name):
                        break;
                    default:
                        return false;
                }
            }

            if (key is null || call.Arguments.Any(argument => argument is Expr.Variable variable
                ? !variables.Contains(variable.Name)
                : !AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            int failed = Label();
            owner.EmitAtomTest(register, key.Name, failed);
            owner.EmitCall(
                success,
                location,
                call,
                entries
            );
            Mark(failed);
            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitLiteralTailClauses(FunctionDefinition function)
        {
            if (TryEmitClosedAtomTailDispatch(function))
            {
                return true;
            }

            if (function.Clauses.Count < 2 || !HasUnconditionalParameters(function.Clauses[^1])
                || function.Clauses[^1].Body is not (Expr.Call or Expr.Binary))
            {
                return false;
            }

            var tests = new List<(int Register, Term Key)[]>();
            var compounds = new List<PredicateCalls?>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause.Guard is not null || clause.Patterns.Any(pattern =>
                        pattern is not (Pattern.Variable { } or Pattern.Literal { Value: Atom })))
                {
                    return false;
                }

                var names = clause.Patterns.OfType<Pattern.Variable>()
                    .Where(variable => variable.Name != VariableScopeNames.Wildcard).Select(variable => variable.Name).ToArray();
                PredicateCalls? compound = ReadPredicateCalls(clause);
                compounds.Add(compound);
                if (names.Distinct(StringComparer.Ordinal).Count() != names.Length
                    || compound is null && clause.Body is not Expr.Literal && (clause.Body is not Expr.Call { DynamicRemote: false } call
                        || !(call.Module is null && entries.ContainsKey((call.Function, call.Arguments.Count))
                            || IsConstantErrorCall(call))
                        || call.Arguments.Any(argument => argument is Expr.Variable variable
                            ? !names.Contains(variable.Name, StringComparer.Ordinal)
                            : !AttributeLiteral.TryNormalizeConstant(argument, out _))))
                {
                    return false;
                }

                tests.Add(clause.Patterns.Select((pattern, index) => (pattern, index))
                    .Where(entry => entry.pattern is Pattern.Literal { })
                    .Select(entry => (entry.index, ((Pattern.Literal)entry.pattern).Value)).ToArray());
            }

            if (function.Arity == 1 && function.Clauses.Count > 2
                && function.Clauses[^1].Body is Expr.Call error && IsConstantErrorCall(error)
                && function.Clauses[0].Body is Expr.Literal result
                && tests.Take(tests.Count - 1).All(test => test is [(0, Atom)])
                && function.Clauses.Take(function.Clauses.Count - 1)
                    .All(clause => clause.Body is Expr.Literal literal && literal.Value.Equals(result.Value)))
            {
                int success = Label();
                int failure = Label();
                var keys = tests.Take(tests.Count - 1).Select(test => (Atom)test[0].Key)
                    .Distinct().Order(Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                    .ToArray();
                owner.Op(SelectValue, XRegisterTag, 0);
                owner.Operand(LabelTag, failure);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, keys.Length * 2);
                foreach (Atom key in keys)
                {
                    owner.LiteralOperand(key);
                    owner.Operand(LabelTag, success);
                }
                Mark(success);
                owner.EmitConstant(result.Value, 0);
                owner.instructions.WriteByte(Return);
                Mark(failure);
                owner.EmitCall(
                    function.Clauses[^1],
                    location,
                    error,
                    entries
                );

                return true;
            }

            if (TryEmitLiteralReturnsWithTailFallback(function, tests))
            {
                return true;
            }

            if (TryEmitRegisterTailCall(function, dispatchOnly: true))
            {
                return true;
            }

            int[] clauseLabels = new int[function.Clauses.Count];
            int[] predicateFailures = new int[function.Clauses.Count];
            for (int index = 0; index < clauseLabels.Length; index++)
            {
                if (index != 0)
                {
                    clauseLabels[index] = Label();
                }
                if (compounds[index] is not null)
                {
                    predicateFailures[index] = Label();
                }
            }

            for (int index = 0; index < clauseLabels.Length; index++)
            {
                if (index != 0)
                {
                    Mark(clauseLabels[index]);
                }

                foreach (var (register, key) in tests[index])
                {
                    int alternative = index + 1;
                    while (tests[alternative].Contains((register, key)))
                    {
                        alternative++;
                    }

                    owner.Op(ExactEqualTest, LabelTag, clauseLabels[alternative]);
                    owner.Operand(XRegisterTag, register);
                    owner.LiteralOperand(key);
                }

                Clause clause = function.Clauses[index];
                if (compounds[index] is { } compound)
                {
                    EmitPredicateCalls(compound, predicateFailures[index]);
                }
                else if (clause.Body is Expr.Call call)
                {
                    owner.EmitCall(
                        clause,
                        location,
                        call,
                        entries
                    );
                }
                else
                {
                    owner.EmitConstant(clause.Body, 0);
                    owner.instructions.WriteByte(Return);
                }
            }

            return true;
        }

        private bool TryEmitLiteralReturnsWithTailFallback(
            FunctionDefinition function,
            IReadOnlyList<(int Register, Term Key)[]> tests
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || tests.Count < 3 || ReadAtomEntryType(function, 0) != 0
                || function.Clauses[^1] is not { Body: Expr.Call fallbackCall } fallback)
            {
                return false;
            }

            var keys = new HashSet<Atom>();
            var results = new HashSet<Term>();
            for (int index = 0; index < tests.Count - 1; index++)
            {
                if (tests[index] is not [(0, Atom key)]
                    || function.Clauses[index].Body is not Expr.Literal returned
                    || key.Equals(returned.Value) || !keys.Add(key))
                {
                    return false;
                }

                results.Add(returned.Value);
            }

            if (results.Count < 2)
            {
                return false;
            }

            owner.EmitLiteralDispatch(
                function.Clauses,
                location,
                emitComputedFallback: () => owner.EmitCall(
                    fallback,
                    location,
                    fallbackCall,
                    entries
                )
            );

            return true;
        }

        private bool TryEmitClosedAtomTailDispatch(FunctionDefinition function)
        {
            bool multipleArguments = function.Arity > 1;
            if (function.Arity == 0 || function.Clauses.Count < 2
                || multipleArguments && (function.Clauses.Count != 2 || owner.ModuleOptimizationDisabled
                    || recursiveClosure is not null || function.Arity >= FirstReservedXRegister))
            {
                return false;
            }

            int selector = multipleArguments
                ? Array.FindIndex(function.Clauses[0].Patterns.ToArray(), pattern => pattern is Pattern.Literal { Value: Atom })
                : 0;
            if (selector < 0)
            {
                return false;
            }

            var keys = new HashSet<Atom>();
            var branches = new List<ClosedAtomBranch>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause is { Guard: null, Patterns: [AliasPattern alias], Body: Expr.Variable result }
                    && TryReadPatternAlias(alias, out var binding, out var shape)
                    && result.Name == binding.Name && shape is Pattern.Literal { Value: Atom matched })
                {
                    if (!keys.Add(matched))
                    {
                        return false;
                    }

                    branches.Add(new(
                        matched,
                        null,
                        null,
                        ReturnsInput: true
                    ));
                    continue;
                }

                if (clause is
                    {
                        Guard: { } guard,
                        Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } input],
                        Body: Expr.Literal { Value: var guardedResult }
                    })
                {
                    var guardKeys = new List<Atom>();
                    if (!ReadAtomGuardKeys(guard, input.Name, guardKeys))
                    {
                        return false;
                    }

                    foreach (Atom guardedKey in guardKeys)
                    {
                        if (!keys.Add(guardedKey))
                        {
                            return false;
                        }

                        branches.Add(new(guardedKey, guardedResult, null));
                    }

                    continue;
                }

                if (clause.Guard is not null || clause.Patterns[selector] is not Pattern.Literal { Value: Atom key }
                    || !keys.Add(key))
                {
                    return false;
                }

                string[] parameters = [];
                if (multipleArguments)
                {
                    var patterns = clause.Patterns.ToArray();
                    patterns[selector] = new Pattern.Variable(VariableScopeNames.Wildcard);
                    if (!HasUnconditionalParameters(clause with { Patterns = patterns }))
                    {
                        return false;
                    }

                    parameters = patterns.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
                }

                Term? literal = clause.Body is Expr.Literal value ? value.Value : null;
                if (literal is null && multipleArguments && AttributeLiteral.TryNormalizeConstant(clause.Body, out var constant))
                {
                    literal = constant;
                }

                if (literal is not null)
                {
                    branches.Add(new(key, literal, null));
                }
                else if (clause.Body is Expr.Call { DynamicRemote: false } call
                    && ReadEntryCall(call, parameters) is { } tail
                    && (!multipleArguments || tail.Module is null || !IsTrappingTailBif(tail.Module, call.Function, call.Arguments.Count)))
                {
                    tail.Constants.RemoveAll(operand => operand.Destination == selector && operand.Value.Equals(key));
                    branches.Add(new(key, null, tail));
                }
                else
                {
                    return false;
                }
            }

            if (branches.Count < 2)
            {
                return false;
            }

            if (multipleArguments && ReadFiniteAtomArguments(function, selector) is { Count: > 0 } finite
                && finite.All(keys.Contains))
            {
                return false;
            }

            // When both boolean keys lead to the same block, beam_jump can replace
            // the switch with is_boolean. Leave that shape on its existing path.
            if (multipleArguments && branches is [var first, var second]
                && first.Key.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
                && second.Key.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
                && first.SharesCode(second))
            {
                return false;
            }

            branches.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            int failure = owner.nextLabel - 2;
            var labels = new int[branches.Count];
            var representatives = new int[branches.Count];
            for (int index = 0; index < branches.Count; index++)
            {
                ClosedAtomBranch current = branches[index];
                int previous = branches.FindIndex(0, index, current.SharesCode);
                representatives[index] = previous < 0 ? index : previous;
            }

            for (int index = labels.Length - 1; index >= 0; index--)
            {
                if (representatives[index] == index)
                {
                    labels[index] = Label();
                }
            }

            owner.EmitMembershipSelect(
                selector,
                failure,
                branches.Select((branch, index) => ((Term)branch.Key, labels[representatives[index]]))
            );

            for (int index = branches.Count - 1; index >= 0; index--)
            {
                if (representatives[index] != index)
                {
                    continue;
                }

                Mark(labels[index]);
                ClosedAtomBranch branch = branches[index];
                if (branch.ReturnsInput)
                {
                    owner.instructions.WriteByte(Return);
                }
                else if (branch.Literal is { } literal)
                {
                    if (selector != 0 || !branch.Key.Equals(literal)
                        || Array.IndexOf(representatives, index, index + 1) >= 0)
                    {
                        owner.EmitConstant(literal, 0);
                    }

                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    EntryCall call = branch.Tail!;
                    owner.EmitOrderedTailMoves(call.Moves, call.Constants);
                    EmitRegisterTailTarget(call.Call with { Module = call.Module }, call.Call.Arguments.Count);
                }
            }

            return true;
        }

        private sealed record ClosedAtomBranch(
            Atom Key,
            Term? Literal,
            EntryCall? Tail,
            bool ReturnsInput = false
        )
        {
            internal bool SharesCode(ClosedAtomBranch other)
            {
                if (ReturnsInput || other.ReturnsInput)
                {
                    return ReturnsInput && other.ReturnsInput;
                }

                if (Literal is { } literal)
                {
                    return literal.Equals(other.Literal);
                }

                return Tail is { Module: null } call
                    && other.Tail is { Module: null } previous
                    && call.Call.Function == previous.Call.Function
                    && call.Call.Arguments.Count == previous.Call.Arguments.Count
                    && call.Moves.SequenceEqual(previous.Moves)
                    && call.Constants.SequenceEqual(previous.Constants);
            }
        }

        private sealed record PredicateCalls(
            Expr.Call First,
            Expr.Call Second,
            int FirstRegister,
            int SecondRegister,
            bool Reverse
        );

        private PredicateCalls? ReadPredicateCalls(Clause clause)
        {
            if (clause.Body is not Expr.Binary { Operator: ErlangOperators.AndAlso }
                || PredicateConjuncts(clause.Body).ToArray() is not [
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable first] } a,
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable second] } b,
                    Expr.Binary
                {
                    Operator: ErlangOperators.GreaterOrEqual or ErlangOperators.LessOrEqual,
                    Left: Expr.Variable left, Right: Expr.Variable right
                } comparison
                ] || first.Name == second.Name || left.Name != first.Name || right.Name != second.Name
                || !ReturnsBoolean(a) || !ReturnsBoolean(b))
            {
                return null;
            }

            int firstRegister = -1;
            int secondRegister = -1;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable variable)
                {
                    if (variable.Name == first.Name) firstRegister = index;
                    if (variable.Name == second.Name) secondRegister = index;
                }
            }

            return firstRegister >= 0 && secondRegister >= 0
                ? new(
                    a,
                    b,
                    firstRegister,
                    secondRegister,
                    comparison.Operator == ErlangOperators.LessOrEqual
                )
                : null;

            bool ReturnsBoolean(Expr.Call call) =>
                localFunctions.TryGetValue((call.Function, 1), out var function)
                && HasUnconditionalParameters(function.Clauses[^1]) && function.Clauses[^1].Guard is null
                && function.Clauses.All(candidate =>
                    candidate.Body is Expr.Literal { Value: Atom { Name: "true" or "false" } });
        }

        private void EmitPredicateCalls(PredicateCalls compound, int failure)
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(compound.FirstRegister, compound.SecondRegister) + 1);
            owner.MoveRegister(
                XRegisterTag,
                compound.SecondRegister,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                compound.FirstRegister,
                YRegisterTag,
                1
            );
            if (compound.FirstRegister != 0)
            {
                owner.MoveToX0(XRegisterTag, compound.FirstRegister);
            }
            owner.EmitCallLine(compound.First, location);
            owner.EmitFieldCall(1, entries[(compound.First.Function, 1)]);
            owner.EmitAtomTest(0, "true", failure);
            Load(0);
            owner.EmitCallLine(compound.Second, CallLocation(compound.First, location));
            owner.EmitFieldCall(1, entries[(compound.Second.Function, 1)]);
            owner.EmitAtomTest(0, "true", failure);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.GreaterOrEqual, 2));
            owner.Operand(YRegisterTag, compound.Reverse ? 0 : 1);
            owner.Operand(YRegisterTag, compound.Reverse ? 1 : 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);
            Mark(failure);
            owner.EmitConstant(Term.A("false"), 0);
            owner.EmitFrameReturn(2);
        }
    }
}
