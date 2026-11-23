namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int RecordGuardExpansionArityLimit = 100;

    private enum ComprehensionPatternKind
    {
        Variable,
        Constant,
        Cons,
        Tuple,
        Map,
        Atom,
        Float,
        Integer,
        Nil
    }

    private sealed record ComprehensionMatchPattern(
        ComprehensionPatternKind Kind,
        Term? Value,
        ComprehensionMatchPattern[] Fields,
        bool IsFunctionHead = false
    );

    private sealed record ComprehensionMatchClause(
        Clause Clause,
        ComprehensionMatchPattern[] Patterns,
        bool HasGuard
    );

    private (IReadOnlyList<Clause> Ssa, IReadOnlyList<Clause> Definition)? ReadConstructorColumnComprehensionOrder(
        IReadOnlyList<Clause> clauses
    )
    {
        if (clauses.Count < 2 || clauses.Any(clause => !HasReadFreePatterns(clause)
            || clause.Guard is not null && !HasListParameterGuard(clause)))
        {
            return null;
        }

        IReadOnlySet<string> emptyScope = new HashSet<string>(StringComparer.Ordinal);
        var ssa = OrderConstructorComprehensionClauses(
            clauses,
            ListComprehensionOrder.Ssa,
            emptyScope,
            expanded: false
        );
        var definitions = OrderConstructorComprehensionClauses(
            clauses,
            ListComprehensionOrder.Definition,
            emptyScope,
            expanded: false
        );

        return ssa.Count == clauses.Count && definitions.Count == clauses.Count ? (ssa, definitions) : null;
    }

    private bool HasListParameterGuard(Clause clause) => clause.Guard is Expr.Call
    {
        Module: null or ErlangModuleNames.Module,
        Function: AutoImportNames.IsList, Arguments: [Expr.Variable tested], DynamicRemote: false
    } call && ResolvesErlangBif(call)
        && clause.Patterns.OfType<Pattern.Variable>().Any(pattern => pattern.Name == tested.Name);

    private static bool HasReadFreePatterns(Clause clause)
    {
        var bindings = new HashSet<string>(StringComparer.Ordinal);

        return clause.Patterns.All(Visit);

        bool Visit(Pattern pattern) => pattern switch
        {
            Pattern.Variable variable => variable.Name == VariableScopeNames.Wildcard || bindings.Add(variable.Name),
            Pattern.Literal { Value: Atom or Nil } => true,
            Pattern.Tuple tuple => tuple.Items.All(Visit),
            MapPattern map => VisitMap(map),
            AliasPattern { Left: Pattern.Variable binding } alias => Visit(binding) && VisitAliasShape(alias.Right),
            AliasPattern { Right: Pattern.Variable binding } alias => Visit(binding) && VisitAliasShape(alias.Left),
            Pattern.List { Items.Count: 0, Tail: null } => true,
            Pattern.List { Items.Count: > 0 } list => list.Items.All(Visit) && (list.Tail is null || Visit(list.Tail)),
            _ => false
        };

        bool VisitMap(MapPattern map)
        {
            var keys = new HashSet<Atom>();

            return map.Fields.All(field => field.Key is Expr.Literal { Value: Atom key }
                && keys.Add(key) && Visit(field.Value));
        }

        bool VisitAliasShape(Pattern shape) => shape is MapPattern && Visit(shape);
    }

    private IReadOnlyList<Clause> OrderConstructorComprehensionClauses(
        IReadOnlyList<Clause> clauses,
        ListComprehensionOrder order,
        IReadOnlySet<string> outerBindings,
        bool expanded = true,
        bool functionHead = false
    )
    {
        if (clauses.Count < 2 || order == ListComprehensionOrder.Core)
        {
            return clauses;
        }

        var matrix = new List<ComprehensionMatchClause>();
        foreach (var clause in clauses)
        {
            var bindings = new HashSet<string>(outerBindings, StringComparer.Ordinal);
            if (clause.Guard is Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } })
            {
                continue;
            }

            bool hasGuard = clause.Guard is not (null or Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } });
            bool hasEquality = false;
            foreach (string name in clause.Patterns.SelectMany(Semantics.Variables))
            {
                hasEquality |= !bindings.Add(name);
            }

            int recordColumn = -1;
            Pattern? recordShape = null;
            // erl_expand_records turns the matching is_record guard into a tuple pattern.
            if (expanded && clause.Guard is { } guard
                && ReadRecordTest(guard) is { Value: Expr.Variable record, Name: var tag, Arity: var arity }
                && arity < RecordGuardExpansionArityLimit && !outerBindings.Contains(record.Name))
            {
                recordColumn = clause.Patterns.ToList().FindIndex(pattern =>
                    pattern is Pattern.Variable variable && variable.Name == record.Name);
                if (recordColumn >= 0 && clause.Patterns.SelectMany(Semantics.Variables).Count(name => name == record.Name) == 1)
                {
                    recordShape = new Pattern.Tuple([
                        new Pattern.Literal(tag),
                        .. Enumerable.Repeat(new Pattern.Variable(VariableScopeNames.Wildcard), arity - 1)
                    ]);
                    hasGuard = false;
                }
            }

            var patterns = new ComprehensionMatchPattern[clause.Patterns.Count];
            for (int column = 0; column < patterns.Length; column++)
            {
                Pattern pattern = column == recordColumn && recordShape is not null ? recordShape : clause.Patterns[column];
                if (ReadComprehensionMatchPattern(pattern, expanded) is not { } shape)
                {
                    return [];
                }

                patterns[column] = functionHead ? shape with { IsFunctionHead = true } : shape;
            }

            matrix.Add(new ComprehensionMatchClause(clause, patterns, hasGuard || hasEquality));
        }

        List<Clause> result = [];

        return matrix.Count != 0 && OrderComprehensionMatches(
            matrix,
            order,
            result,
            expanded
        ) ? result : [];
    }

    private static bool CanExpandComprehensionHead(IReadOnlyList<Clause> clauses)
    {
        foreach (var clause in clauses)
        {
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            var consHeads = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pattern in clause.Patterns)
            {
                string[] names = Semantics.Variables(pattern).ToArray();
                string? consHead = pattern is Pattern.List { Items: [Pattern.Variable head, ..] }
                    && names.Count(name => name == head.Name) == 1 ? head.Name : null;
                foreach (string name in names)
                {
                    // v3_core puts equality between two cons heads in the guard after both destructures.
                    if (!bindings.Add(name) && (name != consHead || !consHeads.Contains(name)))
                    {
                        return false;
                    }
                }

                if (consHead is not null)
                {
                    consHeads.Add(consHead);
                }
            }
        }

        return true;
    }

    private static bool OrderComprehensionMatches(
        IReadOnlyList<ComprehensionMatchClause> clauses,
        ListComprehensionOrder order,
        List<Clause> result,
        bool expanded
    )
    {
        if (clauses.Count == 1)
        {
            result.Add(clauses[0].Clause);

            return true;
        }

        if (clauses[0].Patterns.Length == 0)
        {
            var guarded = clauses.TakeWhile(clause => clause.HasGuard);
            var leaves = order == ListComprehensionOrder.Ssa ? guarded : guarded.Reverse();
            result.AddRange(leaves.Select(clause => clause.Clause));
            if (clauses.SkipWhile(clause => clause.HasGuard).FirstOrDefault() is { } otherwise)
            {
                result.Add(otherwise.Clause);
            }

            return true;
        }

        // match/4 partitions adjacent variable and constructor clauses, then visits from the end.
        var partitions = new List<List<ComprehensionMatchClause>>();
        foreach (var clause in clauses)
        {
            if (partitions.Count == 0
                || IsVariableComprehensionColumn(partitions[^1][0]) != IsVariableComprehensionColumn(clause))
            {
                partitions.Add([]);
            }

            partitions[^1].Add(clause);
        }

        if (!expanded && (partitions.Count > 2
            || partitions.Count == 2 && IsVariableComprehensionColumn(partitions[0][0])
                && !CanOrderDefinitionFreeLeadingClause(partitions[0])))
        {
            return false;
        }

        // Core drops a later partition when an unguarded row consumes every remaining column.
        if (expanded && partitions.SkipLast(1).Any(partition => partition.Any(clause =>
            !clause.HasGuard && clause.Patterns.All(pattern => pattern.Kind == ComprehensionPatternKind.Variable))))
        {
            return false;
        }

        IEnumerable<List<ComprehensionMatchClause>> traversal = partitions;
        if (order == ListComprehensionOrder.Ssa)
        {
            traversal = traversal.Reverse();
        }

        foreach (var partition in traversal)
        {
            bool ordered = IsVariableComprehensionColumn(partition[0])
                ? OrderComprehensionMatches(
                    partition.Select(DropComprehensionMatchColumn).ToArray(),
                    order,
                    result,
                    expanded
                )
                : OrderComprehensionConstructors(
                    partition,
                    order,
                    result,
                    expanded
                );
            if (!ordered)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanOrderDefinitionFreeLeadingClause(IReadOnlyList<ComprehensionMatchClause> partition)
    {
        // match/4 visits the later partition first for SSA names. The earlier
        // arm can still distinguish a later column without owning a local definition.
        return partition is [var leading]
            && leading.Patterns.Skip(1).Any(pattern => pattern.Kind != ComprehensionPatternKind.Variable)
            && !LocalFunctionEntries.Walk(leading.Clause.Body).Any(expression =>
                expression is Expr.Fun or Expr.ListComprehension
                    or Expr.BinaryComprehension or Expr.MapComprehension);
    }

    private static bool OrderComprehensionConstructors(
        IReadOnlyList<ComprehensionMatchClause> clauses,
        ListComprehensionOrder order,
        List<Clause> result,
        bool expanded
    )
    {
        var counts = clauses.GroupBy(clause => clause.Patterns[0].Kind)
            .ToDictionary(group => group.Key, group => group.Count());
        if (counts.TryGetValue(ComprehensionPatternKind.Map, out int maps) && maps > 1)
        {
            return false;
        }

        // opt_single_valued only promotes a closed tuple when its type group has one row.
        // Keep the owner-head fallback for that promotion; nested case and tuple fields are independent.
        if (expanded && counts.TryGetValue(ComprehensionPatternKind.Tuple, out int tuples) && tuples == 1
            && clauses.Any(clause => clause.Patterns[0] is
            { Kind: ComprehensionPatternKind.Tuple, Value: not null, IsFunctionHead: true }))
        {
            return false;
        }

        var selected = clauses.Select(clause => ReadComprehensionConstructor(clause, counts)).ToArray();
        if (!expanded && selected.Any(entry => entry.Kind == ComprehensionPatternKind.Constant))
        {
            return false;
        }

        // select_types and match_value group disjoint shapes. Source order breaks ties between values.
        var types = selected.GroupBy(entry => entry.Kind).OrderBy(group => group.Key).ToArray();
        var orderedTypes = order == ListComprehensionOrder.Ssa ? types.AsEnumerable() : types.Reverse();
        foreach (var type in orderedTypes)
        {
            var values = type.GroupBy(entry => entry.Value).ToArray();
            var orderedValues = order == ListComprehensionOrder.Ssa ? values.AsEnumerable() : values.Reverse();
            foreach (var value in orderedValues)
            {
                if (!OrderComprehensionMatches(
                    value.Select(entry => entry.Clause).ToArray(),
                    order,
                    result,
                    expanded
                ))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static (ComprehensionPatternKind Kind, Term? Value, ComprehensionMatchClause Clause) ReadComprehensionConstructor(
        ComprehensionMatchClause clause,
        Dictionary<ComprehensionPatternKind, int> counts
    )
    {
        var pattern = clause.Patterns[0];
        var remainder = DropComprehensionMatchColumn(clause);
        // opt_single_valued moves a single closed complex literal ahead of the constructor tests.
        if (pattern.Kind is ComprehensionPatternKind.Cons or ComprehensionPatternKind.Tuple
            && counts[pattern.Kind] == 1 && pattern.Value is not null)
        {
            return (ComprehensionPatternKind.Constant, pattern.Value, remainder);
        }

        var key = pattern.Kind switch
        {
            ComprehensionPatternKind.Tuple => Term.I(pattern.Fields.Length),
            ComprehensionPatternKind.Cons => null,
            _ => pattern.Value
        };

        return (pattern.Kind, key, remainder with { Patterns = [.. pattern.Fields, .. remainder.Patterns] });
    }

    private static bool IsVariableComprehensionColumn(ComprehensionMatchClause clause) =>
        clause.Patterns[0].Kind == ComprehensionPatternKind.Variable;

    private static ComprehensionMatchClause DropComprehensionMatchColumn(ComprehensionMatchClause clause) =>
        clause with { Patterns = clause.Patterns[1..] };

    private static ComprehensionMatchPattern? ReadComprehensionMatchPattern(Pattern pattern, bool expanded)
    {
        return pattern switch
        {
            Pattern.Variable or Pattern.Any => new(ComprehensionPatternKind.Variable, null, []),
            Pattern.Literal { Value: Atom atom } => new(ComprehensionPatternKind.Atom, atom, []),
            Pattern.Literal { Value: FloatTerm floating } => new(ComprehensionPatternKind.Float, floating, []),
            Pattern.Literal { Value: Integer integer } => new(ComprehensionPatternKind.Integer, integer, []),
            Pattern.Literal { Value: Nil } or Pattern.List { Items.Count: 0, Tail: null } =>
                new(ComprehensionPatternKind.Nil, Nil.Value, []),
            Pattern.Literal { Value: Cons list } when expanded => ReadLiteralComprehensionConsPattern(list),
            MapPattern when !expanded => new(ComprehensionPatternKind.Map, null, []),
            AliasPattern { Left: Pattern.Variable, Right: var shape } => ReadComprehensionMatchPattern(shape, expanded),
            AliasPattern { Left: var shape, Right: Pattern.Variable } => ReadComprehensionMatchPattern(shape, expanded),
            Pattern.Tuple tuple => ReadComprehensionTuplePattern(tuple.Items, expanded),
            Pattern.List { Items.Count: > 0 } list => ReadComprehensionConsPattern(list, expanded),
            _ => null
        };
    }

    private static ComprehensionMatchPattern? ReadLiteralComprehensionConsPattern(Cons value)
    {
        var items = new List<Pattern>();
        Term tail = value;
        while (tail is Cons cell)
        {
            items.Add(new Pattern.Literal(cell.Head));
            tail = cell.Tail;
        }

        // beam_core_to_ssa expands a literal cons into the ordinary head and tail columns.
        var list = new Pattern.List(items, new Pattern.Literal(tail));

        return ReadComprehensionConsPattern(list, expanded: true);
    }

    private static ComprehensionMatchPattern? ReadComprehensionConsPattern(Pattern.List list, bool expanded)
    {
        var tail = ReadComprehensionMatchPattern(list.Tail ?? new Pattern.Literal(Nil.Value), expanded);
        for (int index = list.Items.Count - 1; index >= 0; index--)
        {
            if (tail is null || ReadComprehensionMatchPattern(list.Items[index], expanded) is not { } head)
            {
                return null;
            }

            Term? literal = head.Value is not null && tail.Value is not null
                ? new Cons(head.Value, tail.Value)
                : null;
            tail = new ComprehensionMatchPattern(ComprehensionPatternKind.Cons, literal, [head, tail]);
        }

        return tail;
    }

    private static ComprehensionMatchPattern? ReadComprehensionTuplePattern(
        IReadOnlyList<Pattern> items,
        bool expanded
    )
    {
        var fields = new ComprehensionMatchPattern[items.Count];
        for (int index = 0; index < fields.Length; index++)
        {
            if (ReadComprehensionMatchPattern(items[index], expanded) is not { } field)
            {
                return null;
            }

            fields[index] = field;
        }

        Term? literal = null;
        if (fields.All(field => field.Value is not null))
        {
            Term[] values = fields.Select(field => field.Value!).ToArray();
            literal = Term.Tuple(values);
        }

        return new ComprehensionMatchPattern(ComprehensionPatternKind.Tuple, literal, fields);
    }
}
