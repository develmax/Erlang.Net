using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<(string Name, int Arity), (string Name, int? Arity)[]> recursiveDefinitionOrder = [];
    private bool hasMixedRecursiveDefinitions;
    private const string PairedListAppendTail = "$list_generator_append_tail";
    private readonly Dictionary<(string Name, int Arity, int CoreOrdinal), int> recursiveClosureFailureOrdinals = [];

    private (FunctionDefinition Parent, IReadOnlyList<(FunctionDefinition Function, BeamSourceLocation Location)> Helpers)? ExtractRecursiveListComprehensions(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        var expressions = function.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body)).ToArray();
        var comprehensions = expressions.OfType<Expr.ListComprehension>().ToArray();
        var anonymous = expressions.OfType<Expr.Fun>().ToArray();
        var mapSibling = expressions.OfType<Expr.MapComprehension>().FirstOrDefault();
        var mapOrdinals = mapSibling is null ? null : ReadMapListDefinitionOrdinals(
            function,
            expressions,
            comprehensions,
            mapSibling
        );
        if (comprehensions.Length == 0 || anonymous.Any(fun => fun.SelfName is not null)
            || mapSibling is not null && mapOrdinals is null
            || expressions.Any(expression => expression is Expr.BinaryComprehension)
            || ReadListMappingPlan(function.Clauses) is not null
            || ReadTupleMappingCase(function) is not null
            || ReadDiscardedListCalls(function, localFunctions) is not null
            || ReadPageComprehension(function, localFunctions) is not null
            || ReadCapturedTableList(function, localFunctions) is not null)
        {
            return null;
        }

        var nestedClosureMapping = comprehensions is [var nestedComprehension] && anonymous.Length != 0
            ? ReadDirectClosureListMapping(
                function,
                anonymous,
                nestedComprehension,
                localFunctions
            )
            : null;
        Expr[] core = comprehensions;
        var ssa = comprehensions;
        var helperOrdinals = Enumerable.Range(mapOrdinals?.Ssa ?? 0, comprehensions.Length).ToArray();
        var failureOrdinals = new Dictionary<Expr.Fun, int>(ReferenceEqualityComparer.Instance);
        Expr[] definitions = comprehensions;
        bool independent = comprehensions.Length > 1 || anonymous.Length != 0;
        bool allowStrict = !independent || function.Clauses.Count == 1 && anonymous.Length == 0;
        bool separatedTupleDefinitions = anonymous.Length != 0 && HasSeparatedTupleOwnerDefinitions(function.Clauses);
        bool orderedTupleDefinitions = false;
        bool nestedCaseSiblings = anonymous.Length == 0 && SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
            && HasBooleanCaseNestedListSiblings(function, comprehensions);
        bool nestedSiblings = nestedCaseSiblings || anonymous.Length == 0 && SsaTypeInferenceEnabled
            && !ModuleOptimizationDisabled && HasSequentialNestedListGenerators(function, comprehensions);
        var pairedAppend = SsaTypeInferenceEnabled && !ModuleOptimizationDisabled && comprehensions.Length == 2
            && anonymous.Length == 0 && function.Clauses is [{ Guard: null } appendClause]
            && HasUnconditionalParameters(appendClause)
            ? ReadPairedListAppend(expressions, appendClause)
            : null;
        if (pairedAppend is { } pair)
        {
            // v3_core evaluates and numbers the right-hand comprehension before
            // lowering the left-hand generator with that list as its tail.
            core = [pair.Right, pair.Left];
            ssa = [pair.Right, pair.Left];
            definitions = core;
        }
        else if (independent)
        {
            if (LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk)
                .Any(expression => expression is Expr.Try or Expr.Receive or Expr.Maybe
                    or Expr.BinaryComprehension or Expr.MapComprehension))
            {
                return null;
            }

            core = WithNestedList(OrderedLocalDefinitions(function.Clauses, ListComprehensionOrder.Core)).ToArray();
            var tupleCase = anonymous.Length == 0 && SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
                ? ReadLiteralTupleComprehensionCase(function.Clauses) : null;
            var constructorOrder = tupleCase is null && SsaTypeInferenceEnabled
                && !ModuleOptimizationDisabled
                && !OrderComprehensionClauses(function.Clauses, ListComprehensionOrder.Ssa).Any()
                    ? ReadConstructorColumnComprehensionOrder(function.Clauses) : null;
            var ssaDefinitions = Ordered(ListComprehensionOrder.Ssa).ToArray();
            definitions = Ordered(ListComprehensionOrder.Definition).ToArray();
            if (SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
                && core.Length == comprehensions.Length + anonymous.Length
                && (ssaDefinitions.Length != core.Length || definitions.Length != core.Length)
                && (anonymous.Length == 0 || separatedTupleDefinitions || HasSingleComprehensionClosureClause())
                && CanExpandComprehensionHead(function.Clauses))
            {
                IReadOnlySet<string> emptyScope = new HashSet<string>(StringComparer.Ordinal);
                ssaDefinitions = OrderedLocalDefinitions(
                    function.Clauses,
                    ListComprehensionOrder.Ssa,
                    emptyScope,
                    functionHead: true
                ).ToArray();
                definitions = OrderedLocalDefinitions(
                    function.Clauses,
                    ListComprehensionOrder.Definition,
                    emptyScope,
                    functionHead: true
                ).ToArray();
                orderedTupleDefinitions = separatedTupleDefinitions && ssaDefinitions.Length == core.Length
                    && definitions.Length == core.Length;
            }

            ssa = ssaDefinitions.OfType<Expr.ListComprehension>().ToArray();
            int ssaCounter = 0;
            var positions = new List<int>(ssa.Length);
            foreach (Expr definition in ssaDefinitions)
            {
                if (definition is Expr.ListComprehension)
                {
                    positions.Add(ssaCounter);
                    ssaCounter += DefinitionWidth(definition);
                }
                else if (definition is Expr.Fun closure
                    && !closure.Clauses.Any(clause => clause.Guard is null && HasUnconditionalParameters(clause)))
                {
                    // beam_core_to_ssa gives a nonexhaustive fun's match_fail
                    // its name from the same counter as a following letrec.
                    failureOrdinals.Add(closure, ssaCounter++);
                }
            }

            helperOrdinals = positions.ToArray();

            bool HasSingleComprehensionClosureClause() =>
                anonymous.Length == 1 && comprehensions.Length == 1 && function.Clauses.Count > 1
                // Reordering the constructor alternatives cannot interleave definitions from other clauses.
                && function.Clauses.Any(clause => clause.Guard is null
                    && OrderedLocalDefinitions(clause.Body, ListComprehensionOrder.Core).Count() == core.Length);

            IEnumerable<Expr> Ordered(ListComprehensionOrder order)
            {
                if (tupleCase is not null)
                {
                    return OrderLiteralTupleComprehensionCase(tupleCase, order);
                }

                if (constructorOrder is { } grouped)
                {
                    var clauses = order == ListComprehensionOrder.Ssa ? grouped.Ssa : grouped.Definition;

                    return clauses.SelectMany(clause => OrderedLocalDefinitions(clause.Body, order));
                }

                return WithNestedList(OrderedLocalDefinitions(function.Clauses, order));
            }

            // A nested closure or comprehension needs a separate scope and SSA traversal.
            if (core.Length != comprehensions.Length + anonymous.Length || ssa.Length != comprehensions.Length
                || definitions.Length != core.Length)
            {
                return null;
            }
        }

        var names = new Dictionary<Expr, int>(ReferenceEqualityComparer.Instance);
        int coreOrdinal = mapOrdinals?.Core ?? 0;
        foreach (var definition in core)
        {
            if (!names.TryAdd(definition, coreOrdinal))
            {
                return null;
            }

            coreOrdinal += DefinitionWidth(definition);
        }

        var helpers = new Dictionary<Expr, (FunctionDefinition Function, BeamSourceLocation Location)[]>(ReferenceEqualityComparer.Instance);
        var replacements = new Dictionary<Expr, Expr>(ReferenceEqualityComparer.Instance);
        var staticMappers = new Dictionary<Expr.ListComprehension, string>(ReferenceEqualityComparer.Instance);
        if (anonymous.Length != 0)
        {
            var folded = FoldImmutableClosureOperands(function.Clauses);
            foreach (var fun in anonymous)
            {
                replacements.Add(fun, folded.GetValueOrDefault(fun, fun) with { CoreOrdinal = names[fun] });
            }
        }

        // Keep the planned Core and SSA ordinals for unlowered siblings.
        // A lone strict generator also has one letrec, with mismatch handled in that helper.
        bool allowPartial = nestedSiblings || independent && anonymous.Length == 0 && comprehensions.All(comprehension =>
            comprehension.Qualifiers is [ComprehensionQualifier.Generator, ..]
            && (comprehension.Qualifiers.Count == 1
                || comprehension.Qualifiers[0] is ComprehensionQualifier.Generator { Strict: false }
                    && comprehension.Qualifiers.Skip(1).All(qualifier => qualifier is ComprehensionQualifier.Filter)));
        bool loweredNested = false;
        for (int index = 0; index < ssa.Length; index++)
        {
            var comprehension = ssa[index];
            bool discarded = expressions.OfType<Expr.Sequence>().Any(sequence => sequence.Items.SkipLast(1)
                .Any(expression => ReferenceEquals(expression, comprehension)
                    || expression is Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard }, Value: var assigned
                    } && ReferenceEquals(assigned, comprehension)));
            if (discarded && !IsDiscardedListEffect(comprehension)
                || !discarded && expressions.OfType<Expr.Match>().Any(match =>
                    match.Pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    && ReferenceEquals(match.Value, comprehension)))
            {
                if (!allowPartial)
                {
                    return null;
                }

                continue;
            }

            var clause = nestedClosureMapping?.Clause ?? function.Clauses.First(candidate =>
                LocalFunctionEntries.Walk(candidate.Body).Any(expression => ReferenceEquals(expression, comprehension)));
            string name = MappingName(
                function.Name,
                function.Arity,
                names[comprehension],
                helperOrdinals[index]
            );
            if (DefinitionWidth(comprehension) == 2)
            {
                if ((nestedCaseSiblings || comprehension.Qualifiers[0] is ComprehensionQualifier.Generator { Pattern: Pattern.Tuple })
                    && PlanRecursiveListGenerators(
                        function,
                        comprehension,
                        clause,
                        location,
                        name,
                        MappingName(
                            function.Name,
                            function.Arity,
                            names[comprehension] + 1,
                            helperOrdinals[index] + 1
                        ),
                        declared,
                        localFunctions,
                        allowTaggedTupleOuter: true
                    ) is { } nested)
                {
                    helpers.Add(comprehension, [nested.Inner, nested.Outer]);
                    replacements.Add(comprehension, nested.Call);
                    loweredNested = true;
                }

                continue;
            }

            var append = !independent ? ReadBoundComprehensionAppend(clause, comprehension) : null;
            Expr.Call? computedTail = null;
            Expr.Variable? appendTail = append?.Right as Expr.Variable;
            if (pairedAppend is { } fused && ReferenceEquals(comprehension, fused.Left))
            {
                if (replacements.GetValueOrDefault(fused.Right) is not Expr.Call rightHelper)
                {
                    return null;
                }

                append = fused.Append;
                appendTail = new Expr.Variable(PairedListAppendTail);
                computedTail = rightHelper;
            }

            var plan = PlanRecursiveListComprehension(
                comprehension,
                clause,
                location,
                name,
                localFunctions,
                independent,
                discarded,
                allowStrict,
                appendTail,
                allowGuardedAliasSibling: orderedTupleDefinitions
            );
            if (plan is not { } mapping || declared.Contains((name, mapping.Helper.Arity)))
            {
                if (!allowPartial)
                {
                    return null;
                }

                continue;
            }

            helpers.Add(comprehension, [(mapping.Helper, mapping.Location)]);
            var replacement = mapping.Call;
            if (computedTail is not null)
            {
                replacement = replacement with
                {
                    Arguments = replacement.Arguments.Select(argument =>
                        argument is Expr.Variable { Name: PairedListAppendTail } ? computedTail : argument).ToArray()
                };
            }

            if (nestedClosureMapping is { } nestedOwner)
            {
                var rewrittenCallback = (Expr.Fun)replacements[nestedOwner.Closure];
                replacements[nestedOwner.Closure] = rewrittenCallback with { Clauses = [nestedOwner.Clause with { Body = replacement }] };
            }
            else
            {
                replacements.Add((Expr?)append ?? comprehension, replacement);
            }
            if (SsaTypeInferenceEnabled && !ModuleOptimizationDisabled && !discarded && append is null
                && mapping.Helper.Arity == 2 && comprehension is
                {
                    Items: [Expr.Apply { Function: Expr.Variable callback, Arguments: [Expr.Variable argument] } mapped],
                    Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable item }]
                }
                && argument.Name == item.Name && callback.Name != item.Name
                && (mapped.SourceLine == 0 || mapped.SourceLine == mapping.Location.Line)
                && ReadClosedListClosure(clause, comprehension, callback.Name) is { } closure
                && names.TryGetValue(closure, out int ordinal))
            {
                staticMappers.Add(comprehension, AnonymousClosureName(function, ordinal));
            }
        }

        if (helpers.Count == 0 || nestedSiblings && !loweredNested
            || (nestedCaseSiblings || pairedAppend is not null) && helpers.Count != 2)
        {
            return null;
        }

        if (mapSibling is not null && mapOrdinals is { } mapOrdinal && function.Clauses is [var mapClause]
            && PlanMapComprehensionListHelper(
                function,
                mapClause,
                mapSibling,
                location,
                mapOrdinal,
                localFunctions
            ) is { } mapHelper && !declared.Contains((mapHelper.Helper.Name, mapHelper.Helper.Arity)))
        {
            helpers.Add(mapSibling, [(mapHelper.Helper, mapHelper.Location)]);
            replacements.Add(mapSibling, mapHelper.Call);
            definitions = OrderedLocalDefinitions(function.Clauses, ListComprehensionOrder.Definition).ToArray();
        }

        foreach (var (comprehension, closure) in staticMappers)
        {
            var group = helpers[comprehension];
            var (helper, helperLocation) = group[0];
            group[0] = (helper with { StaticListClosureTarget = DeferListClosureTarget(closure) }, helperLocation);
        }

        var emittedDefinitions = definitions.Where(definition =>
            definition is not (Expr.ListComprehension or Expr.MapComprehension) || helpers.ContainsKey(definition)).ToArray();
        var ordered = emittedDefinitions.Where(helpers.ContainsKey).SelectMany(definition => helpers[definition]).ToArray();
        recursiveDefinitionOrder.Add(
            (function.Name, function.Arity),
            emittedDefinitions.SelectMany(DefinitionOrder).ToArray()
        );
        hasMixedRecursiveDefinitions |= anonymous.Length != 0;
        foreach (var (closure, ordinal) in failureOrdinals)
        {
            recursiveClosureFailureOrdinals.Add((function.Name, function.Arity, names[closure]), ordinal);
        }
        foreach (var helper in ordered)
        {
            declared.Add((helper.Function.Name, helper.Function.Arity));
        }

        var parent = function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression => expression,
                    rewriteScope: expression => replacements.GetValueOrDefault(expression)
                )
            }).ToArray()
        };

        return (parent, ordered);

        IEnumerable<Expr> WithNestedList(IEnumerable<Expr> definitionsToOrder)
        {
            foreach (Expr definition in definitionsToOrder)
            {
                // Core traverses the callable body before assigning the fun
                // identity. The lifted helper remains under the enclosing MFA.
                if (nestedClosureMapping is { } nested && ReferenceEquals(definition, nested.Closure))
                {
                    yield return comprehensions[0];
                }

                yield return definition;
            }
        }

        IEnumerable<(string Name, int? Arity)> DefinitionOrder(Expr definition) =>
            helpers.TryGetValue(definition, out var group)
                ? group.Select(helper => (helper.Function.Name, (int?)helper.Function.Arity))
                : [(AnonymousClosureName(function, names[definition]), null)];

        int DefinitionWidth(Expr definition) => nestedSiblings && definition is Expr.ListComprehension
        {
            Qualifiers: [ComprehensionQualifier.Generator, ComprehensionQualifier.Generator]
        } ? 2 : 1;
    }

    private (Expr.Fun Closure, Clause Clause)? ReadDirectClosureListMapping(
        FunctionDefinition function,
        IReadOnlyList<Expr.Fun> closures,
        Expr.ListComprehension comprehension,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || UsesSourceInlining || automaticCoreInlining
            || function.Clauses is not
            [
            {
                Guard: null,
                Body: Expr.Call { Module: null, DynamicRemote: false } parentCall
            } entry]
            || !HasUnconditionalParameters(entry)
            || !localFunctions.ContainsKey((parentCall.Function, parentCall.Arguments.Count))
            || parentCall.Arguments.Any(argument => argument is not (Expr.Variable or Expr.Literal or Expr.Fun))
            || closures.Any(candidate => !IsTotalAnonymousClosure(candidate)
                || !parentCall.Arguments.Any(argument => ReferenceEquals(argument, candidate)))
            || comprehension is not
            {
                Items: [Expr.Call { Module: null, DynamicRemote: false } mapped],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false,
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } item,
                    Source: Expr.Variable source
                }]
            }
            || !localFunctions.ContainsKey((mapped.Function, mapped.Arguments.Count)))
        {
            return null;
        }

        var closure = closures.SingleOrDefault(candidate => ReferenceEquals(candidate.Clauses[0].Body, comprehension));
        if (closure is null)
        {
            return null;
        }

        Clause clause = closure.Clauses[0];
        var visible = new HashSet<string>(
            entry.Patterns.Concat(clause.Patterns).OfType<Pattern.Variable>().Select(parameter => parameter.Name),
            StringComparer.Ordinal
        );
        var operands = LocalFunctionEntries.Walk(mapped).ToArray();
        var free = operands.OfType<Expr.Variable>().Select(variable => variable.Name)
            .Where(name => name != item.Name).ToHashSet(StringComparer.Ordinal);
        if (!visible.Contains(source.Name) || free.Count > 1 || !free.IsSubsetOf(visible)
            || operands.Any(operand => operand is not (Expr.Call { Module: null, DynamicRemote: false }
                or Expr.Variable or Expr.Literal)))
        {
            return null;
        }

        return (closure, clause);
    }

    private static bool HasSequentialNestedListGenerators(
        FunctionDefinition function,
        IReadOnlyList<Expr.ListComprehension> comprehensions
    )
    {
        if (comprehensions.Count < 2 || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
            || !HasUnconditionalParameters(clause))
        {
            return false;
        }

        var bindings = sequence.Items.OfType<Expr.Match>()
            .Where(match => match.Pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard })
            .Select(match => match.Value is Expr.Call { DynamicRemote: false, Arguments: [var only] } ? only : match.Value)
            .OfType<Expr.ListComprehension>().ToArray();
        if (bindings.Length != comprehensions.Count || !bindings.SequenceEqual(comprehensions, ReferenceEqualityComparer.Instance))
        {
            return false;
        }

        return bindings.Any(comprehension => comprehension.Qualifiers is
            [ComprehensionQualifier.Generator, ComprehensionQualifier.Generator])
            && bindings.All(comprehension => comprehension.Qualifiers is
                [ComprehensionQualifier.Generator { Strict: false, Source: Expr.Variable or Expr.Call { DynamicRemote: false } }, ..]
                && (comprehension.Qualifiers.Skip(1).All(qualifier => qualifier is ComprehensionQualifier.Filter)
                    || comprehension.Qualifiers is
                        [ComprehensionQualifier.Generator,
                         ComprehensionQualifier.Generator { Strict: false, Source: Expr.Variable or Expr.Call { DynamicRemote: false } }]));
    }

    private static (Expr.Binary Append, Expr.ListComprehension Left, Expr.ListComprehension Right)? ReadPairedListAppend(
        IReadOnlyList<Expr> expressions,
        Clause clause
    )
    {
        if (expressions.Any(expression => expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe
            or Expr.Case or Expr.If or Expr.BinaryComprehension or Expr.MapComprehension))
        {
            return null;
        }

        var parameters = clause.Patterns.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
        foreach (Expr.Binary append in expressions.OfType<Expr.Binary>())
        {
            if (append is
                {
                    Operator: ErlangOperators.Append,
                    Left: Expr.ListComprehension left,
                    Right: Expr.ListComprehension right
                } && IsInputList(left) && IsInputList(right))
            {
                return (append, left, right);
            }
        }

        return null;

        bool IsInputList(Expr.ListComprehension comprehension) => comprehension.Qualifiers is
            [
                ComprehensionQualifier.Generator
            {
                Strict: false, Pattern: Pattern.Variable, Source: Expr.Variable source
            }
            ] && parameters.Contains(source.Name);
    }

    private static Expr.Binary? ReadBoundComprehensionAppend(Clause clause, Expr.ListComprehension comprehension)
    {
        var expressions = LocalFunctionEntries.Walk(clause.Body).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.Case or Expr.If
            or Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe))
        {
            return null;
        }

        // v3_core evaluates the append tail before the generator. An incoming variable is already a value.
        return expressions.OfType<Expr.Binary>().FirstOrDefault(expression =>
            expression.Operator == ErlangOperators.Append && ReferenceEquals(expression.Left, comprehension)
            && expression.Right is Expr.Variable tail
            && clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == tail.Name)
            && comprehension.Qualifiers is [ComprehensionQualifier.Generator generator, ..]
            && !Semantics.Variables(generator.Pattern).Contains(tail.Name, StringComparer.Ordinal));
    }

    private static string AnonymousClosureName(FunctionDefinition owner, int ordinal) =>
        FormattableString.Invariant($"-{owner.Name}/{owner.Arity}-fun-{ordinal}-");

    private void RecordRecursiveClosureBoundaries(
        IReadOnlyList<FunctionDefinition> functions,
        int[] firstClosure,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        Dictionary<int, List<int>> boundaries
    )
    {
        for (int index = 0; index < firstClosure.Length - 1; index++)
        {
            var parent = functions[index];
            if (!recursiveDefinitionOrder.TryGetValue((parent.Name, parent.Arity), out var order))
            {
                continue;
            }

            int boundary = firstClosure[index];
            int end = firstClosure[index + 1];
            foreach (var definition in order)
            {
                if (definition.Arity is not { } arity)
                {
                    int emitted = closures.FindIndex(boundary, end - boundary, closure => closure.Name == definition.Name);
                    if (emitted >= 0)
                    {
                        boundary = emitted + 1;
                    }

                    continue;
                }

                if (!boundaries.TryGetValue(boundary, out var group))
                {
                    group = [];
                    boundaries.Add(boundary, group);
                }

                group.Add(-entries[(definition.Name, arity)]);
            }
        }
    }

    private static bool HasSeparatedTupleOwnerDefinitions(IReadOnlyList<Clause> clauses)
    {
        bool hasLists = false;
        foreach (Clause clause in clauses)
        {
            if (clause.Patterns is not
                [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom } or Pattern.Variable, _] },
                    ..
                ] || clause.Patterns.Skip(1).Any(pattern => pattern is not Pattern.Variable))
            {
                return false;
            }

            Expr[] definitions = LocalFunctionEntries.Walk(clause.Body)
                .Where(expression => expression is Expr.Fun or Expr.ListComprehension).ToArray();
            if (definitions is [] or [Expr.Fun { SelfName: null }])
            {
                continue;
            }

            if (definitions.Any(definition => definition is not Expr.ListComprehension
                {
                    Qualifiers: [ComprehensionQualifier.Generator { Strict: false }, ..] qualifiers
                } || qualifiers.Skip(1).Any(qualifier => qualifier is not ComprehensionQualifier.Filter)))
            {
                return false;
            }

            hasLists = true;
        }

        return hasLists;
    }

    private static bool IsOwnedTupleAliasFilter(Pattern pattern, Expr value, Expr? filter)
    {
        if (filter is null || pattern is not AliasPattern alias || value is not Expr.Variable returned)
        {
            return false;
        }

        return IsAlias(alias.Left, alias.Right) || IsAlias(alias.Right, alias.Left);

        bool IsAlias(Pattern binding, Pattern shape) => binding is Pattern.Variable whole
            && whole.Name == returned.Name && whole.Name != VariableScopeNames.Wildcard
            && shape is Pattern.Tuple tuple
            && tuple.Items.All(field => field is Pattern.Variable or Pattern.Literal { Value: Atom });
    }

    private (Expr.Call Call, FunctionDefinition Helper, BeamSourceLocation Location)? PlanRecursiveListComprehension(
        Expr.ListComprehension comprehension,
        Clause parentClause,
        BeamSourceLocation location,
        string helperName,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        bool independent,
        bool discarded,
        bool allowStrict,
        Expr.Variable? appendTail,
        bool allowGuardedAliasSibling = false
    )
    {
        bool guardedElement = !independent && !discarded && SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
            && IsClosedGuardedComprehension(parentClause, comprehension);
        bool simpleElement = !independent && !discarded && SsaTypeInferenceEnabled
            && comprehension is
            {
                Items: [var body],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Variable variable, Source: Expr.Variable
                }]
            }
            && (ReadConditionalListElement(body, variable.Name) is not null
                || ReadFlatTupleListElement(body, variable.Name) is not null);

        // These value expressions share the normal recursive lowering and the flat operand proof below.
        bool flatElement = !independent && !discarded
            && comprehension is
            {
                Items: [Expr.Variable or Expr.Literal or Expr.Tuple or Expr.List or Expr.Unary
                    or Expr.Binary or Expr.RecordConstruction { Wildcard: null }
                    or Expr.RecordUpdate { Value: Expr.Variable }],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Variable, Source: Expr.Variable
                }]
            }
            && !LocalFunctionEntries.Walk(parentClause.Body).Any(expression =>
                expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe
                || appendTail is null && expression is Expr.Binary { Operator: ErlangOperators.Append, Left: var left }
                    && ReferenceEquals(left, comprehension));

        bool sequentialElement = !discarded && comprehension.Items is [Expr.Block or Expr.Sequence or Expr.Match]
            && comprehension.Qualifiers is [ComprehensionQualifier.Generator sequential, ..]
            && (!sequential.Strict && sequential.Pattern is Pattern.Variable
                || sequential.Strict && sequential.Pattern is AliasPattern sequentialAlias
                    && IsRecursiveTupleGeneratorAlias(sequentialAlias));

        bool caseElement = !discarded && !guardedElement && comprehension.Items is [var element]
            && comprehension.Qualifiers is [ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable }, ..]
            && LocalFunctionEntries.Walk(element).Any(expression => expression is Expr.Case);
        bool lexicalElement = sequentialElement || caseElement;

        // The checked field read stays in the parent and is evaluated once.
        bool recordFieldSource = SsaTypeInferenceEnabled && !discarded && comprehension.Qualifiers is
        [
            ComprehensionQualifier.Generator
        {
            Strict: false, Pattern: Pattern.Variable, Source: Expr.RecordAccess { Value: Expr.Variable }
        }, ..
        ];

        if (comprehension is not
            {
                Items: [var mapped], Qualifiers: [ComprehensionQualifier.Generator
                { Pattern: var item, Source: var source } generator, ..]
            }
            || !TryReadRecursiveListFilter(comprehension.Qualifiers, out Expr? filter)
            || generator.Strict && (!allowStrict || discarded
                || comprehension.Qualifiers.Count != 1 && !CanLiftStrictListComparison(item, source, filter)
                || item is not (Pattern.Variable or Pattern.Tuple)
                    && !(item is AliasPattern strictAlias && IsRecursiveTupleGeneratorAlias(strictAlias))
                || LocalFunctionEntries.Walk(parentClause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Receive or Expr.Maybe
                    or Expr.BinaryComprehension or Expr.MapComprehension))
            || source is not (Expr.Variable or Expr.Call { DynamicRemote: false })
                && !(discarded && IsComputedDiscardedListSource(source))
                && !recordFieldSource
                && !(source is Expr.List { Items.Count: > 0 } && !generator.Strict && !independent && !discarded
                    && comprehension.Qualifiers.Count == 1
                    && !LocalFunctionEntries.Walk(parentClause.Body).Any(expression =>
                        expression is Expr.Try or Expr.Receive or Expr.Maybe))
                && !(source is Expr.RecordAccess { Value: Expr.Variable } && SsaTypeInferenceEnabled && !discarded
                    && (comprehension.Qualifiers.Count == 1 && ReadTupleListProjection(item, mapped) is not null
                        || !generator.Strict && item is Pattern.Tuple && filter is not null
                            && ComprehensionGuard.IsTest(filter, sourceModule)))
            || !(item is Pattern.Variable && (generator.Strict || simpleElement || flatElement || guardedElement || independent || filter is not null
                    || recordFieldSource || lexicalElement || source is Expr.Call or Expr.List
                    || mapped is Expr.Call { DynamicRemote: false } or Expr.Apply { Function: Expr.Variable })
                || item is (Pattern.Tuple or Pattern.List) && IsRecursiveListPattern(item)
                || item is AliasPattern alias && SsaTypeInferenceEnabled && !discarded
                    && (comprehension.Qualifiers.Count == 1 && ReadTupleListProjection(alias, mapped) is not null
                        || (!independent || generator.Strict
                            || allowGuardedAliasSibling && IsOwnedTupleAliasFilter(alias, mapped, filter))
                            && IsRecursiveTupleGeneratorAlias(alias)
                            && source is Expr.Variable or Expr.Call { DynamicRemote: false }
                            && (comprehension.Qualifiers.Count == 1
                                || comprehension.Qualifiers[^1] is ComprehensionQualifier.Filter acceptedGuard
                                    && ComprehensionGuard.IsTest(acceptedGuard.Expression, sourceModule))
                            && !LocalFunctionEntries.Walk(parentClause.Body).Any(expression =>
                                expression is Expr.Try or Expr.Receive or Expr.Maybe))))
        {
            return null;
        }

        if (source is Expr.RecordAccess && SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
            && !generator.Strict && filter is not null && ComprehensionGuard.IsTest(filter, sourceModule)
            && item is Pattern.Tuple pattern && mapped is Expr.Tuple construction
            && pattern.Items.Count == construction.Items.Count
            && pattern.Items.Zip(construction.Items).All(pair => pair.First is Pattern.Variable field
                && field.Name != VariableScopeNames.Wildcard
                && pair.Second is Expr.Variable value && field.Name == value.Name))
        {
            // v3_core matches the source field once. Preserve the matched tuple
            // across the recursive call when the result reconstructs its fields.
            var matchedTuple = new Pattern.Variable("$list_generator_tuple");
            item = new AliasPattern(matchedTuple, item);
            mapped = new Expr.Variable(matchedTuple.Name);
        }

        var mappedOperands = LocalFunctionEntries.Walk(mapped).ToArray();
        Expr[] filterOperands = filter is null ? [] : LocalFunctionEntries.Walk(filter).ToArray();
        bool flatOutput = mappedOperands.All(CanUseMappedExpression) && filterOperands.All(CanUseExpression);
        // The source stays in the parent and runs once, before the helper can bind a generator pattern.
        if (!simpleElement && !flatOutput && !guardedElement || !LocalFunctionEntries.Walk(source).All(CanUseExpression))
        {
            return null;
        }

        var bound = Semantics.Variables(item).ToHashSet(StringComparer.Ordinal);
        Expr[] output = [.. mappedOperands, .. filterOperands];
        if (!ModuleOptimizationDisabled && flatOutput)
        {
            var constants = ReadDominatingHelperConstants(
                parentClause,
                comprehension,
                allowListComprehensionPrefix: discarded && IsComputedDiscardedListSource(source)
            );
            foreach (string name in bound)
            {
                constants.Remove(name);
            }

            foreach (Expr.Apply apply in output.OfType<Expr.Apply>())
            {
                if (apply.Function is Expr.Variable function)
                {
                    constants.Remove(function.Name);
                }
            }

            if (output.OfType<Expr.Variable>().Any(variable => constants.ContainsKey(variable.Name)))
            {
                mapped = SubstituteHelperOperandConstants(mapped, constants);
                filter = filter is null ? null : SubstituteHelperOperandConstants(filter, constants);
                output = LocalFunctionEntries.Walk(mapped)
                    .Concat(filter is null ? [] : LocalFunctionEntries.Walk(filter)).ToArray();
            }
        }

        HashSet<string> free;
        if (lexicalElement)
        {
            if (ClosureCaptures.ReadListComprehensionBindings(parentClause, comprehension) is not { } visible)
            {
                return null;
            }

            // A new match binding belongs to the element. A match against a
            // name already visible outside the generator needs that capture.
            visible.ExceptWith(bound);
            free = ClosureCaptures.ReadEntryReferences(new Clause([], filter, mapped), visible);
        }
        else
        {
            free = (guardedElement ? [] : output.OfType<Expr.Variable>())
                .Select(variable => variable.Name)
                .Where(name => !bound.Contains(name))
                .ToHashSet(StringComparer.Ordinal);
        }

        if (appendTail is not null)
        {
            free.Add(appendTail.Name);
        }

        // Core names incoming arguments before variables defined in the clause body.
        var captures = parentClause.Patterns.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name)
            .Where(free.Remove)
            .Concat(free.Order(StringComparer.Ordinal))
            .ToArray();
        if (captures.Length + 1 >= FirstReservedXRegister)
        {
            return null;
        }

        int line = generator.GeneratorLine != 0 ? generator.GeneratorLine
            : comprehension.PreprocessorLine != 0 ? comprehension.PreprocessorLine
            : comprehension.Source?.Line ?? location.Line;
        var helperLocation = new BeamSourceLocation(generator.GeneratorFile ?? comprehension.PreprocessorFile ?? location.File, line);
        var recursiveCall = new Expr.Call(null, helperName, [source, .. captures.Select(name => new Expr.Variable(name))])
        {
            SourceLine = line,
            PreprocessorFile = helperLocation.File
        };
        BeamSourceLocation? invalidSource = source switch
        {
            _ when generator.InputLine != 0 => new(generator.InputFile ?? helperLocation.File, generator.InputLine),
            Expr.Call producer when generator.Strict => CallLocation(producer, helperLocation),
            _ when generator.Strict => helperLocation,
            _ => null
        };
        var projection = ReadTupleListProjection(item, mapped);
        var optionCall = !discarded ? ReadTupleOptionGenerator(item, mapped) : null;
        bool hasProvenTupleInput = SsaTypeInferenceEnabled && !ModuleOptimizationDisabled && !generator.Strict
            && filter is null && (projection is not null || optionCall is not null)
            && HasProvenTupleGenerator(
                source,
                parentClause,
                comprehension,
                localFunctions,
                projection?.Arity ?? 2,
                projection?.Tag
            );
        bool hasProvenTupleFilterInput = SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
            && !generator.Strict && !discarded && appendTail is null && captures.Length == 0
            && item is Pattern.Variable filteredItem && mapped is Expr.Variable retained && retained.Name == filteredItem.Name
            && filter is not null && ComprehensionGuard.IsTest(filter, sourceModule)
            && ReadTupleFieldExclusions(filter, filteredItem.Name) is { } exclusion
            && HasMinimumTupleListInput(
                source,
                parentClause,
                comprehension,
                localFunctions,
                exclusion.Position
            );
        bool properSource = hasProvenTupleInput || hasProvenTupleFilterInput || IsProperListGeneratorCall(source)
            || SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
            && (source is Expr.List { Tail: null or Expr.Literal { Value: Nil } }
                || source is Expr.Variable provenList && HasProperListBinding(parentClause, comprehension, provenList.Name));
        var helper = BuildRecursiveMapping(
            helperName,
            item,
            mapped,
            captures.Select(name => new Pattern.Variable(name)).ToArray(),
            properInput: properSource,
            filter: filter is null ? null : (filter, ComprehensionGuard.IsTest(filter, sourceModule)),
            discardResult: discarded,
            strictGeneratorLocation: generator.Strict ? helperLocation : null,
            invalidSourceLocation: invalidSource,
            appendTail: appendTail
        ) with
        {
            IsRecursiveListMapper = true,
            // Here the result is constructed from a nil base, a recursive call, and optional cons cells.
            HasProperListResult = !discarded && !properSource && appendTail is null,
            HasProvenRecursiveTupleInput = hasProvenTupleInput,
            HasProvenRecursiveTupleFilterInput = hasProvenTupleFilterInput,
            IsStrictRecursiveListMapper = generator.Strict
        };

        return (recursiveCall, helper, helperLocation);

        // Tuple records use the same field expansion in the parent and in a helper.
        // Keep wildcard initializers and unevaluated defaults in the existing path.
        bool CanUseMappedExpression(Expr expression) => CanUseExpression(expression)
            || lexicalElement && (expression is Expr.Block or Expr.Sequence
                || expression is Expr.Match match && IsRecursiveListPattern(match.Pattern))
            || caseElement && expression is Expr.Case choice
                && choice.Clauses.All(branch => branch.Patterns.All(IsRecursiveListPattern)
                    && (branch.Guard is null || ComprehensionGuard.IsTest(branch.Guard, sourceModule)))
            || discarded && expression is Expr.Call { DynamicRemote: true }
            || !discarded && expression is Expr.RecordUpdate { Value: Expr.Variable }
            || expression is Expr.RecordConstruction { Wildcard: null } record
                && record.Definition.Fields.All(field => record.Fields.Any(value => value.Name == field.Name)
                    || AttributeLiteral.TryNormalizeConstant(field.Value, out _));

        bool CanUseExpression(Expr expression) => CanUseRecursiveListOperand(expression, localFunctions);
    }

    private bool CanUseRecursiveListOperand(
        Expr expression,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    ) => expression switch
    {
        Expr.Call { DynamicRemote: false } call => call.Module is not null
            || localFunctions.ContainsKey((call.Function, call.Arguments.Count))
            || ResolveExternalModule(call.Function, call.Arguments.Count) is not null,
        Expr.Apply { Function: Expr.Variable } => true,
        Expr.Variable or Expr.Literal or Expr.Tuple or Expr.List or Expr.Unary or Expr.Binary
            or Expr.RecordAccess or Expr.RecordTest => true,
        _ => false
    };

    private static bool HasProvenTupleGenerator(
        Expr source,
        Clause parent,
        Expr.ListComprehension comprehension,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        int arity,
        Atom? tag
    )
    {
        if (HasSortedMappedTupleInput(
            source,
            parent,
            comprehension,
            arity,
            tag
        ))
        {
            return true;
        }

        if (source is Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 0 } call)
        {
            if (!localFunctions.TryGetValue((call.Function, 0), out var definition)
                || definition.Clauses is not [{ Guard: null, Patterns: [], Body: var literalBody }])
            {
                return false;
            }

            source = literalBody;
        }

        bool nonempty = false;
        while (source is Expr.List list)
        {
            foreach (Expr element in list.Items)
            {
                if (element is not Expr.Tuple tuple || tuple.Items.Count != arity
                    || tag is not null && (tuple.Items[0] is not Expr.Literal literal || !tag.Equals(literal.Value)))
                {
                    return false;
                }

                nonempty = true;
            }

            // Evaluating a tuple field can call user code or raise. The producer
            // still evaluates those expressions before this helper receives a list.
            source = list.Tail ?? new Expr.Literal(Nil.Value);
        }

        var constants = source is Expr.Variable ? ReadDominatingHelperConstants(parent, comprehension) : null;
        if (!AttributeLiteral.TryNormalizeConstant(source, out Term result, constants))
        {
            return false;
        }

        while (result is Cons cell)
        {
            if (cell.Head is not TupleTerm tuple || tuple.Items.Count != arity
                || tag is not null && !tag.Equals(tuple.Items[0]))
            {
                return false;
            }

            nonempty = true;
            result = cell.Tail;
        }

        return nonempty && result is Nil;
    }

    private static bool CanLiftStrictListComparison(Pattern item, Expr source, Expr? filter)
    {
        return source is Expr.Variable && item is Pattern.Tuple tuple && tuple.Items.All(field => field is Pattern.Variable)
            && filter is Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                    or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual
                    or ErlangOperators.Less or ErlangOperators.LessOrEqual
                    or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual,
                Left: Expr.Variable or Expr.Literal,
                Right: Expr.Variable or Expr.Literal
            };
    }

    private bool TryReadRecursiveListFilter(
        IReadOnlyList<ComprehensionQualifier> qualifiers,
        out Expr? filter
    )
    {
        filter = null;
        if (qualifiers.Count == 1)
        {
            return true;
        }

        if (qualifiers is [_, ComprehensionQualifier.Filter single])
        {
            filter = single.Expression;

            return true;
        }

        var guards = qualifiers.Skip(1).OfType<ComprehensionQualifier.Filter>().ToArray();
        if (guards.Length != qualifiers.Count - 1
            || guards.Any(guard => !ComprehensionGuard.IsTest(guard.Expression, sourceModule)))
        {
            return false;
        }

        // The contiguous guard tests share one protected generator clause. Keep ordinary
        // filter expressions on their existing path, which preserves their bad_filter errors.
        filter = guards[^1].Expression;
        for (int index = guards.Length - 2; index >= 0; index--)
        {
            filter = new Expr.Binary(ErlangOperators.AndAlso, guards[index].Expression, filter);
        }

        return true;
    }

    private static bool IsDiscardedListEffect(Expr.ListComprehension comprehension)
    {
        if (comprehension is not
            {
                Items: [var effect],
                Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: var pattern, Source: var source }]
            })
        {
            return false;
        }

        if (source is not Expr.Variable && !IsComputedDiscardedListSource(source))
        {
            return false;
        }

        if (effect is Expr.Call { DynamicRemote: false } or Expr.Apply { Function: Expr.Variable })
        {
            return pattern is Pattern.Variable || pattern is Pattern.Tuple && IsRecursiveListPattern(pattern);
        }

        // The result of a checked dynamic call is unused, but the call, short circuit
        // and relaxed pattern skip must still run in the recursive helper.
        return source is Expr.Variable && pattern is Pattern.Tuple && IsRecursiveListPattern(pattern) && effect is Expr.Binary
        {
            Operator: ErlangOperators.OrElse,
            Left: Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable receiver,
                Right: Expr.Literal { Value: Atom }
            },
            Right: Expr.Call
            {
                DynamicRemote: true, Arguments: [Expr.Variable receiverCall, Expr.Literal { Value: Atom }, Expr.List { Tail: null }]
            }
        } && receiver.Name == receiverCall.Name;
    }

    private static bool IsComputedDiscardedListSource(Expr source) =>
        source is Expr.Call { DynamicRemote: false }
            or Expr.RecordAccess { Value: Expr.Variable }
            or Expr.List { Items.Count: >= 2 };

    private static bool IsProperListGeneratorCall(Expr expression) => expression is Expr.Call
    {
        DynamicRemote: false,
        Arguments.Count: 1
    } call && (call is { Module: "lists", Function: "reverse" or "sort" }
        || call is { Module: "maps", Function: "keys" or "values" or "to_list" });

    private static bool IsRecursiveTupleGeneratorAlias(AliasPattern alias) => alias switch
    {
        { Left: Pattern.Variable { Name: not VariableScopeNames.Wildcard }, Right: Pattern.Tuple tuple } =>
            IsRecursiveListPattern(tuple),
        { Left: Pattern.Tuple tuple, Right: Pattern.Variable { Name: not VariableScopeNames.Wildcard } } =>
            IsRecursiveListPattern(tuple),
        _ => false
    };

    private static bool IsRecursiveListPattern(Pattern pattern) => pattern switch
    {
        Pattern.Variable or Pattern.Literal => true,
        Pattern.Tuple tuple => tuple.Items.All(IsRecursiveListPattern),
        Pattern.List list => list.Items.All(IsRecursiveListPattern) && (list.Tail is null || IsRecursiveListPattern(list.Tail)),
        _ => false
    };

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitIdentityListComprehension(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.Clauses[0] is not
                {
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [Expr.Variable value] }
                } match
                || item.Name != value.Name || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            var registerGuard = match with { Patterns = [new Pattern.Variable(VariableScopeNames.Wildcard), item] };
            if (!CanEmitSeparateEntryGuard(registerGuard, match.Guard))
            {
                return false;
            }

            int skip = match.Guard is null ? 0 : Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            if (match.Guard is null)
            {
                EmitFrameAllocation(1, 1);
                owner.EmitListParts(
                    0,
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                EmitEntryGuardPrefix(registerGuard, match.Guard, skip);
                EmitFrameAllocation(2, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }

            owner.EmitRecursiveCons(entry, 1);
            if (skip != 0)
            {
                Mark(skip);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, entry);
            }

            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }

        private bool TryEmitRecursiveListCallMapping(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || !function.IsRecursiveListMapper
                || function.IsStrictRecursiveListMapper || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [Expr.Binary { Operator: ErlangOperators.Append } append] }
                }
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            Term[] prefix = ReadFoldedListMapperPrefix(append.Left);
            Expr mapped = prefix.Length == 0 ? append : append.Right;
            var calls = new List<ListMapperCall>();
            if (!AppendListMapperCalls(
                mapped,
                item.Name,
                calls,
                allowAppend: true
            ) || calls.Any(stage => stage.Call.Module is null
                && !entries.ContainsKey((stage.Call.Function, stage.Arguments.Length))
                && owner.ResolveExternalModule(stage.Call.Function, stage.Arguments.Length) is null))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            if (prefix.Length == 0)
            {
                EmitFrameAllocation(1, 1);
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
                var current = EmitListMapperCalls(calls, location);
                owner.EmitMappingResult(entry, current == location ? null : location);
            }
            else
            {
                EmitFoldedListMapperPrefix(prefix, calls, entry);
            }

            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }

        private static Term[] ReadFoldedListMapperPrefix(Expr left)
        {
            if (!AttributeLiteral.TryNormalizeConstant(left, out Term value)
                || AppendPrefix(new Expr.Literal(value)) is not { Count: > 0 } items)
            {
                return [];
            }

            return items.Cast<Expr.Literal>().Select(item => item.Value).ToArray();
        }

        private void EmitFoldedListMapperPrefix(Term[] prefix, List<ListMapperCall> calls, int entry)
        {
            EmitFrameAllocation(1, 2);
            EmitFrameInitialization(0, 1);
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );

            var current = EmitListMapperCalls(calls, location);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * prefix.Length);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = prefix.Length - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(prefix[index]);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(index == 0 ? YRegisterTag : XRegisterTag, 0);
            }

            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (current != location)
            {
                owner.EmitLine(location);
            }

            owner.EmitRecursiveCons(entry, 1);
        }

        private bool IsRecursiveListGeneratorFailure(Expr.Call call) =>
            call.IsListGeneratorSourceFailure || parentFunction is { IsRecursiveListMapper: true } mapper
            && mapper.Clauses[^1].Body is Expr.Call failure
            && ReferenceEquals(failure.Arguments, call.Arguments);

        private void EmitRecursiveListMappingEnd(FunctionDefinition function, int empty)
        {
            int baseResult = function.IsRecursiveListMapper ? RecursiveListBaseResultRegister(function) : 0;
            if (function.IsRecursiveListMapper && function.Clauses[^1].Body is Expr.Variable)
            {
                Mark(empty);
                if (baseResult != 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        baseResult,
                        XRegisterTag,
                        0
                    );
                }
                owner.instructions.WriteByte(Return);
            }
            else
            {
                BeamSourceLocation? invalid = function.IsRecursiveListMapper
                    && function.Clauses[^1].Body is Expr.Call { SourceLine: not 0 } badGenerator
                    ? CallLocation(badGenerator, location) : null;
                owner.EmitMappingEnd(
                    empty,
                    Label(),
                    invalid,
                    baseResult
                );
            }
        }

        private void EmitRecursiveListMappingRejection(
            FunctionDefinition function,
            int entry,
            int rejectedRegister
        )
        {
            if (function.IsStrictRecursiveListMapper)
            {
                owner.EmitListMappingError(rejectedRegister + 1, rejectedRegister, ErlangErrorReasons.BadMatch);

                return;
            }

            owner.Op(TailLocalCall, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, entry);
        }

        private bool TryEmitImmediateListMapping(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.IsStrictRecursiveListMapper
                || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable item],
                        Tail: Pattern.Variable tail
                    }, ..],
                    Body: Expr.List
                    {
                        Items: [var value],
                        Tail: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, ..]
                        } recursive
                    }
                } branch
                || recursive.Function != function.Name || recursive.Arguments.Count != function.Arity
                || input.Name != tail.Name || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            Term? literal = null;
            bool captured = branch.Patterns is [_, Pattern.Variable capture]
                && value is Expr.Variable variable && variable.Name == capture.Name && variable.Name != item.Name
                && recursive.Arguments[1] is Expr.Variable retained && retained.Name == capture.Name;
            if (!captured && (function.Arity != 1 || !AttributeLiteral.TryNormalizeConstant(value, out literal)))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(function.Arity, captured ? 1 : 0);
            if (captured)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }

            // A variable or wildcard generator pattern never rejects the head.
            // When the result is independent of the head, only copy its tail.
            owner.Op(GetTail, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            if (captured)
            {
                owner.EmitRecursiveCons(entry, function.Arity);
            }
            else
            {
                owner.EmitFieldCall(function.Arity, entry);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(literal!);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(0);
            }

            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }

        private bool TryEmitStaticListClosureMapping(FunctionDefinition function)
        {
            if (function.StaticListClosureTarget is not int target
                || !entries.TryGetValue((function.Name, 2), out int recurse))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitFieldCall(1, target);
            Load(0, 2);
            Load(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitRecursiveCons(recurse, 2);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }

        private bool TryEmitCapturedListMapping(FunctionDefinition function)
        {
            if (TryEmitStaticListClosureMapping(function) || TryEmitImmediateListMapping(function))
            {
                return true;
            }

            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable { Name: "@BeamComprehensionTail" } },
                        Pattern.Variable capture],
                    Body: Expr.List
                    {
                        Items: [Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable environment, Expr.Variable element]
                        } mapped]
                    }
                }, ..]
                || environment.Name != capture.Name || element.Name != item.Name
                || !entries.TryGetValue((mapped.Function, 2), out int mapper)
                || !entries.TryGetValue((function.Name, 2), out int recurse))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            Load(1, 0);
            owner.EmitCallLine(mapped, location);
            owner.EmitFieldCall(2, mapper);

            Load(0, 2);
            Load(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            if (CallLocation(mapped, location) != location)
            {
                owner.EmitLine(location);
            }
            owner.EmitRecursiveCons(recurse, 2);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
