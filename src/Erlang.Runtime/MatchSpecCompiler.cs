using System.Globalization;

namespace Erlang;

public static class MatchSpecCompiler
{
    public static MatchSpecCompilation Compile(Term specification, MatchSpecDialect dialect)
    {
        if (dialect is not MatchSpecDialect.Table and not MatchSpecDialect.Trace)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var errors = new List<string>();
        var clauses = new List<TupleTerm>();
        var actions = new HashSet<MatchSpecTraceAction>();
        if (specification is not Cons)
            errors.Add(MatchSpecDiagnostics.ProgramsNotList);
        else
        {
            Term proper = specification;
            while (proper is Cons part)
                proper = part.Tail;
            if (proper is not Nil)
                return new(null, Array.AsReadOnly(new[] { MatchSpecDiagnostics.ProgramsNotProper }));
            foreach (Term part in Cons.Items(specification))
            {
                if (part is not TupleTerm { Items.Count: 3 } shape)
                    return new(null, Array.AsReadOnly(new[] { MatchSpecDiagnostics.ClauseArity }));
                if (dialect == MatchSpecDialect.Trace && shape.Items[0] is Cons)
                {
                    Term rest = shape.Items[0];
                    while (rest is Cons head)
                        rest = head.Tail;
                    if (rest is not Nil && rest is not Atom { Name: MatchSpecNames.Wildcard })
                        return new(null, Array.AsReadOnly(new[] { MatchSpecDiagnostics.TraceHead(shape.Items[0]) }));
                }
            }
            Term tail = specification;
            while (tail is Cons cell)
            {
                if (cell.Head is not TupleTerm { Items.Count: 3 } clause)
                    errors.Add(MatchSpecDiagnostics.ClauseArity);
                else
                {
                    clauses.Add(clause);
                    var bound = new HashSet<int>();
                    int clauseDiagnosticsBefore = errors.Count;
                    MatchSpecPatternCompilationStatus patternStatus = ValidatePattern(
                        clause.Items[0],
                        bound,
                        errors
                    );
                    if (patternStatus == MatchSpecPatternCompilationStatus.HardFailure)
                        return new(null, errors.AsReadOnly());
                    if (dialect == MatchSpecDialect.Trace && clause.Items[0] is not Cons && clause.Items[0] is not Nil && clause.Items[0] is not TupleTerm && clause.Items[0] is not Atom { Name: MatchSpecNames.Wildcard } && !Variable(clause.Items[0], out _))
                    {
                        errors.Add(MatchSpecDiagnostics.InvalidHead);

                        return new(null, errors.AsReadOnly());
                    }
                    if (dialect == MatchSpecDialect.Trace && clause.Items[0] is Cons)
                    {
                        Term rest = clause.Items[0];
                        while (rest is Cons head)
                            rest = head.Tail;
                        if (rest is not Nil && rest is not Atom { Name: MatchSpecNames.Wildcard })
                            errors.Add(MatchSpecDiagnostics.TraceHead(clause.Items[0]));
                    }
                    ValidateExpressions(
                        clause.Items[1],
                        true,
                        dialect,
                        bound,
                        actions,
                        errors
                    );
                    if (dialect == MatchSpecDialect.Table && clause.Items[2] is Nil)
                    {
                        errors.Add(MatchSpecDiagnostics.EmptyTableBody);
                    }
                    else
                    {
                        ValidateExpressions(
                            clause.Items[2],
                            false,
                            dialect,
                            bound,
                            actions,
                            errors
                        );
                    }
                    if (errors.Count != clauseDiagnosticsBefore)
                        return new(null, errors.AsReadOnly());
                }
                tail = cell.Tail;
            }
            if (tail is not Nil)
                errors.Add(MatchSpecDiagnostics.ProgramsNotProper);
        }

        return new(errors.Count == 0 ? new MatchSpecProgram(dialect, clauses, actions) : null, errors.AsReadOnly());
    }

