namespace Erlang.Otp;

internal static class SysTermination
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 2, 3 })
            registry.Register(
                SysTerminationNames.Module,
                SysTerminationNames.Terminate,
                arity,
                Terminate
            );
    }

    private static async ValueTask<Term> Terminate(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term timeout = arguments.Count == 2 ? Term.I(SysTerminationParameters.DefaultTimeoutMilliseconds) : arguments[2];
        try
        {
            if (!timeout.Equals(ProcLibStartupAtoms.Infinity) && timeout is not Integer { Value.Sign: >= 0 })
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            Term target = await Resolve(context, arguments[0]);

            try
            {
                return await Request(context, target, arguments[1], timeout);
            }
            catch (ErlangException error) when (
                error.ExceptionClass == ErlangExceptionClasses.Exit &&
                arguments[0] is TupleTerm registry &&
                (registry.Items.Count == 2 && registry.Items[0].Equals(SysTerminationAtoms.Global) ||
                 registry.Items.Count == 3 && registry.Items[0].Equals(SysTerminationAtoms.Via)) &&
                target is Pid process &&
                error.Reason.Equals(Term.Tuple(SysTerminationAtoms.NodeDown, Term.A(process.Node)))
            )
            {
                context.Exit(ProcLibStopAtoms.NoProcess);

                return Nil.Value;
            }
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
        {
            throw new ErlangException(Term.Tuple(
                error.Reason,
                Term.Tuple(SysTerminationAtoms.Module, SysTerminationAtoms.Terminate, Cons.From(arguments))
            ), ErlangExceptionClasses.Exit);
        }
    }

    private static async ValueTask<Term> Resolve(ProcessContext context, Term target)
    {
        if (target is Pid)
            return target;
        if (target is Atom)
        {
            Term process = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                SysTerminationNames.WhereIs,
                target
            );
            if (process is Pid)
                return process;
            if (process.Equals(ProcessInfoAtoms.Undefined))
                context.Exit(ProcLibStopAtoms.NoProcess);

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), process));
        }
        if (target is TupleTerm { Items: [Atom { Name: SysTerminationNames.Global }, var globalName] })
            return await ResolveRegistry(context, SysTerminationNames.Global, globalName);
        if (target is TupleTerm { Items: [Atom { Name: SysTerminationNames.Via }, var registry, var viaName] })
            return await ResolveRegistry(context, CoreModules.AtomName(registry), viaName);
        if (target is TupleTerm { Items: [var name, Atom node] })
        {
            if (node.Name == context.Runtime.Node)
                return await Resolve(context, name);
            if (context.Runtime.Node == SysTerminationNames.UndistributedNode)
                context.Exit(Term.Tuple(SysTerminationAtoms.NodeDown, node));

            return target;
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    private static async ValueTask<Term> ResolveRegistry(ProcessContext context, string module, Term name)
    {
        Term target = await context.Runtime.Modules.Call(
            context,
            module,
            SysTerminationNames.WhereIsName,
            name
        );
        if (target is Pid)
            return target;
        if (target.Equals(ProcessInfoAtoms.Undefined))
            context.Exit(ProcLibStopAtoms.NoProcess);

        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), target));
    }

    private static async ValueTask<Term> Request(
        ProcessContext context,
        Term target,
        Term reason,
        Term timeout
    )
    {
        if (target.Equals(context.Self))
            context.Exit(SysTerminationAtoms.CallingSelf);
        bool plainMonitor = timeout.Equals(ProcLibStartupAtoms.Infinity) &&
            (target is Pid local && local.Node == context.Runtime.Node ||
             target is TupleTerm { Items: [Atom, Atom node] } && node.Name == context.Runtime.Node);
        Term[] monitorArguments = plainMonitor
            ? [ProcLibStartupAtoms.Process, target]
            : [ProcLibStartupAtoms.Process, target, SysTerminationAtoms.AliasOptions];
        var reference = (ReferenceTerm)await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErlangModuleNames.Monitor,
            monitorArguments
        );
        Term tag = plainMonitor ? reference : new Cons(SysTerminationAtoms.Alias, reference);
        Term message = Term.Tuple(
            SysTerminationAtoms.System,
            Term.Tuple(context.Self, tag),
            Term.Tuple(SysTerminationAtoms.Terminate, reason)
        );
        Term[] sendArguments = plainMonitor ? [target, message] : [target, message, SysTerminationAtoms.SendOptions];
        await context.Runtime.Modules.Call(context, ProcLibNames.Erlang, ProcLibNames.Send, sendArguments);
        TupleTerm? response = await context.ReceiveErlangAsync(
            value => IsReply(value, tag) || IsDown(value, reference) ? (TupleTerm)value : null,
            timeout
        );
        if (response is null)
        {
            context.Runtime.Demonitor(context, reference, flush: true);
            response = await context.ReceiveAsync(value => IsReply(value, tag) ? (TupleTerm)value : null, TimeSpan.Zero);
            if (response is null)
                context.Exit(ProcLibStopAtoms.Timeout);
        }
        if (IsReply(response!, tag))
        {
            context.Runtime.Demonitor(context, reference, flush: true);

            return response!.Items[1];
        }
        Term exitReason = response!.Items[^1];
        if (exitReason.Equals(SysTerminationAtoms.Disconnected))
        {
            Term node = target is TupleTerm remote ? remote.Items[1] : Term.A(((Pid)target).Node);
            exitReason = Term.Tuple(SysTerminationAtoms.NodeDown, node);
        }
        context.Exit(exitReason);

        return Nil.Value;
    }

    private static bool IsReply(Term value, Term tag)
        => value is TupleTerm { Items: [var candidate, _] } && candidate.Equals(tag);

    private static bool IsDown(Term value, ReferenceTerm reference)
        => value is TupleTerm { Items: [var tag, var candidate, _, _, _] } &&
            tag.Equals(ProcLibStartupAtoms.Down) && candidate.Equals(reference);
}
