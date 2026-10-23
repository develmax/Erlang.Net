namespace Erlang.Otp;

internal static class ProcLibOptionStartup
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 2, 4 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibOptionNames.SpawnOpt,
                arity,
                Spawn
            );
        registry.Register(
            ProcLibNames.Module,
            ProcLibStartupNames.Start,
            5,
            (context, arguments) => Start(
                context,
                arguments,
                false,
                false
            )
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibStartupNames.StartLink,
            5,
            (context, arguments) => Start(
                context,
                arguments,
                true,
                false
            )
        );
        foreach (int arity in new[] { 3, 4, 5 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibOptionNames.StartMonitor,
                arity,
                (context, arguments) => Start(
                    context,
                    arguments,
                    false,
                    true
                )
            );
    }

    private static async ValueTask<Term> Spawn(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var initialization = await ProcLib.PrepareSpawn(context, arguments.Take(arguments.Count - 1).ToArray(), false);

        return SpawnPrepared(context, initialization.Body, initialization.Description with { Options = arguments[^1] });
    }

    private static Term SpawnPrepared(
        ProcessContext context,
        Func<ProcessContext, ValueTask<Term>> body,
        SequenceSpawnDescription description
    )
    {
        Term[] builtinArguments = [ProcLibAtoms.Module, ProcLibAtoms.Initializer, description.Arguments, description.Options];
        ProcessSpawnOptions options = ProcessSpawnOptionRegistrations.ParseOptions(context, builtinArguments);

        return context.Runtime.SpawnWithOptions(
            context,
            body,
            options,
            description
        );
    }

    private static async ValueTask<Term> Start(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        bool link,
        bool keepMonitor
    )
    {
        if (arguments[0] is not Atom || arguments[1] is not Atom || arguments[2] is not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        Term options = Nil.Value;
        if (arguments.Count == 5)
        {
            options = arguments[4];
            await VerifyNoMonitor(context, arguments, options);
        }
        options = new Cons(ProcLibStartupAtoms.Monitor, options);
        if (link)
            options = new Cons(ProcLibStartupAtoms.Link, options);
        var initialization = await ProcLib.PrepareSpawn(context, arguments.Take(3).ToArray(), link);
        var spawned = (TupleTerm)SpawnPrepared(context, initialization.Body, initialization.Description with { Options = options });
        var child = (Pid)spawned.Items[0];
        var reference = (ReferenceTerm)spawned.Items[1];
        Term timeout = arguments.Count >= 4 ? arguments[3] : ProcLibStartupAtoms.Infinity;

        return keepMonitor
            ? await ProcLibStartup.WaitMonitoredInitialization(
                context,
                child,
                reference,
                timeout
            )
            : await ProcLibStartup.WaitInitialization(
                context,
                child,
                reference,
                timeout
            );
    }

    private static async ValueTask VerifyNoMonitor(ProcessContext context, IReadOnlyList<Term> arguments, Term options)
    {
        Term member = await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.Member,
            ProcLibStartupAtoms.Monitor,
            options
        );
        if (member.Equals(Term.A(ErlangBooleanAtoms.True)))
        {
            await RejectMonitor(context, arguments);

            return;
        }
        Term found = await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.KeyFind,
            ProcLibStartupAtoms.Monitor,
            Term.I(1),
            options
        );
        if (found.Equals(Term.A(ErlangBooleanAtoms.False)))
            return;
        if (found is TupleTerm { Items: [_, _] })
        {
            await RejectMonitor(context, arguments);

            return;
        }
        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), found));
    }

    private static async ValueTask RejectMonitor(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibStartupNames.Error,
            Term.A(ErlangErrorReasons.BadArgument),
            Cons.From(arguments)
        );
    }
}