    internal static bool Variable(Term term, out int value)
    {
        value = 0;
        if (term is not Atom atom || atom.Name.Length < 2 || atom.Name[0] != '$')
            return false;
        ReadOnlySpan<char> digits = atom.Name.AsSpan(1);
        if (digits.Length > 1 && digits[0] == '0' || digits.ContainsAnyExceptInRange('0', '9'))
            return false;
        if (!int.TryParse(
            digits,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value
        ))
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return true;
    }

    internal static void CheckDepth(int depth)
    {
        if (depth >= MatchSpecNames.MaximumDepth)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
    }

    private readonly record struct PatternStep(Term Term, int Depth);

    private static MatchSpecPatternCompilationStatus ValidatePattern(
        Term pattern,
        HashSet<int> bound,
        List<string> errors
    )
    {
        var deferred = new Stack<PatternStep>();

        void Submit(Term value, int depth)
        {
            CheckDepth(depth);
            if (Variable(value, out int variable))
                bound.Add(variable);
            else if (value is TupleTerm or Cons or MapTerm)
                deferred.Push(new(value, depth));
        }

        Submit(pattern, 0);
        while (deferred.TryPop(out PatternStep step))
        {
            if (step.Term is TupleTerm tuple)
            {
                foreach (Term item in tuple.Items)
                    Submit(item, step.Depth + 1);
            }
            else if (step.Term is Cons)
            {
                Term cursor = step.Term;
                int depth = step.Depth;
                while (cursor is Cons cell)
                {
                    CheckDepth(depth);
                    Submit(cell.Head, depth + 1);
                    cursor = cell.Tail;
                    depth++;
                }
                Submit(cursor, depth);
            }
            else if (step.Term is MapTerm map)
            {
                foreach (var entry in map.Entries)
                {
                    if (Variable(entry.Key, out _))
                    {
                        errors.Add(MatchSpecDiagnostics.VariableMapKey);

                        return MatchSpecPatternCompilationStatus.HardFailure;
                    }
                    if (entry.Key is Atom { Name: MatchSpecNames.Wildcard })
                    {
                        errors.Add(MatchSpecDiagnostics.WildcardMapKey);

                        return MatchSpecPatternCompilationStatus.HardFailure;
                    }
                    Submit(entry.Value, step.Depth + 1);
                }
            }
        }

        return MatchSpecPatternCompilationStatus.Completed;
    }

    private static void ValidateExpressions(
        Term expressions,
        bool guard,
        MatchSpecDialect dialect,
        HashSet<int> bound,
        HashSet<MatchSpecTraceAction> actions,
        List<string> errors
    )
    {
        if (expressions is not Cons && expressions is not Nil)
        {
            errors.Add(MatchSpecDiagnostics.ExpressionsNotList);

            return;
        }
        while (expressions is Cons cell)
        {
            ValidateExpression(
                cell.Head,
                guard,
                dialect,
                bound,
                actions,
                errors,
                0
            );
            expressions = cell.Tail;
        }
        if (expressions is not Nil)
            errors.Add(MatchSpecDiagnostics.ExpressionsNotProper);
    }

