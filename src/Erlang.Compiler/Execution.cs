namespace Erlang.Compiler;

public static class Execution
{
    private sealed record Selection(Clause Clause, Dictionary<string, Term> Bindings);
    private sealed class TailCall(string function, IReadOnlyList<Term> arguments) : Exception
    {
        public string Function { get; } = function; public IReadOnlyList<Term> Arguments { get; } = arguments;
    }

    public static ValueTask<Term> EvaluateAsync(Expr expression, ProcessContext context) => Evaluate(
        expression,
        context,
        new Dictionary<string, Term>(StringComparer.Ordinal),
        null
    );

    public static async ValueTask<Term> InvokeAsync(
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    )
    {
        while (true)
        {
            var definition = module.Functions.FirstOrDefault(f => f.Name == function && f.Arity == arguments.Count) ?? throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
            var selection = Select(
                definition.Clauses,
                arguments,
                [],
                context
            ) ?? throw new ErlangException(ErlangErrorReasons.FunctionClause);
            try
            {
                return await Evaluate(
                    selection.Clause.Body,
                    context,
                    selection.Bindings,
                    module,
                    true
                );
            }
            catch (TailCall next)
            {
                function = next.Function;
                arguments = next.Arguments;
            }
        }
    }

    private static Selection? Select(
        IReadOnlyList<Clause> clauses,
        IReadOnlyList<Term> values,
        Dictionary<string, Term> parent,
        ProcessContext ctx,
        Dictionary<string, Term>? keyScope = null
    )
    {
        foreach (var clause in clauses)
        {
            var b = new Dictionary<string, Term>(parent, StringComparer.Ordinal);
            bool matched = clause.Patterns.Count == values.Count;
            for (int i = 0; matched && i < values.Count; i++)
                matched = clause.Patterns[i].Match(
                    values[i],
                    b,
                    ctx,
                    keyScope ?? parent
                );
            if (matched && Guard(clause.Guard, b, ctx))
                return new(clause, b);
        }

        return null;
    }

    private static bool Guard(Expr? e, Dictionary<string, Term> b, ProcessContext ctx)
    {
        if (e is null)
            return true;
        if (e is Expr.GuardAlternatives alternatives)
            return alternatives.Items.Any(x => Guard(x, b, ctx));
        try
        {
            return GuardValue(e, b, ctx).Equals(Term.A(ErlangBooleanAtoms.True));
        }
        catch (ErlangException)
        {
            return false;
        }
    }

