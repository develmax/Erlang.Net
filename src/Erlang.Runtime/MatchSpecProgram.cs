namespace Erlang;

public sealed class MatchSpecProgram
{
    private readonly IReadOnlyList<TupleTerm> clauses;

    internal MatchSpecProgram(MatchSpecDialect dialect, IEnumerable<TupleTerm> clauses, IEnumerable<MatchSpecTraceAction> actions)
    {
        Dialect = dialect;
        this.clauses = Array.AsReadOnly(clauses.ToArray());
        RequiredTraceActions = Array.AsReadOnly(actions.OrderBy(action => action.Name, StringComparer.Ordinal).ThenBy(action => action.Arity).ToArray());
    }

    public MatchSpecDialect Dialect { get; }

    public IReadOnlyList<MatchSpecTraceAction> RequiredTraceActions { get; }

    public ValueTask<MatchSpecResult> RunAsync(
        ProcessContext context,
        Term against,
        MatchSpecRunMode mode = MatchSpecRunMode.Test,
        IMatchSpecTraceActions? actions = null
    ) => ValueTask.FromResult(Run(
        context,
        against,
        mode,
        actions
    ));

    public MatchSpecResult Run(
        ProcessContext context,
        Term against,
        MatchSpecRunMode mode = MatchSpecRunMode.Test,
        IMatchSpecTraceActions? actions = null
    )
    {
        if (mode is not MatchSpecRunMode.Test and not MatchSpecRunMode.Production)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        foreach (var action in RequiredTraceActions)
        {
            if (mode == MatchSpecRunMode.Test ? !MatchSpecOperations.TestSupports(action.Name) : actions?.Supports(action.Name, action.Arity) != true)
                throw MatchSpecNames.UnsupportedAction(action.Name, action.Arity);
        }
        if (Dialect == MatchSpecDialect.Trace)
        {
            if (against is not Cons && against is not Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var arguments = new List<Term>();
            while (against is Cons cell)
            {
                arguments.Add(cell.Head);
                against = cell.Tail;
            }
            against = Cons.From(arguments);
        }
        foreach (var clause in clauses)
        {
            var state = new State(
                context,
                against,
                mode,
                actions
            );
            Term pattern = clause.Items[0];
            Term input = against;
            if (Dialect == MatchSpecDialect.Trace && pattern is TupleTerm)
                input = new TupleTerm(Cons.Items(against));
            if (!Matches(
                pattern,
                input,
                state.Bindings,
                0
            ))
                continue;
            bool accepted = true;
            foreach (Term guard in Cons.Items(clause.Items[1]))
            {
                try
                {
                    if (Evaluate(guard, state, 0) is not Atom { Name: ErlangBooleanAtoms.True })
                    {
                        accepted = false;
                        break;
                    }
                }
                catch (ErlangException)
                {
                    accepted = false;
                    break;
                }
            }
            if (!accepted)
                continue;
            Term value = Term.A(ErlangBooleanAtoms.True);
            foreach (Term body in Cons.Items(clause.Items[2]))
            {
                try
                {
                    value = Evaluate(body, state, 0);
                }
                catch (ErlangException)
                {
                    value = Term.A(MatchSpecNames.Exit);
                }
            }

            return new(
                true,
                Dialect == MatchSpecDialect.Trace ? state.TraceValue : value,
                state.ReturnTrace,
                state.ExceptionTrace
            );
        }

        return new(
            false,
            Term.A(ErlangBooleanAtoms.False),
            false,
            false
        );
    }

    private sealed class State(
        ProcessContext context,
        Term against,
        MatchSpecRunMode mode,
        IMatchSpecTraceActions? actions
    )
    {
        public ProcessContext Context { get; } = context;
        public Term Against { get; } = against;
        public MatchSpecRunMode Mode { get; } = mode;
        public IMatchSpecTraceActions? Actions { get; } = actions;
        public SortedDictionary<int, Term> Bindings { get; } = new();
        public Term TraceValue = Term.A(ErlangBooleanAtoms.True);
        public bool ReturnTrace;
        public bool ExceptionTrace;
    }

    private static bool Matches(
        Term pattern,
        Term value,
        SortedDictionary<int, Term> bindings,
        int depth
    )
    {
        MatchSpecCompiler.CheckDepth(depth);
        if (pattern is Atom { Name: MatchSpecNames.Wildcard })
            return true;
        if (MatchSpecCompiler.Variable(pattern, out int variable))
        {
            if (bindings.TryGetValue(variable, out Term? bound))
                return bound.Equals(value);
            bindings.Add(variable, value);

            return true;
        }
        if (pattern is FloatTerm number)
            return value is FloatTerm other && BitConverter.DoubleToInt64Bits(number.Value) == BitConverter.DoubleToInt64Bits(other.Value);
        if (pattern is TupleTerm tuple)
        {
            if (value is not TupleTerm candidate || tuple.Items.Count != candidate.Items.Count)
                return false;
            for (int index = 0; index < tuple.Items.Count; index++)
            {
                if (!Matches(
                    tuple.Items[index],
                    candidate.Items[index],
                    bindings,
                    depth + 1
                ))
                    return false;
            }

            return true;
        }
        if (pattern is Cons cell)
            return value is Cons candidate && Matches(
                cell.Head,
                candidate.Head,
                bindings,
                depth + 1
            ) && Matches(
                cell.Tail,
                candidate.Tail,
                bindings,
                depth + 1
            );
        if (pattern is MapTerm map)
        {
            if (value is not MapTerm candidate)
                return false;
            foreach (var pair in map.Entries)
            {
                if (!candidate.TryGet(pair.Key, out Term? item) || !Matches(
                    pair.Value,
                    item!,
                    bindings,
                    depth + 1
                ))
                    return false;
            }

            return true;
        }

        return pattern.Equals(value);
    }

    private static Term Evaluate(Term expression, State state, int depth)
    {
        MatchSpecCompiler.CheckDepth(depth);
        if (MatchSpecCompiler.Variable(expression, out int variable))
            return state.Bindings[variable];
        if (expression is Atom atom)
        {
            return atom.Name switch
            {
                MatchSpecNames.Whole => state.Against,
                MatchSpecNames.Variables => Cons.From(state.Bindings.Values),
                _ => atom
            };
        }
        if (expression is Cons cell)
        {
            Term head = Evaluate(cell.Head, state, depth + 1);
            Term tail = Evaluate(cell.Tail, state, depth + 1);

            return new Cons(head, tail);
        }
        if (expression is MapTerm map)
        {
            var entries = new KeyValuePair<Term, Term>[map.Entries.Count];
            var values = new Term[entries.Length];
            for (int index = entries.Length - 1; index >= 0; index--)
                values[index] = Evaluate(map.Entries[index].Value, state, depth + 1);
            for (int index = entries.Length - 1; index >= 0; index--)
            {
                Term key = Evaluate(map.Entries[index].Key, state, depth + 1);
                entries[index] = new(key, values[index]);
            }

            return new MapTerm(entries);
        }
        if (expression is not TupleTerm tuple)
            return expression;
        if (tuple.Items.Count == 1 && tuple.Items[0] is TupleTerm construction)
        {
            var items = new Term[construction.Items.Count];
            for (int index = items.Length - 1; index >= 0; index--)
                items[index] = Evaluate(construction.Items[index], state, depth + 1);

            return new TupleTerm(items);
        }
        string name = ((Atom)tuple.Items[0]).Name;
        if (name == MatchSpecNames.Constant)
            return tuple.Items[1];
        if (name == MatchSpecNames.Self)
            return state.Context.Self;
        if (name is MatchSpecOperations.AndAlso or MatchSpecOperations.AndThen or MatchSpecOperations.OrElse)
        {
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                Term value = Evaluate(tuple.Items[index], state, depth + 1);
                if (index == tuple.Items.Count - 1)
                    return value;
                bool condition = CoreModules.Bool(value);
                if (name == MatchSpecOperations.OrElse ? condition : !condition)
                    return CoreModules.Boolean(condition);
            }
        }
        var arguments = new Term[tuple.Items.Count - 1];
        for (int index = arguments.Length - 1; index >= 0; index--)
            arguments[index] = Evaluate(tuple.Items[index + 1], state, depth + 1);
        if (name is MatchSpecOperations.And or MatchSpecOperations.Or)
        {
            bool result = name == MatchSpecOperations.And;
            foreach (Term argument in arguments)
            {
                bool operand = CoreModules.Bool(argument);
                result = name == MatchSpecOperations.And ? result & operand : result | operand;
            }

            return CoreModules.Boolean(result);
        }
        if (MatchSpecOperations.Trace.ContainsKey(name))
        {
            Term result;
            if (state.Mode == MatchSpecRunMode.Production)
                result = state.Actions!.Execute(state.Context, name, arguments);
            else
                result = name == MatchSpecNames.Caller ? Term.A(MatchSpecNames.Undefined) : Term.A(ErlangBooleanAtoms.True);
            if (name == MatchSpecNames.Message)
                state.TraceValue = arguments[0];
            else if (name == MatchSpecNames.ReturnTrace)
                state.ReturnTrace = true;
            else if (name == MatchSpecNames.ExceptionTrace)
                state.ExceptionTrace = true;

            return result;
        }
        if (arguments.Length == 1 && MatchSpecOperations.Unary.Contains(name))
            return ErlangOperatorValues.Unary(name, arguments[0]);
        if (arguments.Length == 2 && MatchSpecOperations.Binary.Contains(name))
            return ErlangOperatorValues.Binary(
                name,
                arguments[0],
                arguments[1],
                state.Context
            );

        return state.Context.Runtime.Modules.CallMatchSpecGuard(state.Context, name, arguments);
    }
}
