namespace Erlang.Compiler;

public static partial class Execution
{
    private sealed record Selection(Clause Clause, Dictionary<string, Term> Bindings);
    private sealed class TailCall(string function, IReadOnlyList<Term> arguments, ModuleCodeImage? image) : Exception
    {
        public string Function { get; } = function; public IReadOnlyList<Term> Arguments { get; } = arguments;
        public ModuleCodeImage? Image { get; } = image;
    }

    public static ValueTask<Term> EvaluateAsync(Expr expression, ProcessContext context) => Evaluate(
        expression,
        context,
        new Dictionary<string, Term>(StringComparer.Ordinal),
        null
    );

    public static ValueTask<Term> InvokeAsync(
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    ) => InvokeAsync(
        module,
        function,
        context,
        arguments,
        null
    );

    internal static async ValueTask<Term> InvokeAsync(
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        ModuleCodeImage? image,
        SourceCodeLease? lease = null
    )
    {
        image ??= context.ExecutingSourceImage(module) ?? context.Runtime.Modules.SourceImage(module.Name, module);
        try
        {
            while (true)
            {
                if (lease is null && image is not null)
                    lease = await context.Runtime.Modules.AcquireSourceCode(context, image);
                try
                {
                    using var flow = lease?.Attach(module);

                    return await InvokeFrame(
                        module,
                        function,
                        context,
                        arguments,
                        image
                    );
                }
                catch (TailCall next)
                {
                    function = next.Function;
                    arguments = next.Arguments;
                    image = next.Image;
                }
                catch (SourceTailCall next)
                {
                    lease?.Dispose();
                    lease = null;
                    module = next.Module;
                    function = next.Function;
                    arguments = next.Arguments;
                    image = next.Image;
                }
                catch (RuntimeTailCall next)
                {
                    lease?.Dispose();
                    lease = null;

                    return await CallRuntime(
                        context,
                        next.Module,
                        next.Function,
                        next.Arguments
                    );
                }
            }
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private static async ValueTask<Term> InvokeFrame(
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        ModuleCodeImage? image
    )
    {
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();

            await context.YieldExecution();

            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
        }

        using var frame = context.EnterFunction(module.Name, function, arguments.Count);
        try
        {
            if (image is not null && NifImageBindingStore.TryGetValue(image, out var bindings))
            {
                using var native = bindings!.Admit(function, arguments.Count);
                if (native is not null)
                    return await native.InvokeAsync(context, arguments);
            }

            var definition = module.Functions.FirstOrDefault(f => f.Name == function && f.Arity == arguments.Count) ?? throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
            var selection = await Select(
                definition.Clauses,
                arguments,
                [],
                context
            ) ?? throw new ErlangException(ErlangErrorReasons.FunctionClause);

            return await Evaluate(
                selection.Clause.Body,
                context,
                selection.Bindings,
                module,
                true
            );
        }
        catch (ErlangException exception)
        {
            throw context.CaptureSourceFailure(
                exception,
                exception.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)) ? arguments : null
            );
        }
    }

    private static async ValueTask<Term> CallRuntime(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments
    )
    {
        ModuleCallDispatchKind dispatch = ModuleCallDispatchKind.Direct;
        try
        {
            return await context.Runtime.Modules.CallWithDispatch(
                context,
                module,
                function,
                arguments,
                out dispatch
            );
        }
        catch (ErlangException exception)
        {
            if (dispatch == ModuleCallDispatchKind.UndefinedFunctionHandler)
                throw;
            throw RuntimeCallFrames.Capture(
                context,
                exception,
                module,
                function,
                arguments
            );
        }
    }

    internal static async ValueTask<Term> InvokeAnonymousAsync(
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun expression,
        Dictionary<string, Term> environment,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments,
        SourceCodeLease? lease = null
    )
    {
        try
        {
            if (lease is null && image is not null)
                lease = await context.Runtime.Modules.AcquireSourceCode(context, image);
            using var flow = module is null ? null : lease?.Attach(module);

            return await InvokeAnonymousFrame(
                module,
                identity,
                expression,
                environment,
                context,
                image,
                arguments
            );
        }
        catch (TailCall next) when (module is not null)
        {
            var transferred = lease;
            lease = null;

            return await InvokeAsync(
                module,
                next.Function,
                context,
                next.Arguments,
                next.Image,
                transferred
            );
        }
        catch (SourceTailCall next)
        {
            lease?.Dispose();
            lease = null;

            return await InvokeAsync(
                next.Module,
                next.Function,
                context,
                next.Arguments,
                next.Image
            );
        }
        catch (RuntimeTailCall next)
        {
            lease?.Dispose();
            lease = null;

            return await CallRuntime(
                context,
                next.Module,
                next.Function,
                next.Arguments
            );
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private static async ValueTask<Term> InvokeAnonymousFrame(
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun expression,
        Dictionary<string, Term> environment,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments
    )
    {
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();

            await context.YieldExecution();

            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
        }

        using var frame = context.EnterFunction(identity.Module.Name, identity.Name!.Name, arguments.Count);
        try
        {
            foreach (var clause in expression.Clauses)
            {
                var scope = new Dictionary<string, Term>(environment, StringComparer.Ordinal);
                foreach (string name in clause.Patterns.SelectMany(Semantics.Variables))
                    scope.Remove(name);
                var selection = await Select(
                    [clause],
                    arguments,
                    scope,
                    context,
                    environment
                );
                if (selection is not null)
                    return await Evaluate(
                        selection.Clause.Body,
                        context,
                        selection.Bindings,
                        module,
                        true
                    );
            }

            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        catch (ErlangException exception)
        {
            throw context.CaptureSourceFailure(
                exception,
                exception.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)) ? arguments : null
            );
        }
    }

    private static async ValueTask<Selection?> Select(
        IReadOnlyList<Clause> clauses,
        IReadOnlyList<Term> values,
        Dictionary<string, Term> parent,
        ProcessContext ctx,
        Dictionary<string, Term>? keyScope = null
    )
    {
        foreach (var clause in clauses)
        {
            var b = CompiledBindingScope.Copy(parent);
            bool matched = clause.Patterns.Count == values.Count;
            for (int i = 0; matched && i < values.Count; i++)
                matched = await clause.Patterns[i].MatchAsync(
                    values[i],
                    b,
                    ctx,
                    keyScope ?? parent
                );
            if (matched && await GuardAsync(clause.Guard, b, ctx))
                return new(clause, b);
        }

        return null;
    }



    internal static Term PatternKey(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => GuardValue(e, b, ctx);

    private static Term GuardValue(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => e switch
    {
        Expr.Literal l => l.Value,
        Expr.Variable v => b.TryGetValue(v.Name, out var value) ? value : throw new ErlangException(Term.Tuple(Term.A(ExecutionErrorReasons.Unbound), Term.A(v.Name))),
        Expr.RecordTest record => GuardValue(record.Value, b, ctx) is TupleTerm tuple && tuple.Items.Count == record.Arity && tuple.Items[0] is Atom tag && tag.Name == record.Name ? Term.A(ErlangBooleanAtoms.True) : throw new ErlangException(ErlangErrorReasons.BadArgument),
        NativeRecordExpression { Operation: NativeRecordOperation.Access } record => NativeRecordLanguage.Access(
            GuardValue(record.Value!, b, ctx),
            record.Module,
            record.Name,
            record.Caller,
            record.External,
            Term.A(record.Fields[0].Name)
        ),
        Expr.RecordConstruction record => GuardValue(RecordExpressions.Create(record), b, ctx),
        Expr.RecordAccess record => RecordExpressions.Access(
            GuardValue(record.Value, b, ctx),
            record.Name,
            record.Arity,
            record.Index
        ),
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
        Expr.Call x when RecordPredicateSyntax.HasLiteralNonpositiveSize(x) => Term.A(ErlangBooleanAtoms.False),
        Expr.Call x when RecordPredicateSyntax.TupleTest(x) is { } predicate => RecordPredicateSyntax.Test(GuardValue(x.Arguments[0], b, ctx), predicate),
        Expr.Call x when x.Module == ErlangModuleNames.Module && x.Arguments.Count == 1 && ComprehensionGuard.IsOperator(x.Function, 1) => Unary(x.Function, GuardValue(x.Arguments[0], b, ctx)),
        Expr.Call x when x.Module == ErlangModuleNames.Module && x.Arguments.Count == 2 && ComprehensionGuard.IsOperator(x.Function, 2) => Binary(
            x.Function,
            GuardValue(x.Arguments[0], b, ctx),
            GuardValue(x.Arguments[1], b, ctx),
            ctx
        ),
        // Synchronous host Pattern.Match retains its synchronous contract.
        // Source execution uses PatternKeyAsync and the cooperative BIF.
        Expr.Call x when ctx is not null && x.Function == ErlangModuleNames.Length && x.Arguments.Count == 1 => ErlangListLength.ReadMatchSpec(GuardValue(x.Arguments[0], b, ctx)),
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
        try
        {
            return await EvaluateCore(
                e,
                ctx,
                b,
                module,
                tail
            );
        }
        catch (ErlangException exception)
        {
            throw ctx.CaptureSourceFailure(exception);
        }
    }

    private static async ValueTask<Term> EvaluateCore(
        Expr e,
        ProcessContext ctx,
        Dictionary<string, Term> b,
        ModuleDefinition? module,
        bool tail = false
    )
    {
        await ctx.ExecutionCheckpoint();

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
                return b.TryGetValue(v.Name, out var boundValue) ? boundValue : throw new ErlangException(Term.Tuple(Term.A(ExecutionErrorReasons.Unbound), Term.A(v.Name)));
            case NativeRecordExpression record:
                {
                    if (record.Operation == NativeRecordOperation.Access)
                        return NativeRecordLanguage.Access(
                            await Evaluate(
                                record.Value!,
                                ctx,
                                b,
                                module
                            ),
                            record.Module,
                            record.Name,
                            record.Caller,
                            record.External,
                            Term.A(record.Fields[0].Name)
                        );
                    var inputs = await Arguments(record.Children.ToArray());
                    int valueOffset = record.Value is null ? 0 : 1;
                    var fields = record.Fields.Select((field, index) => new KeyValuePair<Atom, Term>(Term.A(field.Name), inputs[index + valueOffset])).ToArray();
                    if (record.Operation == NativeRecordOperation.Update)
                        return NativeRecordLanguage.Update(
                            inputs[0],
                            record.Module,
                            record.Name,
                            record.Caller,
                            record.External,
                            fields
                        );
                    var definition = record.External ? ctx.Runtime.Modules.NativeRecordDefinition(record.Module, record.Name!) : module?.NativeRecords.FirstOrDefault(value => value.Name == record.Name);

                    return NativeRecordLanguage.Construct(
                        record.Module,
                        definition,
                        record.Name!,
                        fields,
                        record.External
                    );
                }
            case Expr.RecordConstruction record:
                return await Evaluate(
                    RecordExpressions.Create(record),
                    ctx,
                    b,
                    module
                );
            case Expr.RecordAccess record:
                return RecordExpressions.Access(
                    await Evaluate(
                        record.Value,
                        ctx,
                        b,
                        module
                    ),
                    record.Name,
                    record.Arity,
                    record.Index
                );
            case Expr.RecordUpdate record:
                var updates = await Arguments(record.Fields.Select(field => field.Value).ToArray());
                var original = await Evaluate(
                    record.Value,
                    ctx,
                    b,
                    module
                );

                return RecordExpressions.Update(original, record, updates);
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
                return await MapExpressionBindings.Evaluate(
                    m,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    ),
                    EvaluateMap,
                    module is not null
                );
            case Expr.ListComprehension comprehension:
                return await ComprehensionExecution.Evaluate(
                    comprehension,
                    b,
                    ctx,
                    module,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    )
                );
            case Expr.BinaryComprehension binaryComprehension:
                return await ComprehensionExecution.Evaluate(
                    binaryComprehension,
                    b,
                    ctx,
                    module,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    )
                );
            case Expr.MapComprehension mapComprehension:
                return await ComprehensionExecution.Evaluate(
                    mapComprehension,
                    b,
                    ctx,
                    module,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    )
                );
            case Expr.Bits bits:
                return await BitExpressionBindings.Evaluate(
                    bits,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    ),
                    module is not null
                );
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
                    if (!await CompiledBindingScope.MatchAsync(
                        m.Pattern,
                        value,
                        b,
                        ctx
                    ))
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
                    var scope = CompiledBindingScope.Copy(b);
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
            case Expr.Try tried:
                return await TryExpressionExecution.Evaluate(
                    tried,
                    ctx.Runtime,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    ),
                    async (clause, value, scope) => clause.Patterns.Count == 1 && await clause.Patterns[0].MatchAsync(
                        value,
                        scope,
                        ctx,
                        scope
                    ) && await GuardAsync(clause.Guard, scope, ctx)
                );
            case Expr.Maybe conditional:
                return await MaybeExpressionExecution.Evaluate(
                    conditional,
                    b,
                    (expression, scope) => Evaluate(
                        expression,
                        ctx,
                        scope,
                        module
                    ),
                    (pattern, value, scope) => CompiledBindingScope.MatchAsync(
                        pattern,
                        value,
                        scope,
                        ctx
                    ),
                    async (clause, value, scope) => clause.Patterns.Count == 1 && await clause.Patterns[0].MatchAsync(
                        value,
                        scope,
                        ctx,
                        scope
                    ) && await GuardAsync(clause.Guard, scope, ctx)
                );
            case Expr.Unary u:
                return Unary(u.Operator, await Evaluate(
                    u.Operand,
                    ctx,
                    b,
                    module
                ), ctx);
            case Expr.Binary x:
                {
                    bool shortCircuit = x.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse;
                    var leftScope = shortCircuit ? b : CompiledBindingScope.Copy(b);
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
                    var rightScope = CompiledBindingScope.Copy(b);
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

                    if (x.Operator == ErlangOperators.Send)
                    {
                        await ErlangOperatorValues.TrySendAsync(ctx, left, right);

                        return right;
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
                    string? targetModule = x.Module ?? (module?.Functions.Any(f => f.Name == x.Function && f.Arity == x.Arguments.Count) == true ? null : SourceCallResolution.ExternalModule(module, x.Function, x.Arguments.Count));
                    if (targetModule == ErlangModuleNames.Module && RecordPredicateSyntax.HasLiteralNonpositiveSize(x))
                        return Term.A(ErlangBooleanAtoms.False);
                    var args = await Arguments(x.Arguments);
                    if (x.Module == ErlangModuleNames.Module && x.Function == SourceLoadNifNames.Function
                        && args.Length == SourceLoadNifNames.Arity)
                        return await CallSourceLoadNif(ctx, args);
                    if (module is not null && x.Module is null && module.Functions.Any(f => f.Name == x.Function && f.Arity == args.Length))
                    {
                        if (tail)
                            throw new TailCall(x.Function, args, ctx.ExecutingSourceImage(module));

                        return await InvokeAsync(
                            module,
                            x.Function,
                            ctx,
                            args
                        );
                    }

                    if (tail && targetModule is not null &&
                        ctx.Runtime.Modules.SourceEntry(targetModule ?? ErlangModuleNames.Module, x.Function, args.Length) is { Owner: ModuleDefinition source } entry)
                        throw new SourceTailCall(
                            source,
                            x.Function,
                            args,
                            entry.Image
                        );

                    if (tail && (targetModule is not null && targetModule != ErlangModuleNames.Module || targetModule == ErlangModuleNames.Module && ErlangSourceWrapperSignatures.Contains(x.Function, args.Length)))
                        throw new RuntimeTailCall(targetModule ?? ErlangModuleNames.Module, x.Function, args);

                    ModuleCallDispatchKind dispatch = ModuleCallDispatchKind.Direct;
                    try
                    {
                        if (targetModule == ErlangModuleNames.Module && RecordPredicateSyntax.TupleTest(x) is { } predicate)
                            return RecordPredicateSyntax.Test(predicate.StaticApply ? Cons.Items(args[2]).First() : args[0], predicate);
                        if (targetModule is null)
                            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

                        if (x.Module == ErlangModuleNames.Module && x.Function == CompilerLoaderNames.CallOnLoad && args.Length == 1)
                            return await ctx.Runtime.Modules.InvokeLoaderOnLoad(ctx, args[0]);
                        if (CompilerLoaderSyntax.IsStaticApply(x))
                            return await ctx.Runtime.Modules.InvokeLoaderOnLoad(ctx, ((Cons)args[CompilerLoaderSyntaxArity.ArgumentList]).Head);

                        return await ctx.Runtime.Modules.CallWithDispatch(
                            ctx,
                            targetModule ?? ErlangModuleNames.Module,
                            x.Function,
                            args,
                            out dispatch
                        );
                    }
                    catch (ErlangException exception)
                    {
                        if (dispatch == ModuleCallDispatchKind.UndefinedFunctionHandler)
                            throw;
                        throw RuntimeCallFrames.Capture(
                            ctx,
                            exception,
                            targetModule ?? ErlangModuleNames.Module,
                            x.Function,
                            args
                        );
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

                    if (fun is not FunctionTerm f)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), fun));

                    return await f.Invoke(ctx, args);
                }
            case Expr.Case x:
                {
                    var value = await Evaluate(
                        x.Value,
                        ctx,
                        b,
                        module
                    );

                    return await Branch(await Select(
                        x.Clauses,
                        [value],
                        b,
                        ctx
                    ) ?? throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value)));
                }
            case Expr.If x:
                return await Branch(await Select(
                    x.Clauses,
                    [],
                    b,
                    ctx
                ) ?? throw new ErlangException(ErlangErrorReasons.IfClause));
            case Expr.Receive x:
                {
                    Term? timeout = x.Timeout is null ? null : await Evaluate(
                        x.Timeout,
                        ctx,
                        b,
                        module
                    );

                    ValueTask<Selection?> SelectMessage(Term message) => Select(
                        x.Clauses,
                        [message],
                        b,
                        ctx
                    );
                    var selection = timeout is null
                        ? await ctx.ReceiveSelectingAsync(SelectMessage)
                        : await ctx.ReceiveSelectingAsync(SelectMessage, timeout);
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
                    var capture = ClosureCaptures.Capture(f, b);
                    var identity = LocalFunctionEntries.Get(module, f, ctx);
                    var image = module is null ? null : ctx.ExecutingSourceImage(module) ?? ctx.Runtime.Modules.SourceImage(module.Name, module);
                    string[] names = capture.Keys.ToArray();

                    FunctionTerm Create(IReadOnlyList<Term> environment, LocalFunctionInvocation? invocation = null)
                    {
                        if (environment.Count != names.Length)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var restored = names.Select((name, index) => new KeyValuePair<string, Term>(name, environment[index])).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

                        FunctionTerm? result = null;
                        result = new FunctionTerm(
                            identity,
                            f.Clauses[0].Patterns.Count,
                            environment,
                            (context, args) => SourceFunctionInvocation.Anonymous(
                                module,
                                identity,
                                f,
                                restored,
                                (ProcessContext)context,
                                image,
                                args,
                                invocation?.OriginalFunction ?? result!,
                                invocation
                            )
                        );

                        return result;
                    }
                    identity = ctx.Runtime.Functions.RegisterSourceFactory(
                        identity,
                        f.Clauses[0].Patterns.Count,
                        names.Length,
                        Create
                    );

                    return Create(capture.Values.ToArray());
                }
            case Expr.FunctionReference reference:
                return NamedFunctionReferences.Evaluate(module, reference, ctx);
            default:
                throw new NotSupportedException();
        }
    }

    private static Term Unary(string op, Term value, ProcessContext? context = null)
    {
        try
        {
            return UnaryValue(op, value);
        }
        catch (ErlangException exception)
        {
            throw context is null ? exception.WithStackFrame(ErlangModuleNames.Module, op, [value])
                : context.CaptureNativeFailure(exception, ErlangModuleNames.Module, op, [value]);
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
            throw context is null ? exception.WithStackFrame(ErlangModuleNames.Module, op, [a, b])
                : context.CaptureNativeFailure(exception, ErlangModuleNames.Module, op, [a, b]);
        }
    }

    private static Term UnaryValue(string op, Term value) => ErlangOperatorValues.Unary(op, value);

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
        Term first,
        Term second,
        ProcessContext? context
    ) => ErlangOperatorValues.Binary(
        op,
        first,
        second,
        context
    );
}