    internal static Term PatternKey(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => GuardValue(e, b, ctx);

    private static Term GuardValue(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => e switch
    {
        Expr.Literal l => l.Value,
        Expr.Variable v => b.TryGetValue(v.Name, out var value) ? value : throw new ErlangException(ExecutionErrorReasons.Unbound),
        Expr.Tuple t => new TupleTerm(t.Items.Select(x => GuardValue(x, b, ctx))),
        Expr.List l => Cons.From(l.Items.Select(x => GuardValue(x, b, ctx)), l.Tail is null ? null : GuardValue(l.Tail, b, ctx)),
        Expr.Map m => EvaluateMap(
            m.Base is null ? new MapTerm([]) : GuardValue(m.Base, b, ctx),
            m.Fields.Select(f => (GuardValue(f.Key, b, ctx), GuardValue(f.Value, b, ctx), f.Exact)).ToArray()
        ),
        Expr.Bits bits => BitConstruction.Create(bits.Segments.Select(s => (GuardValue(s.Value, b, ctx), s.Size is null ? null : GuardValue(s.Size, b, ctx), s)).ToArray()),
        Expr.Unary u => Unary(u.Operator, GuardValue(u.Operand, b, ctx)),
        Expr.Binary { Operator: ErlangOperators.AndAlso } x => CoreModules.Bool(GuardValue(x.Left, b, ctx)) ? GuardValue(x.Right, b, ctx) : Term.A(ErlangBooleanAtoms.False),
        Expr.Binary { Operator: ErlangOperators.OrElse } x => CoreModules.Bool(GuardValue(x.Left, b, ctx)) ? Term.A(ErlangBooleanAtoms.True) : GuardValue(x.Right, b, ctx),
        Expr.Binary x => Binary(
            x.Operator,
            GuardValue(x.Left, b, ctx),
            GuardValue(x.Right, b, ctx),
            ctx
        ),
        Expr.Call x when ctx is not null && Semantics.GuardBifs.Contains((x.Function, x.Arguments.Count)) => ctx.Runtime.Modules.Call(
            ctx,
            ErlangModuleNames.Module,
            x.Function,
            x.Arguments.Select(a => GuardValue(a, b, ctx)).ToArray()
        ).GetAwaiter().GetResult(),
        _ => throw new ErlangException(ExecutionErrorReasons.IllegalGuard)
    };

    private static async ValueTask<Term> Evaluate(
        Expr e,
        ProcessContext ctx,
        Dictionary<string, Term> b,
        ModuleDefinition? module,
        bool tail = false
    )
    {
        await ctx.ReduceAsync();

        async ValueTask<Term[]> Arguments(IReadOnlyList<Expr> expressions)
        {
            if (module is not null)
                return await CompiledExpressionBindings.EvaluateList(
                    expressions,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    )
                );

            return await ExpressionBindings.EvaluateList(
                expressions,
                b,
                (expression, scope) => Evaluate(
                    expression,
                    ctx,
                    scope,
                    module
                )
            );
        }

        async ValueTask<Term> Branch(Selection selection)
        {
            var result = await Evaluate(
                selection.Clause.Body,
                ctx,
                selection.Bindings,
                module,
                tail
            );
            foreach (var v in selection.Bindings)
                b[v.Key] = v.Value;

            return result;
        }
        switch (e)
        {
            case Expr.Literal l:
                return l.Value;
            case Expr.Variable v:
                return b.TryGetValue(v.Name, out var boundValue) ? boundValue : throw new ErlangException(ExecutionErrorReasons.Unbound);
            case Expr.Tuple t:
                return new TupleTerm(await Arguments(t.Items));
            case Expr.List l:
                if (module is not null)
                    return await CompiledExpressionBindings.EvaluateCons(
                        l,
                        b,
                        (expression, scope) => Evaluate(
                            expression,
                            ctx,
                            scope,
                            module
                        )
                    );

                return await ExpressionBindings.EvaluateCons(
                    l,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    )
                );
            case Expr.Map m:
                {
                    Term mapBase = m.Base is null ? new MapTerm([]) : await Evaluate(
                        m.Base,
                        ctx,
                        b,
                        module
                    );
                    var fields = new (Term Key, Term Value, bool Exact)[m.Fields.Count];
                    for (int i = 0; i < fields.Length; i++)
                        fields[i] = (await Evaluate(
                            m.Fields[i].Key,
                            ctx,
                            b,
                            module
                        ), await Evaluate(
                            m.Fields[i].Value,
                            ctx,
                            b,
                            module
                        ), m.Fields[i].Exact);

                    // Evaluate expressions before map type/key checks, as required by reference error cases.
                    return EvaluateMap(mapBase, fields);
                }
            case Expr.Bits bits:
                {
                    var segments = new (Term Value, Term? Size, BitSegment Segment)[bits.Segments.Count];
                    for (int i = 0; i < segments.Length; i++)
                    {
                        var segment = bits.Segments[i];
                        segments[i] = (await Evaluate(
                            segment.Value,
                            ctx,
                            b,
                            module
                        ), segment.Size is null ? null : await Evaluate(
                            segment.Size,
                            ctx,
                            b,
                            module
                        ), segment);
                    }

                    return BitConstruction.Create(segments);
                }
            case Expr.Sequence s:
                {
                    Term result = Term.A(ExecutionResultAtoms.EmptySequence);
                    for (int i = 0; i < s.Items.Count; i++)
                        result = await Evaluate(
                            s.Items[i],
                            ctx,
                            b,
                            module,
                            tail && i == s.Items.Count - 1
                        );

                    return result;
                }
            case Expr.Match m:
                {
                    var value = await Evaluate(
                        m.Value,
                        ctx,
                        b,
                        module
                    );
                    if (!m.Pattern.Match(value, b, ctx))
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value));

                    return value;
                }
            case Expr.Block block:
                return await Evaluate(
                    block.Body,
                    ctx,
                    b,
                    module,
                    tail
                );
            case Expr.Catch caught:
                {
                    var scope = new Dictionary<string, Term>(b, StringComparer.Ordinal);
                    try
                    {
                        var result = await Evaluate(
                            caught.Operand,
                            ctx,
                            scope,
                            module
                        );
                        foreach (var binding in scope)
                            b[binding.Key] = binding.Value;

                        return result;
                    }
                    catch (ErlangException exception)
                    {
                        return exception.ExceptionClass switch
                        {
                            ErlangExceptionClasses.Throw => exception.Reason,
                            ErlangExceptionClasses.Exit => Term.Tuple(Term.A(CatchResultAtoms.Exit), exception.Reason),
                            _ => Term.Tuple(Term.A(CatchResultAtoms.Exit), Term.Tuple(exception.Reason, exception.StackTraceTerm))
                        };
                    }
                }
            case Expr.Unary u:
                return Unary(u.Operator, await Evaluate(
                    u.Operand,
                    ctx,
                    b,
                    module
                ));
            case Expr.Binary x:
                {
                    bool shortCircuit = x.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse;
                    var leftScope = shortCircuit ? b : new Dictionary<string, Term>(b, StringComparer.Ordinal);
                    var left = await Evaluate(
                        x.Left,
                        ctx,
                        leftScope,
                        module
                    );
                    if (shortCircuit && left is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), left)).WithStackFrame(ErlangModuleNames.Module, x.Operator, [left]);
                    if (x.Operator == ErlangOperators.AndAlso && left.Equals(Term.A(ErlangBooleanAtoms.False)))
                        return Term.A(ErlangBooleanAtoms.False);
                    if (x.Operator == ErlangOperators.OrElse && left.Equals(Term.A(ErlangBooleanAtoms.True)))
                        return Term.A(ErlangBooleanAtoms.True);
                    var rightScope = new Dictionary<string, Term>(b, StringComparer.Ordinal);
                    var right = await Evaluate(
                        x.Right,
                        ctx,
                        rightScope,
                        module
                    );
                    if (!shortCircuit)
                    {
                        foreach (var binding in rightScope)
                            if (leftScope.TryGetValue(binding.Key, out var previous) && !previous.Equals(binding.Value))
                                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), binding.Value));
                        foreach (var binding in leftScope)
                            b[binding.Key] = binding.Value;
                        foreach (var binding in rightScope)
                            b[binding.Key] = binding.Value;
                    }

                    return x.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse ? right : Binary(
                        x.Operator,
                        left,
                        right,
                        ctx
                    );
                }
            case Expr.Call x:
                {
                    var args = await Arguments(x.Arguments);
                    if (module is not null && (x.Module is null || x.Module == module.Name && module.Exports.Contains((x.Function, args.Length))) && module.Functions.Any(f => f.Name == x.Function && f.Arity == args.Length))
                    {
                        if (tail)
                            throw new TailCall(x.Function, args);

                        return await InvokeAsync(
                            module,
                            x.Function,
                            ctx,
                            args
                        );
                    }

                    try
                    {
                        return await ctx.Runtime.Modules.Call(
                            ctx,
                            x.Module ?? ErlangModuleNames.Module,
                            x.Function,
                            args
                        );
                    }
                    catch (ErlangException exception)
                    {
                        throw exception.WithStackFrame(x.Module ?? ErlangModuleNames.Module, x.Function, args);
                    }
                }
            case Expr.Apply x:
                {
                    var fun = await Evaluate(
                        x.Function,
                        ctx,
                        b,
                        module
                    );
                    var args = await Arguments(x.Arguments);

                    return fun is FunctionTerm f ? await f.Invoke(ctx, args) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), fun));
                }
            case Expr.Case x:
                {
                    var value = await Evaluate(
                        x.Value,
                        ctx,
                        b,
                        module
                    );

                    return await Branch(Select(
                        x.Clauses,
                        [value],
                        b,
                        ctx
                    ) ?? throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value)));
                }
            case Expr.If x:
                return await Branch(Select(
                    x.Clauses,
                    [],
                    b,
                    ctx
                ) ?? throw new ErlangException(ErlangErrorReasons.IfClause));
            case Expr.Receive x:
                {
                    TimeSpan? timeout = null;
                    if (x.Timeout is not null)
                    {
                        var time = await Evaluate(
                            x.Timeout,
                            ctx,
                            b,
                            module
                        );
                        timeout = time switch
                        {
                            Atom { Name: ReceiveTimeoutAtoms.Infinity } => null,
                            Integer i when i.Value >= 0 && i.Value <= uint.MaxValue => TimeSpan.FromMilliseconds((double)i.Value),
                            _ => throw new ErlangException(ErlangErrorReasons.TimeoutValue)
                        };
                    }
                    var selection = await ctx.ReceiveAsync(message => Select(
                        x.Clauses,
                        [message],
                        b,
                        ctx
                    ), timeout);
                    if (selection is not null)
                        return await Branch(selection);

                    return x.After is not null ? await Evaluate(
                        x.After,
                        ctx,
                        b,
                        module,
                        tail
                    ) : throw new ErlangException(ErlangErrorReasons.Timeout);
                }
            case Expr.Fun f:
                {
                    var capture = new Dictionary<string, Term>(b, StringComparer.Ordinal);

                    return new FunctionTerm(
                        f.Clauses[0].Patterns.Count,
                        async (context, args) =>
                    {
                        var process = (ProcessContext)context;
                        foreach (var clause in f.Clauses)
                        {
                            var scope = new Dictionary<string, Term>(capture, StringComparer.Ordinal);
                            foreach (string name in clause.Patterns.SelectMany(Semantics.Variables))
                                scope.Remove(name);
                            var selection = Select(
                                [clause],
                                args,
                                scope,
                                process,
                                capture
                            );
                            if (selection is not null)
                                return await Evaluate(
                                    selection.Clause.Body,
                                    process,
                                    selection.Bindings,
                                    module
                                );
                        }
                        throw new ErlangException(ErlangErrorReasons.FunctionClause);
                    }
                    );
                }
            default:
                throw new NotSupportedException();
        }
    }

    private static Term Unary(string op, Term value)
    {
        try
        {
            return UnaryValue(op, value);
        }
        catch (ErlangException exception)
        {
            throw exception.WithStackFrame(ErlangModuleNames.Module, op, [value]);
        }
    }

    private static Term Binary(
        string op,
        Term a,
        Term b,
        ProcessContext? context
    )
    {
        try
        {
            return BinaryValue(
                op,
                a,
                b,
                context
            );
        }
        catch (ErlangException exception)
        {
            throw exception.WithStackFrame(ErlangModuleNames.Module, op, [a, b]);
        }
    }

    private static Term UnaryValue(string op, Term value) => op switch
    {
        ErlangOperators.Plus when value is Integer or FloatTerm => value,
        ErlangOperators.Minus when value is FloatTerm f => new FloatTerm(-f.Value),
        ErlangOperators.Minus => CoreModules.Arithmetic(ErlangOperators.Minus, Term.I(0), value),
        ErlangOperators.Not => CoreModules.Boolean(!CoreModules.Bool(value)),
        ErlangOperators.BitwiseNot when value is Integer integer => new Integer(~integer.Value),
        _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
    };

    private static MapTerm EvaluateMap(Term mapBase, IReadOnlyList<(Term Key, Term Value, bool Exact)> fields)
    {
        if (mapBase is not MapTerm map)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), mapBase));
        var entries = map.Entries.ToDictionary(e => e.Key, e => e.Value);
        foreach (var field in fields)
        {
            if (field.Exact && !entries.ContainsKey(field.Key))
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), field.Key));
            entries[field.Key] = field.Value;
        }

        return new MapTerm(entries);
    }

    private static Term BinaryValue(
        string op,
        Term a,
        Term b,
        ProcessContext? ctx
    ) => op switch
    {
        ErlangOperators.And => CoreModules.Boolean(CoreModules.Bool(a) & CoreModules.Bool(b)),
        ErlangOperators.Or => CoreModules.Boolean(CoreModules.Bool(a) | CoreModules.Bool(b)),
        ErlangOperators.Xor => CoreModules.Boolean(CoreModules.Bool(a) ^ CoreModules.Bool(b)),
        ErlangOperators.NumericEqual => CoreModules.Boolean(a.NumericEquals(b)),
        ErlangOperators.NumericNotEqual => CoreModules.Boolean(!a.NumericEquals(b)),
        ErlangOperators.ExactEqual => CoreModules.Boolean(a.Equals(b)),
        ErlangOperators.ExactNotEqual => CoreModules.Boolean(!a.Equals(b)),
        ErlangOperators.Less => CoreModules.Boolean(a.CompareTo(b) < 0),
        ErlangOperators.Greater => CoreModules.Boolean(a.CompareTo(b) > 0),
        ErlangOperators.LessOrEqual => CoreModules.Boolean(a.CompareTo(b) <= 0),
        ErlangOperators.GreaterOrEqual => CoreModules.Boolean(a.CompareTo(b) >= 0),
        ErlangOperators.Append => Cons.From(Cons.Items(a), b),
        ErlangOperators.SubtractList => Subtract(a, b),
        ErlangOperators.Send when ctx is not null => ctx.Runtime.Send(
            a is Pid p ? p : a is Atom name && ctx.Runtime.WhereIs(name.Name) is Pid registered ? registered : throw new ErlangException(ErlangErrorReasons.BadArgument),
            b
        ),
        _ => CoreModules.Arithmetic(op, a, b)
    };

    private static Term Subtract(Term a, Term b)
    {
        var items = Cons.Items(a).ToList();
        foreach (var x in Cons.Items(b))
        {
            int i = items.FindIndex(v => v.Equals(x));
            if (i >= 0)
                items.RemoveAt(i);
        }

        return Cons.From(items);
    }
}
