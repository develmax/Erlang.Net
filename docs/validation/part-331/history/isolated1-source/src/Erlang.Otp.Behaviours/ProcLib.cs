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
    }

    private static async ValueTask<Term> Spawn(ProcessContext context, IReadOnlyList<Term> arguments, bool link)
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

        async ValueTask<Term> Body(ProcessContext child)
        {
            using var frame = child.EnterFunction(ProcLibNames.Module, ProcLibNames.Initializer, callback is null ? 5 : 3);
            child.Dictionary[ProcLibAtoms.Ancestors] = new Cons(parent, ancestors);
            Term[] values = callback is null ? Cons.Items(arguments[2]).ToArray() : [];
            child.Dictionary[ProcLibAtoms.InitialCall] = callback is not null
                ? await child.Runtime.Modules.Call(
                    child,
                    ProcLibNames.Erlang,
                    ProcLibNames.FunInfoMfa,
                    callback
                )
                : TranslateInitialCall(module!, function!, values);

            return callback is not null
                ? await callback.Invoke(child, [])
                : await child.Runtime.Modules.Call(
                    child,
                    module!.Name,
                    function!.Name,
                    values
                );
        }

        return context.Runtime.SpawnTraced(
            Body,
            context,
            link,
            SequenceSpawnDescription.Module(
                ProcLibAtoms.Module,
                ProcLibAtoms.Initializer,
                wrapperArguments,
                SequenceSpawnDescription.DefaultOptions(link ? ProcLibNames.SpawnLink : ProcLibNames.Spawn)
            )
        ).Pid;
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
            Term ancestors = context.Dictionary.GetValueOrDefault(ProcLibAtoms.Ancestors, ProcessInfoAtoms.Undefined);
            if (ancestors is not Cons head)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), ancestors));
            parent = head.Head;
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

    private static Term TranslateInitialCall(Atom module, Atom function, Term[] arguments)
    {
        if (module.Name == ProcLibNames.Gen && function.Name == ProcLibNames.InitIt)
        {
            int behaviourIndex = arguments.Length == 6 ? 3 : 4;
            if (arguments.Length is 6 or 7 && arguments[0] is Atom { Name: ProcLibNames.GenServer })
            {
                if (arguments[behaviourIndex] is Atom { Name: ProcLibNames.Supervisor } && arguments[behaviourIndex + 1] is TupleTerm { Items: [_, var supervisor, _] })
                    return Term.Tuple(Term.A(ProcLibNames.Supervisor), supervisor, Term.I(1));
                if (arguments[behaviourIndex] is Atom { Name: ProcLibNames.SupervisorBridge } && arguments[behaviourIndex + 1] is Cons { Head: var bridge })
                    return Term.Tuple(Term.A(ProcLibNames.SupervisorBridge), bridge, Term.I(1));
            }
            if (arguments.Length > 0 && arguments[0] is Atom { Name: ProcLibNames.GenEvent })
                return Term.Tuple(Term.A(ProcLibNames.GenEvent), Term.A(ProcLibNames.InitIt), Term.I(6));
            if (arguments.Length == 6 && arguments[3] is Atom sixModule)
                return Term.Tuple(sixModule, Term.A(ProcLibNames.Init), Term.I(1));
            if (arguments.Length >= 5 && arguments[4] is Atom otherModule)
                return Term.Tuple(otherModule, Term.A(ProcLibNames.Init), Term.I(1));
        }

        return Term.Tuple(module, function, Term.I(arguments.Length));
    }
}
