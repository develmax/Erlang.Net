namespace Erlang.Otp;

internal static class ProcLib
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (bool link in new[] { false, true })
        {
            string function = link ? ProcLibNames.SpawnLink : ProcLibNames.Spawn;
            registry.Register(
                ProcLibNames.Module,
                function,
                1,
                (context, arguments) => Spawn(context, arguments, link)
            );
            registry.Register(
                ProcLibNames.Module,
                function,
                3,
                (context, arguments) => Spawn(context, arguments, link)
            );
        }
        registry.Register(
            ProcLibNames.Module,
            ProcLibNames.InitAck,
            1,
            Acknowledge
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibNames.InitAck,
            2,
            Acknowledge
        );
        ProcLibStartup.Register(registry);
        ProcLibOptionStartup.Register(registry);
        ProcLibIntrospection.Register(registry);
        ProcLibStop.Register(registry);
        SysTermination.Register(registry);
        ProcLibReportFormattingRegistrations.Register(registry);
        ProcLibLifecycle.Register(registry);
    }

    private static async ValueTask<Term> Spawn(ProcessContext context, IReadOnlyList<Term> arguments, bool link)
    {
        var initialization = await PrepareSpawn(context, arguments, link);

        return context.Runtime.SpawnTraced(
            initialization.Body,
            context,
            link,
            initialization.Description
        ).Pid;
    }

    internal static async ValueTask<(Func<ProcessContext, ValueTask<Term>> Body, SequenceSpawnDescription Description)> PrepareSpawn(ProcessContext context, IReadOnlyList<Term> arguments, bool link)
    {
        FunctionTerm? callback = null;
        Atom? module = null;
        Atom? function = null;
        if (arguments.Count == 1)
        {
            callback = arguments[0] as FunctionTerm ?? throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        else
        {
            if (arguments[0] is not Atom requestedModule || arguments[1] is not Atom requestedFunction || arguments[2] is not Cons and not Nil)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            module = requestedModule;
            function = requestedFunction;
        }
        Term parent = context.Self;
        Term registered = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibNames.ProcessInfo,
            context.Self,
            Term.A(ProcLibNames.RegisteredName)
        );
        if (registered is TupleTerm { Items: [_, Atom name] })
            parent = name;
        Term inherited = context.Dictionary.GetValueOrDefault(ProcLibAtoms.Ancestors, Nil.Value);
        Term ancestors = inherited is Cons or Nil ? inherited : Nil.Value;
        Term wrapperArguments = callback is not null ? Term.List(parent, ancestors, callback) : Term.List(
            parent,
            ancestors,
            module!,
            function!,
            arguments[2]
        );

        ValueTask<Term> Body(ProcessContext child)
            => ProcLibLifecycle.Initialize(child, Cons.Items(wrapperArguments).ToArray());

        return (
            Body,
            SequenceSpawnDescription.Module(
                ProcLibAtoms.Module,
                ProcLibAtoms.Initializer,
                wrapperArguments,
                SequenceSpawnDescription.DefaultOptions(link ? ProcLibNames.SpawnLink : ProcLibNames.Spawn)
            )
        );
    }

    internal static Term Parent(ProcessContext context)
    {
        Term ancestors = context.Dictionary.GetValueOrDefault(ProcLibAtoms.Ancestors, ProcessInfoAtoms.Undefined);
        if (ancestors is not Cons head)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), ancestors));

        return head.Head;
    }

    private static async ValueTask<Term> Acknowledge(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term parent;
        Term result;
        if (arguments.Count == 2)
        {
            parent = arguments[0];
            result = arguments[1];
        }
        else
        {
            parent = Parent(context);
            result = arguments[0];
        }
        await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibNames.Send,
            parent,
            Term.Tuple(ProcLibAtoms.Ack, context.Self, result)
        );

        return ProcLibAtoms.Ok;
    }

}
