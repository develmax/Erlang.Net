namespace Erlang.Otp;

internal static class ProcLibLifecycle
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 3, 5 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibNames.Initializer,
                arity,
                Initialize
            );
        registry.Register(
            ProcLibNames.Module,
            ProcLibLifecycleNames.WakeUp,
            3,
            WakeUp
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibLifecycleNames.Hibernate,
            3,
            Hibernate
        );
    }

    internal static async ValueTask<Term> Initialize(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        FunctionTerm? callback = null;
        if (arguments.Count == 3)
            callback = arguments[2] as FunctionTerm ?? throw new ErlangException(ErlangErrorReasons.FunctionClause);
        else
            RequireMfa(arguments.Skip(2).ToArray());
        context.Dictionary[ProcLibAtoms.Ancestors] = new Cons(arguments[0], arguments[1]);
        context.Dictionary[ProcLibAtoms.InitialCall] = callback is not null
            ? await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcLibNames.FunInfoMfa,
                callback
            )
            : ProcLibInitialCallTranslation.Translate(
                (Atom)arguments[2],
                (Atom)arguments[3],
                arguments[4]
            );

        return await Execute(
            context,
            callback,
            arguments.Skip(2).ToArray(),
            callback is not null ? ProcLibNames.Initializer : ProcLibLifecycleNames.InitializeApply,
            callback is not null ? arguments : arguments.Skip(2).ToArray()
        );
    }

    private static ValueTask<Term> WakeUp(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        RequireMfa(arguments);

        return Execute(
            context,
            null,
            arguments,
            ProcLibLifecycleNames.WakeUp,
            arguments
        );
    }

    private static ValueTask<Term> Hibernate(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        RequireMfa(arguments);

        return context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibLifecycleNames.Hibernate,
            ProcLibAtoms.Module,
            ProcLibLifecycleAtoms.WakeUp,
            Cons.From(arguments)
        );
    }

    private static void RequireMfa(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom || arguments[1] is not Atom || arguments[2] is not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    private static async ValueTask<Term> Execute(
        ProcessContext context,
        FunctionTerm? callback,
        IReadOnlyList<Term> arguments,
        string function,
        IReadOnlyList<Term> frameArguments
    )
    {
        using var frame = context.EnterFunction(ProcLibNames.Module, function, frameArguments.Count);
        try
        {
            return callback is not null
                ? await callback.Invoke(context, [])
                : await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibLifecycleNames.Apply,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                );
        }
        catch (ErlangException error)
        {
            Term trace = error.ExplicitStackTrace || error.AutomaticStackTraceCaptured
                ? error.StackTraceTerm
                : Cons.From(Cons.Items(error.StackTraceTerm).Concat(
                    Cons.Items(context.CaptureErrorStack(Term.I(frameArguments.Count), null))
                ));
            Term initial = context.Dictionary.GetValueOrDefault(ProcLibAtoms.InitialCall, ProcessInfoAtoms.Undefined);
            Term start = Nil.Value;
            if (initial is TupleTerm { Items: [Atom module, Atom initialFunction, Integer arity] })
                start = Term.Tuple(module, initialFunction, await ProcLibIntrospection.MakeDummyArguments(context, arity));
            await ProcLibCrashReportLogging.Report(
                context,
                error.ExceptionClass,
                error.Reason,
                start,
                trace
            );
            Term reason = error.ExceptionClass switch
            {
                ErlangExceptionClasses.Error => Term.Tuple(error.Reason, trace),
                ErlangExceptionClasses.Throw => Term.Tuple(
                    Term.Tuple(ProcLibLifecycleAtoms.NoCatch, error.Reason),
                    trace
                ),
                _ => error.Reason
            };

            throw new ErlangException(reason, ErlangExceptionClasses.Exit)
            {
                StackTraceTerm = trace,
                ExplicitStackTrace = true
            };
        }
    }
}