    private static void ValidateExpression(
        Term expression,
        bool guard,
        MatchSpecDialect dialect,
        HashSet<int> bound,
        HashSet<MatchSpecTraceAction> actions,
        List<string> errors,
        int depth
    )
    {
        CheckDepth(depth);
        if (Variable(expression, out int variable))
        {
            if (!bound.Contains(variable))
                errors.Add(MatchSpecDiagnostics.Unbound(variable));
        }
        else if (expression is Cons cell)
        {
            ValidateExpression(
                cell.Head,
                guard,
                dialect,
                bound,
                actions,
                errors,
                depth + 1
            );
            ValidateExpression(
                cell.Tail,
                guard,
                dialect,
                bound,
                actions,
                errors,
                depth + 1
            );
        }
        else if (expression is MapTerm map)
        {
            if (map.Entries.Count > MatchSpecNames.MaximumConstructedMapEntries)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            foreach (var pair in map.Entries.Reverse())
                ValidateExpression(
                    pair.Value,
                    guard,
                    dialect,
                    bound,
                    actions,
                    errors,
                    depth + 1
                );
            foreach (var pair in map.Entries.Reverse())
                ValidateExpression(
                    pair.Key,
                    guard,
                    dialect,
                    bound,
                    actions,
                    errors,
                    depth + 1
                );
        }
        else if (expression is TupleTerm tuple)
        {
            if (tuple.Items.Count == 1 && tuple.Items[0] is TupleTerm construction)
            {
                foreach (Term item in construction.Items.Reverse())
                    ValidateExpression(
                        item,
                        guard,
                        dialect,
                        bound,
                        actions,
                        errors,
                        depth + 1
                    );

                return;
            }
            if (tuple.Items.Count == 0 || tuple.Items[0] is not Atom name || Variable(name, out _))
            {
                errors.Add(MatchSpecDiagnostics.InvalidTuple(tuple));

                return;
            }
            int arity = tuple.Items.Count - 1;
            int beforeErrors = errors.Count;
            if (name.Name == MatchSpecNames.Constant)
            {
                if (arity != 1)
                    errors.Add(MatchSpecDiagnostics.ConstantArguments(tuple));

                return;
            }
            if (name.Name == MatchSpecNames.Self)
            {
                if (arity != 0)
                    errors.Add(MatchSpecDiagnostics.NoArguments(name.Name, tuple));

                return;
            }
            if (MatchSpecOperations.Trace.TryGetValue(name.Name, out var arities))
            {
                if (name.Name is MatchSpecOperations.ControlWordGet or MatchSpecOperations.ControlWordSet && !arities.Contains(arity))
                    errors.Add(MatchSpecDiagnostics.Unknown(name.Name, arity));
                else if (name.Name is MatchSpecOperations.ControlWordGet or MatchSpecOperations.ControlWordSet && (dialect != MatchSpecDialect.Trace || guard && name.Name == MatchSpecOperations.ControlWordSet))
                    errors.Add(MatchSpecDiagnostics.Context(name.Name, arity));
                else if (dialect != MatchSpecDialect.Trace)
                    errors.Add(MatchSpecDiagnostics.Dialect(name.Name));
                else if (guard && name.Name is not MatchSpecOperations.ControlWordGet and not MatchSpecOperations.IsSequentialTrace)
                    errors.Add(MatchSpecDiagnostics.Guard(name.Name));
                else if (!arities.Contains(arity))
                    errors.Add(arity == 0 || arities.Count != 1 || !arities.Contains(0) ? MatchSpecDiagnostics.ArgumentCount(name.Name, tuple) : MatchSpecDiagnostics.NoArguments(name.Name, tuple));
                else
                    actions.Add(new(name.Name, arity));
            }
            else if (MatchSpecOperations.IsBoolean(name.Name))
            {
                if (arity == 0)
                    errors.Add(MatchSpecDiagnostics.EmptyBoolean(name.Name, tuple));
            }
            else if (!MatchSpecOperations.IsPure(name.Name, arity))
            {
                errors.Add(MatchSpecDiagnostics.Unknown(name.Name, arity));

                return;
            }
            if (errors.Count != beforeErrors)
                return;
            if (name.Name is MatchSpecOperations.AndAlso or MatchSpecOperations.OrElse or MatchSpecOperations.AndThen)
            {
                for (int index = 1; index < tuple.Items.Count; index++)
                    ValidateExpression(
                        tuple.Items[index],
                        guard,
                        dialect,
                        bound,
                        actions,
                        errors,
                        depth + 1
                    );

                return;
            }
            for (int index = tuple.Items.Count - 1; index > 0; index--)
                ValidateExpression(
                    tuple.Items[index],
                    guard,
                    dialect,
                    bound,
                    actions,
                    errors,
                    depth + 1
                );
        }
    }
}
