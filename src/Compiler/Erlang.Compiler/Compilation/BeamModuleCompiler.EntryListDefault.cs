using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static (Pattern.Variable Alias, Expr.Match Check, Expr.Variable Input)? ReadCheckedListBinding(Expr expression)
        {
            if (expression is Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } alias,
                    Value: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                            {
                                Pattern: Pattern.List
                                {
                                    Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                },
                                Value: Expr.Variable input
                            } check,
                            Expr.Variable returned
                        ]
                    }
                } && input.Name == returned.Name)
            {
                return (alias, check, input);
            }

            return null;
        }

        private bool TryEmitCheckedListEntry(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [var first, var last] }
                || ReadCheckedListBinding(first) is not { } binding)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int source = Array.IndexOf(parameters, binding.Input.Name);
            if (source < 0 || parameters.Contains(binding.Alias.Name, StringComparer.Ordinal))
            {
                return false;
            }

            Expr Resolve(Expr operand) => operand is Expr.Variable variable && variable.Name == binding.Alias.Name
                ? binding.Input : operand;

            if (last is Expr.Tuple tuple)
            {
                Expr[] operands = tuple.Items.Select(Resolve).ToArray();
                if (!operands.All(operand => operand is Expr.Variable variable
                    ? variable.Name != VariableScopeNames.Wildcard && parameters.Contains(variable.Name, StringComparer.Ordinal)
                    : AttributeLiteral.TryNormalizeConstant(operand, out _)))
                {
                    return false;
                }

                int failure = Label();
                owner.EmitNonemptyList(source, failure);
                int live = operands.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name) + 1)
                    .DefaultIfEmpty(0).Max();
                owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, operands.Length);
                foreach (Expr operand in operands)
                {
                    EmitDirectOperand(clause, operand);
                }

                owner.instructions.WriteByte(Return);
                EmitFailure(failure);
                return true;
            }

            if (last is not Expr.Call { Module: null, DynamicRemote: false } tail
                || !entries.TryGetValue((tail.Function, tail.Arguments.Count), out int entry)
                || tail.Arguments.OfType<Expr.Call>().ToArray() is not
                    [Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.Hd, Arguments: [Expr.Variable list] } head]
                || !owner.ResolvesErlangBif(head) || parameters.Length >= FirstReservedXRegister
                || Resolve(list) is not Expr.Variable headInput || headInput.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            const string result = "\0checked-list-head";
            int headSource = Array.IndexOf(parameters, headInput.Name);
            var arguments = tail.Arguments.Select(operand => ReferenceEquals(operand, head)
                ? new Expr.Variable(result) : Resolve(operand)).ToArray();
            if (headSource < 0 || ReadEntryCall(tail with { Arguments = arguments }, [.. parameters, result]) is not { } plan)
            {
                return false;
            }

            int headArgument = Array.FindIndex(arguments, operand => operand is Expr.Variable variable && variable.Name == result);
            int headDestination = parameters.Length;
            if (!plan.Moves.Any(move => move.Source == headArgument))
            {
                headDestination = headArgument;
                plan.Moves.Remove((headArgument, parameters.Length));
            }

            int failed = Label();
            owner.EmitNonemptyList(source, failed);
            if (headSource == source)
            {
                owner.Op(GetHead, XRegisterTag, headSource);
            }
            else
            {
                owner.EmitCallLine(head, location);
                owner.Op(BifOne, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, head.Function, 1));
                owner.Operand(XRegisterTag, headSource);
            }

            owner.Operand(XRegisterTag, headDestination);
            owner.EmitOrderedTailMoves(plan.Moves, plan.Constants);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, tail.Arguments.Count);
            owner.Operand(LabelTag, entry);
            EmitFailure(failed);
            return true;

            void EmitFailure(int failure)
            {
                Mark(failure);
                var current = binding.Check.SourceLine == 0 ? location
                    : new BeamSourceLocation(binding.Check.PreprocessorFile ?? location.File, binding.Check.SourceLine);
                if (current != location)
                {
                    owner.EmitLine(current);
                }

                owner.Op(BadMatch, XRegisterTag, source);
            }
        }

        private bool TryEmitConsCallMatch(IReadOnlyList<Clause> clauses, int functionInfo)
        {
            Clause clause;
            (int Input, int Result, Term? Literal)? empty = null;
            if (clauses is [var only])
            {
                clause = only;
            }
            else if (clauses is [var first, var second])
            {
                (clause, var fallback) = HasNilParameter(first) ? (second, first) : (first, second);
                empty = ReadEntryListReturn([clause, fallback]);
                if (empty is null)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } head],
                        Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } tail
                    }],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Literal literal, Value: Expr.Call firstCall } match,
                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable last] } tailCall
                        ]
                    }
                }
                || head.Name == tail.Name || tail.Name != last.Name
                || ReadEntryCall(firstCall, [head.Name]) is not { } plan
                || !entries.TryGetValue((tailCall.Function, 1), out int entry))
            {
                return false;
            }

            int nonCons = empty is null ? functionInfo : Label();
            int matchFailure = Label();
            owner.EmitNonemptyList(0, nonCons);
            EmitFrameAllocation(1, 1);
            owner.EmitListParts(0, XRegisterTag, 0, YRegisterTag, 0);
            EmitEntryCall(plan);
            owner.Op(ExactEqualTest, LabelTag, matchFailure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(literal.Value);
            owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (empty is { } fallbackReturn)
            {
                EmitEmptyListReturn(fallbackReturn, nonCons, functionInfo);
            }

            Mark(matchFailure);
            var previous = CallLocation(firstCall, location);
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private static (int Input, int Result, Term? Literal)? ReadEntryListReturn(IReadOnlyList<Clause> clauses)
        {
            if (clauses.Count < 2 || clauses[^1] is not { Guard: null } empty)
            {
                return null;
            }

            int input = -1;
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < empty.Patterns.Count; index++)
            {
                Pattern pattern = empty.Patterns[index];
                if (ListEntryTest(pattern) == IsNil && input < 0)
                {
                    input = index;
                }
                else if (pattern is not Pattern.Variable variable
                    || (variable.Name != VariableScopeNames.Wildcard && !names.TryAdd(variable.Name, index)))
                {
                    return null;
                }
            }

            if (input < 0 || clauses.Take(clauses.Count - 1).Any(clause =>
                ListEntryTest(EntryShape(clause.Patterns[input])) != IsNonemptyList))
            {
                return null;
            }

            if (empty.Body is Expr.Variable result && names.TryGetValue(result.Name, out int register))
            {
                return (input, register, null);
            }

            return AttributeLiteral.TryNormalizeConstant(empty.Body, out Term constant)
                ? (input, -1, constant)
                : null;
        }

        private bool HasProperListEntry(FunctionDefinition function, int parameter, bool followForwarding = false)
        {
            var visiting = new HashSet<(string Name, int Arity, int Parameter)>();
            return Prove(function, parameter);

            bool Prove(FunctionDefinition target, int index)
            {
                var key = (target.Name, target.Arity, index);
                if (owner.IsDeclaredNative((target.Name, target.Arity)) || !visiting.Add(key))
                {
                    return false;
                }

                bool hasCaller = false;
                bool allProper = HasOnlyLocalCalls(target, target.Arity, (caller, call) =>
                {
                    Expr argument = call.Arguments[index];
                    if (!ReferenceEquals(caller, target))
                    {
                        hasCaller = true;
                        return argument is Expr.ListComprehension
                            || (InferPairFacts(argument) & PairFacts.ProperList) != 0
                            || followForwarding && argument is Expr.Variable variable && ProveForwarded(caller, call, variable);
                    }

                    return target.Clauses.Any(clause => (ReferenceEquals(clause.Body, call)
                        || clause.Body is Expr.Sequence { Items: [.., var last] } && ReferenceEquals(last, call)
                        || clause.Body is Expr.List { Tail: var result } && ReferenceEquals(result, call)
                        || followForwarding && clause.Body is Expr.Sequence sequence
                            && sequence.Items.Any(item => IsDirectUse(item, call)))
                        && PreservesProperTail(argument, clause.Patterns[index]));
                });
                visiting.Remove(key);

                return allProper && hasCaller;
            }

            bool ProveForwarded(FunctionDefinition caller, Expr.Call call, Expr.Variable variable)
            {
                foreach (Clause clause in caller.Clauses)
                {
                    var statements = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
                    bool properBinding = false;
                    foreach (Expr statement in statements)
                    {
                        if (IsDirectUse(statement, call))
                        {
                            int source = Array.FindIndex(clause.Patterns.ToArray(),
                                pattern => pattern is Pattern.Variable parameter && parameter.Name == variable.Name);
                            return properBinding || source >= 0 && Prove(caller, source);
                        }

                        if (statement is Expr.Match
                            {
                                Pattern: Pattern.Variable binding,
                                Value: Expr.ListComprehension
                            } && binding.Name == variable.Name)
                        {
                            properBinding = true;
                        }
                    }
                }

                return false;
            }

            static bool IsDirectUse(Expr expression, Expr.Call call) =>
                ReferenceEquals(expression, call)
                || expression is Expr.Match binding && IsDirectUse(binding.Value, call)
                || expression is Expr.Tuple tuple && tuple.Items.Any(item => IsDirectUse(item, call));

            static bool PreservesProperTail(Expr argument, Pattern source)
            {
                if (argument is Expr.List list)
                {
                    return list.Tail is null || PreservesProperTail(list.Tail, source);
                }
                if (argument is Expr.Literal { Value: Nil })
                {
                    return true;
                }
                if (source is Pattern.List { Tail: { } tail })
                {
                    return PreservesProperTail(argument, tail);
                }
                return argument is Expr.Variable forwarded && source is Pattern.Variable original
                    && original.Name != VariableScopeNames.Wildcard && original.Name == forwarded.Name;
            }
        }

        private bool TryEmitSharedConsLookahead(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    { Guard: null, Patterns:
                        [
                            Pattern.List { Items: [Pattern.Literal first, Pattern.Literal second], Tail: Pattern.Variable suffix },
                            Pattern.Variable accumulator
                        ],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments:
                            [
                                Expr.List { Items: [var restored], Tail: Expr.Variable restoredSuffix },
                                Expr.Variable kept
                            ] } skip },
                    { Guard: null, Patterns:
                        [
                            Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                            Pattern.Variable collected
                        ],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments:
                            [
                                Expr.Variable remaining,
                                Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable previous }
                            ] } collect },
                    _
                ]
                || skip.Function != function.Name || collect.Function != function.Name
                || !DistinctBindings(suffix, accumulator) || !DistinctBindings(head, tail, collected)
                || restoredSuffix.Name != suffix.Name || kept.Name != accumulator.Name
                || remaining.Name != tail.Name || item.Name != head.Name || previous.Name != collected.Name
                || !AttributeLiteral.TryNormalizeConstant(restored, out Term restoredHead) || !restoredHead.Equals(second.Value)
                || !entries.TryGetValue((function.Name, 2), out int entry))
            {
                return false;
            }

            int fallback = Label();
            int prepend = Label();
            owner.Op(IsNonemptyList, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.EmitListParts(0, XRegisterTag, 2, XRegisterTag, 0);
            TestHead(2, first.Value);
            owner.Op(IsNonemptyList, LabelTag, prepend);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetHead, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            TestHead(3, second.Value);
            Recur();

            Mark(prepend);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            Recur();
            Mark(fallback);
            return true;

            void TestHead(int register, Term literal)
            {
                owner.Op(ExactEqualTest, LabelTag, prepend);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(literal);
            }

            void Recur()
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }
        }

        private bool TryEmitConsAndNilTailCalls(IReadOnlyList<Clause> clauses, int functionInfo)
        {
            if (clauses is not [{ Guard: null, Body: Expr.Call } cons,
                { Guard: null, Body: Expr.Call emptyCall } empty]
                || !TryResolveOrdinaryCall(emptyCall, out string? module))
            {
                return false;
            }

            int input = -1;
            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < empty.Patterns.Count; index++)
            {
                if (ListEntryTest(empty.Patterns[index]) == IsNil && input < 0)
                {
                    input = index;
                }
                else if (empty.Patterns[index] is not Pattern.Variable parameter
                    || parameter.Name != VariableScopeNames.Wildcard && !registers.TryAdd(parameter.Name, index))
                {
                    return false;
                }
            }

            if (input < 0)
            {
                return false;
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int index = 0; index < emptyCall.Arguments.Count; index++)
            {
                var argument = emptyCall.Arguments[index];
                if (argument is Expr.Variable variable && registers.TryGetValue(variable.Name, out int register))
                {
                    if (register != index)
                    {
                        moves.Add((index, register));
                    }
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

            int nil = 0;
            if (!TryEmitConsEntryInRegisters(cons, input, functionInfo, EmitConsTest, reuseListInput: true))
            {
                return false;
            }

            Mark(nil);
            if (parentFunction is null || !HasProperListEntry(parentFunction, input))
            {
                owner.EmitNilTest(XRegisterTag, input, functionInfo);
            }
            owner.EmitArgumentMoves(moves, constants);
            EmitRegisterTailTarget(emptyCall with { Module = module }, emptyCall.Arguments.Count);

            return true;

            void EmitConsTest()
            {
                nil = Label();
                owner.EmitNonemptyList(input, nil);
            }
        }

        private bool TryEmitListHeadConstructedCalls(
            IReadOnlyList<Clause> clauses,
            (int Input, int Result, Term? Literal) empty,
            int functionInfo)
        {
            int input = empty.Input;
            if (clauses is not [{ Guard: null, Body: Expr.Call firstCall } first,
                { Guard: null, Body: Expr.Call secondCall } second, _]
                || first.Patterns[input] is not Pattern.List { Items: [Pattern.Literal key], Tail: Pattern.Variable firstTail }
                || second.Patterns[input] is not Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable secondTail })
            {
                return false;
            }

            int headRegister = first.Patterns.Count;
            if (headRegister >= FirstReservedXRegister
                || ReadInputs(first, firstTail) is not { } firstInputs
                || ReadInputs(second, secondTail) is not { } secondInputs
                || !Bind(secondInputs, head, headRegister)
                || !TryEmitRegisterConstructedArgument(firstCall, firstInputs, validateOnly: true)
                || !TryEmitRegisterConstructedArgument(secondCall, secondInputs, validateOnly: true))
            {
                return false;
            }

            int otherHead = Label();
            int nonCons = Label();
            owner.EmitNonemptyList(input, nonCons);
            owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, input);
            owner.Op(ExactEqualTest, LabelTag, otherHead);
            owner.Operand(XRegisterTag, headRegister);
            owner.LiteralOperand(key.Value);
            TryEmitRegisterConstructedArgument(firstCall, firstInputs);
            Mark(otherHead);
            TryEmitRegisterConstructedArgument(secondCall, secondInputs);
            EmitEmptyListReturn(empty, nonCons, functionInfo);

            return true;

            Dictionary<string, int>? ReadInputs(Clause clause, Pattern.Variable tail)
            {
                var registers = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    Pattern pattern = index == input ? tail : clause.Patterns[index];
                    if (pattern is not Pattern.Variable variable || !Bind(registers, variable, index))
                    {
                        return null;
                    }
                }

                return registers;
            }

            static bool Bind(Dictionary<string, int> registers, Pattern.Variable variable, int register) =>
                variable.Name == VariableScopeNames.Wildcard || registers.TryAdd(variable.Name, register);
        }

        private bool TryEmitListHeadTail(
            IReadOnlyList<Clause> clauses,
            (int Input, int Result, Term? Literal) empty,
            int functionInfo)
        {
            int input = empty.Input;
            if (clauses is not [{ Body: Expr.Literal or Expr.Variable } first,
                { Guard: null, Body: Expr.Call { Module: null, DynamicRemote: false } call } fallback, _]
                || first.Patterns[input] is not Pattern.List { Items: [var head], Tail: Pattern.Variable firstTail }
                || fallback.Patterns[input] is not Pattern.List
                    { Items: [Pattern.Variable ignoredHead], Tail: Pattern.Variable tail }
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int entry))
            {
                return false;
            }

            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            var fallbackBindings = new Dictionary<string, int>(StringComparer.Ordinal);
            var tests = new List<Action<int>>();
            int headRegister = first.Patterns.Count;
            if (headRegister >= FirstReservedXRegister)
            {
                return false;
            }

            int nextRegister = headRegister + 1;
            for (int index = 0; index < first.Patterns.Count; index++)
            {
                if (index == input)
                {
                    continue;
                }

                if (first.Patterns[index] is not Pattern.Variable parameter
                    || fallback.Patterns[index] is not Pattern.Variable fallbackParameter
                    || !ReadPattern(parameter, index)
                    || !BindFallback(fallbackParameter, index))
                {
                    return false;
                }
            }

            if (!BindFallback(tail, input)
                || ignoredHead.Name != VariableScopeNames.Wildcard && fallbackBindings.ContainsKey(ignoredHead.Name)
                || !ReadPattern(firstTail, input) || !ReadPattern(head, headRegister))
            {
                return false;
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                if (call.Arguments[index] is Expr.Variable argument && fallbackBindings.TryGetValue(argument.Name, out int source))
                {
                    if (source != index)
                    {
                        moves.Add((index, source));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out Term constant))
                {
                    constants.Add((index, constant));
                }
                else
                {
                    return false;
                }
            }

            if (first.Guard is not null)
            {
                if (first.Guard is not Expr.Binary { Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual,
                        Left: Expr.Variable tested } comparison
                    || !bindings.TryGetValue(tested.Name, out int register)
                    || !AttributeLiteral.TryNormalizeConstant(comparison.Right, out Term operand))
                {
                    return false;
                }

                AddEquality(register, operand, comparison.Operator == ErlangOperators.ExactEqual);
            }

            int resultRegister = 0;
            if (first.Body is Expr.Variable result && !bindings.TryGetValue(result.Name, out resultRegister))
            {
                return false;
            }

            int tailCall = Label();
            int nonCons = Label();
            owner.EmitNonemptyList(input, nonCons);
            owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, input);
            foreach (var test in tests)
            {
                test(tailCall);
            }

            if (first.Body is Expr.Literal literal)
            {
                owner.EmitConstant(literal.Value, 0);
            }
            else if (resultRegister != 0)
            {
                owner.MoveRegister(XRegisterTag, resultRegister, XRegisterTag, 0);
            }

            owner.instructions.WriteByte(Return);
            Mark(tailCall);
            owner.EmitArgumentMoves(moves, constants);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, entry);
            EmitEmptyListReturn(empty, nonCons, functionInfo);

            return true;

            bool BindFallback(Pattern.Variable variable, int register) =>
                variable.Name == VariableScopeNames.Wildcard || fallbackBindings.TryAdd(variable.Name, register);

            bool ReadPattern(Pattern pattern, int source)
            {
                switch (pattern)
                {
                    case Pattern.Variable variable:
                        if (variable.Name != VariableScopeNames.Wildcard && !bindings.TryAdd(variable.Name, source))
                        {
                            int bound = bindings[variable.Name];
                            var range = bound < headRegister && bound != input && parentFunction is { } function
                                ? ReadPrivateIntegerBounds(function, bound)
                                : null;
                            tests.Add(failure =>
                            {
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, source);
                                if (range is { } bounds)
                                {
                                    owner.EmitTypedRegister(bound, IntegerTypeBits, bounds.Lower, bounds.Upper);
                                }
                                else
                                {
                                    owner.Operand(XRegisterTag, bound);
                                }
                            });
                        }

                        return true;
                    case Pattern.Literal literal:
                        AddEquality(source, literal.Value, exactEqual: true);
                        return true;
                    case AliasPattern { Left: Pattern.Variable alias, Right: Pattern.Tuple rightTuple }:
                        return ReadPattern(alias, source) && ReadPattern(rightTuple, source);
                    case AliasPattern { Left: Pattern.Tuple leftTuple, Right: Pattern.Variable alias }:
                        return ReadPattern(alias, source) && ReadPattern(leftTuple, source);
                    case Pattern.Tuple tuple:
                        bool tagged = HasTupleTag(tuple, out Atom tag);
                        if (tagged)
                        {
                            tests.Add(failure => EmitDetailRecordTest(source, tuple.Items.Count, tag, failure));
                        }
                        else
                        {
                            tests.Add(failure => EmitTupleShapeTest(XRegisterTag, source, tuple.Items.Count, failure));
                        }

                        var fields = Enumerable.Range(tagged ? 1 : 0, tuple.Items.Count - (tagged ? 1 : 0))
                            .Where(index => tuple.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                            .ToArray();
                        for (int offset = 0; offset < fields.Length; offset++)
                        {
                            int index = fields[offset];
                            int destination = offset == fields.Length - 1 && !bindings.ContainsValue(source)
                                ? source : nextRegister++;
                            if (destination >= FirstReservedXRegister)
                            {
                                return false;
                            }

                            tests.Add(_ => owner.EmitTuplePart(source, index, destination));
                            if (!ReadPattern(tuple.Items[index], destination))
                            {
                                return false;
                            }
                        }

                        return true;
                    default:
                        return false;
                }
            }

            void AddEquality(int register, Term value, bool exactEqual)
            {
                tests.Add(failure =>
                {
                    owner.Op(exactEqual ? ExactEqualTest : ExactNotEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, register);
                    owner.LiteralOperand(value);
                });
            }
        }

        private (long Lower, long Upper)? ReadPrivateIntegerBounds(FunctionDefinition function, int parameter)
        {
            var target = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(target))
            {
                return null;
            }

            long lower = long.MaxValue;
            long upper = long.MinValue;
            foreach (var caller in localFunctions.Values)
            {
                foreach (var clause in caller.Clauses)
                {
                    foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk))
                    {
                        if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == target)
                        {
                            return null;
                        }

                        if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                            || (call.Function, call.Arguments.Count) != target)
                        {
                            continue;
                        }

                        if (call.Arguments[parameter] is Expr.Literal { Value: Integer literal }
                            && literal.Value >= long.MinValue && literal.Value <= long.MaxValue)
                        {
                            long value = (long)literal.Value;
                            lower = Math.Min(lower, value);
                            upper = Math.Max(upper, value);
                        }
                        else if (!ReferenceEquals(caller, function) || !ReferenceEquals(clause.Body, call)
                            || clause.Patterns[parameter] is not Pattern.Variable input
                            || call.Arguments[parameter] is not Expr.Variable forwarded || input.Name != forwarded.Name)
                        {
                            return null;
                        }
                    }
                }
            }

            return lower <= upper ? (lower, upper) : null;
        }

        private bool TryEmitConsEntryInRegisters(
            Clause clause,
            int input,
            int failure,
            Action? emitEntryTest = null,
            bool reuseListInput = false,
            bool allowFramedBody = false
        )
        {
            if (clause.Guard is not null || clause.Patterns[input] is not Pattern.List
                {
                    Items: [var head], Tail: Pattern.Variable tail
                } || clause.Patterns.Count + 2 >= FirstReservedXRegister)
            {
                return false;
            }

            var bindings = new List<(Pattern.Variable Variable, int Register)>();
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (index == input)
                {
                    continue;
                }

                if (clause.Patterns[index] is not Pattern.Variable parameter)
                {
                    return false;
                }

                bindings.Add((parameter, index));
            }

            switch (head)
            {
                case Pattern.Tuple tuple:
                    return TryEmitTupleHeadInRegisters(clause, input, failure, tuple, tail, bindings, emitEntryTest, reuseListInput, allowFramedBody);
                case AliasPattern { Left: Pattern.Tuple left, Right: Pattern.Variable right }:
                    bindings.Add((right, clause.Patterns.Count));
                    return TryEmitTupleHeadInRegisters(clause, input, failure, left, tail, bindings, emitEntryTest, reuseListInput, allowFramedBody);
                case AliasPattern { Left: Pattern.Variable leftVariable, Right: Pattern.Tuple rightTuple }:
                    bindings.Add((leftVariable, clause.Patterns.Count));
                    return TryEmitTupleHeadInRegisters(clause, input, failure, rightTuple, tail, bindings, emitEntryTest, reuseListInput, allowFramedBody);
            }

            if (head is not Pattern.Variable headVariable)
            {
                return false;
            }

            int headRegister = clause.Patterns.Count;
            int tailRegister = headRegister + 1;
            bindings.Add((headVariable, headRegister));
            bindings.Add((tail, tailRegister));
            var named = bindings.Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard).ToArray();
            if (!DistinctBindings(named.Select(binding => binding.Variable).ToArray()))
            {
                return false;
            }

            var referenced = ReadUsedEntryParameters(new Clause(
                named.Select(binding => (Pattern)binding.Variable).ToArray(), null, clause.Body));
            var used = named.Where(binding => referenced is null || referenced.Contains(binding.Variable.Name)).ToArray();
            bool usesHead = used.Any(binding => binding.Register == headRegister);
            bool usesTail = used.Any(binding => binding.Register == tailRegister);
            if (clause.Body is Expr.Call call)
            {
                var registers = used.ToDictionary(binding => binding.Variable.Name, binding => binding.Register,
                    StringComparer.Ordinal);
                if (usesTail)
                {
                    registers[tail.Name] = input;
                }
                else if (usesHead)
                {
                    registers[headVariable.Name] = input;
                }

                if (TryEmitRegisterConstructedArgument(call, registers, (clause.Patterns.Count, EmitListExtraction), emitEntryTest))
                {
                    return true;
                }

                if (reuseListInput && emitEntryTest is not null)
                {
                    var inputs = Enumerable.Repeat(VariableScopeNames.Wildcard, tailRegister + 1).ToArray();
                    foreach (var (name, register) in registers)
                    {
                        inputs[register] = name;
                    }

                    if (ReadEntryCall(call, inputs) is { } ordinary)
                    {
                        emitEntryTest();
                        EmitListExtraction();
                        EmitEntryCallArguments(ordinary);
                        EmitRegisterTailTarget(call with { Module = ordinary.Module }, call.Arguments.Count);
                        return true;
                    }
                }
            }

            if (emitEntryTest is not null && !allowFramedBody)
            {
                return false;
            }

            if (clause.Body is Expr.List { Tail: Expr.Call })
            {
                var direct = used.Select(binding => binding.Register == tailRegister
                    || (!usesTail && binding.Register == headRegister)
                        ? (binding.Variable, input) : binding).ToArray();
                if (TryEmitConsCallResult(clause.Body, direct, EmitListExtraction,
                    (input, headRegister, clause.Patterns.Count, usesHead && usesTail), emitEntryTest))
                {
                    return true;
                }
            }

            emitEntryTest?.Invoke();
            if (usesHead && usesTail)
            {
                owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, tailRegister);
            }
            else if (usesHead || usesTail)
            {
                owner.Op(usesHead ? GetHead : GetTail, XRegisterTag, input);
                owner.Operand(XRegisterTag, input);
                used[^1] = (usesHead ? headVariable : tail, input);
            }

            EmitConsEntryBody(clause, used);
            return true;

            void EmitListExtraction()
            {
                if (usesHead && usesTail)
                {
                    owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, input);
                }
                else if (usesHead || usesTail)
                {
                    owner.Op(usesHead ? GetHead : GetTail, XRegisterTag, input);
                    owner.Operand(XRegisterTag, input);
                }
            }
        }

        private bool TryEmitTupleHeadInRegisters(
            Clause clause, int input, int failure, Pattern.Tuple tuple, Pattern.Variable tail,
            List<(Pattern.Variable Variable, int Register)> bindings, Action? emitEntryTest, bool reuseListInput, bool allowFramedBody)
        {
            int headRegister = clause.Patterns.Count;
            int tailRegister = headRegister + 1;
            int nextRegister = tailRegister + 1;
            int firstMember = HasTupleTag(tuple, out Atom tag) ? 1 : 0;
            bindings.Add((tail, tailRegister));
            var fields = new List<(int Index, int Register)>();
            var tests = new List<(int Index, Term Literal)>();
            for (int index = firstMember; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable:
                        bindings.Add((variable, nextRegister));
                        fields.Add((index, nextRegister++));
                        break;
                    case Pattern.Literal { Value: Atom or Integer or Nil } literal:
                        tests.Add((index, literal.Value));
                        break;
                    default:
                        return false;
                }
            }

            var named = bindings.Where(binding => binding.Variable.Name != VariableScopeNames.Wildcard).ToArray();
            if (nextRegister >= FirstReservedXRegister
                || !DistinctBindings(named.Select(binding => binding.Variable).ToArray()))
            {
                return false;
            }

            var referenced = ReadUsedEntryParameters(new Clause(
                named.Select(binding => (Pattern)binding.Variable).ToArray(), null, clause.Body));
            var used = named.Where(binding => referenced is null || referenced.Contains(binding.Variable.Name)).ToArray();
            bool usesTail = used.Any(binding => binding.Register == tailRegister);
            if (clause.Body is Expr.Call call)
            {
                var registers = used.ToDictionary(binding => binding.Variable.Name, binding => binding.Register,
                    StringComparer.Ordinal);
                var extracted = fields.Where(field => used.Any(binding => binding.Register == field.Register)).ToArray();
                bool reuseHead = !registers.ContainsValue(headRegister);
                int lastDestination = reuseListInput ? input : headRegister;
                if (reuseHead && extracted.Length != 0)
                {
                    int last = extracted[^1].Register;
                    string name = used.First(binding => binding.Register == last).Variable.Name;
                    registers[name] = lastDestination;
                }

                if (TryEmitRegisterConstructedArgument(call, registers, (tailRegister + 1, EmitFields), () => EmitTests(extractTail: true)))
                {
                    return true;
                }

                void EmitFields()
                {
                    for (int index = 0; index < extracted.Length; index++)
                    {
                        var (field, register) = extracted[index];
                        owner.EmitTuplePart(headRegister, field, reuseHead && index == extracted.Length - 1 ? lastDestination : register);
                    }
                }
            }

            if (emitEntryTest is not null && !allowFramedBody)
            {
                return false;
            }

            EmitTests(usesTail);
            if (!usesTail && clause.Body is Expr.Variable returned
                && Array.Find(used, binding => binding.Variable.Name == returned.Name) is { Variable: not null } result)
            {
                int field = fields.FindIndex(item => item.Register == result.Register);
                if (field >= 0)
                {
                    owner.EmitTuplePart(headRegister, fields[field].Index, 0);
                }
                else if (result.Register != 0)
                {
                    owner.MoveRegister(XRegisterTag, result.Register, XRegisterTag, 0);
                }

                owner.instructions.WriteByte(Return);
                return true;
            }

            // Keep the X registers contiguous for the following allocation's live count.
            int destination = usesTail ? tailRegister + 1 : tailRegister;
            foreach (var (index, register) in fields)
            {
                int bindingIndex = Array.FindIndex(used, binding => binding.Register == register);
                if (bindingIndex >= 0)
                {
                    owner.EmitTuplePart(headRegister, index, destination);
                    used[bindingIndex] = (used[bindingIndex].Variable, destination++);
                }
            }

            EmitConsEntryBody(clause, used);
            return true;

            void EmitTests(bool extractTail)
            {
                emitEntryTest?.Invoke();
                if (extractTail)
                {
                    owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, tailRegister);
                }
                else
                {
                    owner.Op(GetHead, XRegisterTag, input);
                    owner.Operand(XRegisterTag, headRegister);
                }
                if (firstMember == 1)
                {
                    EmitDetailRecordTest(headRegister, tuple.Items.Count, tag, failure);
                }
                else
                {
                    EmitTupleShapeTest(XRegisterTag, headRegister, tuple.Items.Count, failure);
                }

                foreach (var (index, literal) in tests)
                {
                    owner.EmitTuplePart(headRegister, index, nextRegister);
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, nextRegister);
                    owner.LiteralOperand(literal);
                }
            }
        }

        private void EmitConsEntryBody(Clause clause, (Pattern.Variable Variable, int Register)[] used, bool reuseCallSlots = false)
        {
            if (clause.Body is Expr.Call call && TryEmitRegisterConstructedArgument(call,
                used.ToDictionary(binding => binding.Variable.Name, binding => binding.Register, StringComparer.Ordinal)))
            {
                return;
            }

            if (TryEmitConsCallResult(clause.Body, used))
            {
                return;
            }

            int live = used.Length == 0 ? 0 : used.Max(binding => binding.Register) + 1;
            EmitFrameAllocation(live, frameSize);
            EmitFrameInitialization(used.Length, frameSize);
            foreach (var (variable, register) in used)
            {
                int slot = Slot();
                owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
                variables.Add(variable.Name, slot);
            }

            if (!TailLocal(clause.Body, reuseCallSlots))
            {
                EmitReturnExpression(clause.Body, reuseCallSlots);
                owner.EmitFrameReturn(frameSize);
            }

        }

        private bool TryEmitConsCallResult(Expr body, (Pattern.Variable Variable, int Register)[] used,
            Action? emitBindings = null,
            (int Input, int Head, int Arity, bool Both)? cons = null,
            Action? emitEntryTest = null)
        {
            int live = used.Length == 0 ? 0 : used.Max(binding => binding.Register) + 1;
            var inputs = Enumerable.Repeat(VariableScopeNames.Wildcard, live).ToArray();
            foreach (var (variable, register) in used)
            {
                inputs[register] = variable.Name;
            }

            if (ReadConsCallResult(body, inputs, cons is { Both: true } pair ? pair.Head : -1,
                cons is { Both: true } list ? list.Input : -1) is not { } result)
            {
                return false;
            }

            emitEntryTest?.Invoke();
            EmitConsCallResult(result, cons?.Arity ?? live, emitBindings, cons?.Input ?? 0);
            return true;
        }

        private sealed record ConsCallResult(
            EntryCall Call,
            int[] Saved,
            Term? Literal,
            EntryConstructedArgument? Tuple,
            bool ExtractHead,
            int? DeferredField = null,
            ConsHeadCall? HeadCall = null
        );

        private ConsCallResult? ReadConsCallResult(Expr body, string[] inputs, int directHead = -1, int listInput = -1)
        {
            if (body is not Expr.List { Items: [var head], Tail: Expr.Call call })
            {
                return null;
            }

            int headRegister = head is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                ? Array.IndexOf(inputs, variable.Name) : -1;
            Term? literal = null;
            EntryConstructedArgument? tuple = null;
            int[] saved;
            if (headRegister >= 0)
            {
                saved = [headRegister];
            }
            else if (AttributeLiteral.TryNormalizeConstant(head, out literal))
            {
                saved = [];
            }
            else if (directHead < 0 && ReadEntryConstructedArgument(head, 1, inputs) is { IsTuple: true } constructed)
            {
                tuple = constructed;
                saved = constructed.Operands.Where(operand => operand.Source.HasValue)
                    .Select(operand => operand.Source!.Value).Distinct().OrderDescending().ToArray();
            }
            else
            {
                return ReadComputedConsHead(head, call, inputs, directHead, listInput);
            }

            bool extractHead = headRegister >= 0 && headRegister == directHead;
            string[]? savedInputs = null;
            if (extractHead)
            {
                savedInputs = [inputs[headRegister]];
                inputs = (string[])inputs.Clone();
                inputs[headRegister] = VariableScopeNames.Wildcard;
            }

            return ReadEntryCall(call, inputs, savedInputs) is { } plan
                ? new(plan, saved, literal, tuple, extractHead) : null;
        }

        private void EmitConsCallResult(
            ConsCallResult result,
            int live,
            Action? emitBindings = null,
            int listInput = 0,
            BeamSourceLocation? previous = null
        )
        {
            if (result.HeadCall is { } headCall)
            {
                EmitComputedConsHead(result, headCall, live, emitBindings, listInput, previous);
                return;
            }

            int frame = result.Saved.Length;
            EmitFrameAllocation(live, frame);
            if (result.ExtractHead)
            {
                owner.EmitListParts(listInput, YRegisterTag, 0, XRegisterTag, listInput);
            }
            else
            {
                emitBindings?.Invoke();
                for (int slot = 0; slot < frame; slot++)
                {
                    owner.MoveRegister(XRegisterTag, result.Saved[slot], YRegisterTag, slot);
                }
            }

            var call = result.Call;
            owner.EmitOrderedTailMoves(call.Moves, call.Constants, call.SavedInputs);
            owner.EmitCallLine(call.Call, previous ?? location);
            Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + (result.Tuple?.HeapWords ?? 0));
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (result.DeferredField is int field)
            {
                owner.Op(GetTupleElement, YRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, 1);
            }

            if (result.Tuple is { } tuple)
            {
                owner.EmitTupleHeader(XRegisterTag, tuple.Operands.Length, tuple.Register);
                foreach (var (source, constant) in tuple.Operands)
                {
                    if (source is int register)
                    {
                        if (result.DeferredField.HasValue)
                        {
                            owner.Operand(XRegisterTag, 1);
                        }
                        else
                        {
                            owner.Operand(YRegisterTag, Array.IndexOf(result.Saved, register));
                        }
                    }
                    else
                    {
                        owner.LiteralOperand(constant!);
                    }
                }

                owner.Op(PutList, XRegisterTag, tuple.Register);
            }
            else if (result.DeferredField.HasValue)
            {
                owner.Op(PutList, XRegisterTag, 1);
            }
            else if (frame != 0)
            {
                owner.Op(PutList, YRegisterTag, 0);
            }
            else
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(result.Literal!);
            }

            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);
        }

        private void EmitEmptyListReturn(
            (int Input, int Result, Term? Literal) empty,
            int label,
            int failure,
            bool? properList = null)
        {
            Mark(label);
            if (!(properList ?? (parentFunction is not null && HasProperListEntry(parentFunction, empty.Input))))
            {
                owner.EmitNilTest(XRegisterTag, empty.Input, failure);
            }

            if (empty.Literal is { } literal)
            {
                if (empty.Input != 0 || literal is not Nil)
                {
                    owner.EmitConstant(literal, 0);
                }
            }
            else if (empty.Result != 0)
            {
                owner.MoveRegister(XRegisterTag, empty.Result, XRegisterTag, 0);
            }

            owner.instructions.WriteByte(Return);
        }

        private bool TryEmitEntryListDefault(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence { Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable binding,
                            Value: Expr.Case { Value: Expr.Variable tested, Clauses: [var empty, var nonempty] } decision
                        },
                        _, ..
                    ] } sequence
                }
                || !DistinctBindings(input, binding) || tested.Name != input.Name
                || empty is not { Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }] }
                || nonempty is not { Guard: null, Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    }], Body: Expr.Variable passed }
                || passed.Name != input.Name
                || !AttributeLiteral.TryNormalizeConstant(empty.Body, out Term defaultValue))
            {
                return false;
            }

            var continuation = new Expr.Sequence(sequence.Items.Skip(1).ToArray());
            if (ReadUsedEntryParameters(new Clause(clause.Patterns, null, continuation)) is not { Count: 0 })
            {
                return false;
            }

            int nil = Label();
            int join = Label();
            int failed = Label();
            owner.Op(IsNonemptyList, LabelTag, nil);
            owner.Operand(XRegisterTag, 0);
            JumpTo(join);
            Mark(nil);
            owner.Op(IsNil, LabelTag, failed);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(defaultValue, 0);
            Mark(join);

            EmitFrameAllocation(1, frameSize);
            EmitFrameInitialization(1, frameSize);
            variables.Add(binding.Name, Save());
            if (!TailLocal(continuation))
            {
                Expression(continuation);
                owner.EmitFrameReturn(frameSize);
            }

            Mark(failed);
            owner.EmitLine(decision.SourceLine == 0 ? location
                : new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
            return true;
        }
    }
}
