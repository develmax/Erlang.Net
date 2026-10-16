namespace Erlang.Otp;

internal static class ProcLibStartup
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (bool link in new[] { false, true })
        {
            string function = link ? ProcLibStartupNames.StartLink : ProcLibStartupNames.Start;
            foreach (int arity in new[] { 3, 4 })
                registry.Register(
                    ProcLibNames.Module,
                    function,
                    arity,
                    (context, arguments) => Start(context, arguments, link)
                );
        }
        foreach (int arity in new[] { 2, 3 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibStartupNames.InitFail,
                arity,
                Fail
            );
    }

    private static async ValueTask<Term> Start(ProcessContext context, IReadOnlyList<Term> arguments, bool link)
    {
        var initialization = await ProcLib.PrepareSpawn(context, arguments.Take(3).ToArray(), link);
        Term options = link ? Term.List(ProcLibStartupAtoms.Link, ProcLibStartupAtoms.Monitor) : Term.List(ProcLibStartupAtoms.Monitor);
        var spawned = context.Runtime.SpawnMonitorTraced(
            context,
            initialization.Body,
            link,
            initialization.Description with { Options = options }
        );
        return await WaitInitialization(
            context,
            spawned.Process.Pid,
            spawned.Reference,
            arguments.Count == 4 ? arguments[3] : ProcLibStartupAtoms.Infinity
        );
    }

    internal static async ValueTask<Term> WaitInitialization(
        ProcessContext context,
        Pid child,
        ReferenceTerm reference,
        Term timeoutValue
    )
    {
        TupleTerm? response = await context.ReceiveErlangAsync(
            value =>
            value is TupleTerm { Items: [var tag, var sender, _] } tuple &&
                (tag.Equals(ProcLibAtoms.Ack) || tag.Equals(ProcLibStartupAtoms.Nack)) && sender.Equals(child) ? tuple :
            IsDown(value, child, reference) ? (TupleTerm)value : null,
            timeoutValue
        );
        if (response is null)
        {
            context.Runtime.Unlink(context, child);
            context.Runtime.Exit(context, child, ProcLibStartupAtoms.Kill);
            await FlushLinkedExit(context, child);
            await AwaitDown(context, child, reference);

            return Term.Tuple(ProcLibStartupAtoms.Error, ProcLibStartupAtoms.Timeout);
        }
        if (response.Items[0].Equals(ProcLibAtoms.Ack))
        {
            context.Runtime.Demonitor(context, reference, flush: true);

            return response.Items[2];
        }
        context.Runtime.Unlink(context, child);
        await FlushLinkedExit(context, child);
        if (response.Items.Count == StartupHandshakeLayout.DownFields)
            return Term.Tuple(ProcLibStartupAtoms.Error, response.Items[StartupHandshakeLayout.DownReasonIndex]);
        await AwaitDown(context, child, reference);

        return response.Items[2];
    }

    private static bool IsDown(Term value, Pid child, ReferenceTerm reference) => value is TupleTerm
    {
        Items: [var tag, var monitor, var kind, var target, _]
    } && tag.Equals(ProcLibStartupAtoms.Down) && monitor.Equals(reference) && kind.Equals(ProcLibStartupAtoms.Process) && target.Equals(child);

    private static async ValueTask FlushLinkedExit(ProcessContext context, Pid child)
    {
        await context.ReceiveAsync(
            value => value is TupleTerm { Items: [var tag, var sender, _] } tuple && tag.Equals(ProcLibStartupAtoms.LinkedExit) && sender.Equals(child) ? tuple : null,
            TimeSpan.Zero
        );
    }

    private static async ValueTask<TupleTerm> AwaitDown(ProcessContext context, Pid child, ReferenceTerm reference)
    {
        var down = await context.ReceiveAsync(value => IsDown(value, child, reference) ? (TupleTerm)value : null);

        return down!;
    }

    private static async ValueTask<Term> Fail(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term parent = arguments.Count == 3 ? arguments[0] : ProcLib.Parent(context);
        Term result = arguments[arguments.Count - 2];
        Term exception = arguments[arguments.Count - 1];
        await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibNames.Send,
            parent,
            Term.Tuple(ProcLibStartupAtoms.Nack, context.Self, result)
        );
        switch (exception)
        {
            case TupleTerm { Items: [Atom { Name: ProcLibStartupNames.Error }, var reason] }:
                return await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibStartupNames.Error,
                    reason
                );

            case TupleTerm { Items: [Atom { Name: ProcLibStartupNames.Exit }, var reason] }:
                return await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibStartupNames.Exit,
                    reason
                );

            case TupleTerm { Items: [Atom { Name: ProcLibStartupNames.Throw }, var reason] }:
                return await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibStartupNames.Throw,
                    reason
                );

            case TupleTerm { Items: [var kind, var reason, var stack] }:
                Term raised = await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibStartupNames.Raise,
                    kind,
                    reason,
                    stack
                );

                return await context.Runtime.Modules.Call(
                    context,
                    ProcLibNames.Erlang,
                    ProcLibStartupNames.Error,
                    raised,
                    Term.List(parent, result, exception)
                );

            default:
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), exception));
        }
    }
}
