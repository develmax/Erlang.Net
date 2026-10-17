namespace Erlang.Tests;

internal static class SchedulerPublicApiHostChecks
{
    internal static async Task InvalidValuesPreserveBindingAndOnlineCount()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        var actor = runtime.Spawn(async context =>
        {
            await SchedulerControlCalls.Bind(context, SchedulerAffinityHostNames.First);
            Term[] invalid = [Term.I(SchedulerPublicApiNames.InvalidNegative), Term.I(SchedulerPublicApiNames.InvalidAboveConfigured),
                new FloatTerm(SchedulerAffinityHostNames.First), Term.A(SchedulerPublicApiNames.InvalidAtom), Nil.Value,
                Term.Tuple(), new Integer(System.Numerics.BigInteger.One << SchedulerPublicApiNames.BigIntegerShift)];
            foreach (Term value in invalid)
                foreach (string function in new[] { SchedulerAffinityHostNames.Flag, SchedulerAffinityHostNames.SystemFlag })
                {
                    bool rejected = false;
                    try
                    {
                        await runtime.Modules.Call(
                            context,
                            SchedulerAffinityHostNames.Erlang,
                            function,
                            Term.A(function == SchedulerAffinityHostNames.Flag ? SchedulerAffinityHostNames.Scheduler : SchedulerAffinityHostNames.Online),
                            value
                        );
                    }
                    catch (ErlangException error) when (error.Reason.Equals(Term.A(SchedulerPublicApiNames.BadArgument)))
                    {
                        rejected = true;
                    }
                    SchedulerControlCalls.Require(rejected);
                    await RequireIdentity(context, Term.I(SchedulerAffinityHostNames.First));
                    SchedulerControlCalls.Require((await runtime.Modules.Call(
                        context,
                        SchedulerAffinityHostNames.Erlang,
                        SchedulerAffinityHostNames.Info,
                        Term.A(SchedulerAffinityHostNames.Online)
                    ))
                        .Equals(Term.I(SchedulerAffinityHostNames.Capacity)));
                    SchedulerControlCalls.Require((await SchedulerControlCalls.Bind(context, SchedulerAffinityHostNames.First))
                        .Equals(Term.I(SchedulerAffinityHostNames.First)));
                }

            return Nil.Value;
        });
        await SchedulerControlCalls.Normal(actor);
    }

    internal static async Task QueriesAndAllLocalSpawnOptionForms()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        runtime.Modules.Register(
            SchedulerPublicApiNames.Module,
            SchedulerPublicApiNames.Child,
            1,
            async (context, arguments) =>
        {
            await RequireIdentity(context, arguments[0]);

            return Nil.Value;
        }
        );
        var actor = runtime.Spawn(async context =>
        {
            foreach (string query in new[] { SchedulerPublicApiNames.Schedulers, SchedulerPublicApiNames.Total,
                SchedulerAffinityHostNames.Online })
                SchedulerControlCalls.Require((await runtime.Modules.Call(
                    context,
                    SchedulerAffinityHostNames.Erlang,
                    SchedulerAffinityHostNames.Info,
                    Term.A(query)
                )).Equals(Term.I(SchedulerAffinityHostNames.Capacity)));
            var callback = new FunctionTerm(
                0,
                async (executionContext, _) =>
            {
                var childContext = (ProcessContext)executionContext;
                await RequireIdentity(childContext, Term.I(SchedulerAffinityHostNames.Second));

                return Nil.Value;
            }
            );
            Term options = Term.List(
                Term.A(SchedulerAffinityHostNames.Monitor),
                Term.Tuple(Term.A(SchedulerAffinityHostNames.Scheduler), Term.I(SchedulerAffinityHostNames.Second))
            );
            Term module = Term.A(SchedulerPublicApiNames.Module);
            Term function = Term.A(SchedulerPublicApiNames.Child);
            Term arguments = Term.List(Term.I(SchedulerAffinityHostNames.Second));
            Term node = Term.A(runtime.Node);
            Term[][] inputs = [
                [callback, options],
                [node, callback, options],
                [module, function, arguments, options],
                [node, module, function, arguments, options]
            ];
            foreach (Term[] input in inputs)
            {
                var spawned = (TupleTerm)await runtime.Modules.Call(
                    context,
                    SchedulerAffinityHostNames.Erlang,
                    SchedulerAffinityHostNames.Spawn,
                    input
                );
                await RequireDown(context, spawned.Items[SchedulerPublicApiNames.ReferenceIndex]);
            }
            foreach (Term[] input in inputs)
            {
                Term reference = await runtime.Modules.Call(
                    context,
                    SchedulerAffinityHostNames.Erlang,
                    SchedulerPublicApiNames.SpawnRequest,
                    input
                );
                Term? reply = await context.ReceiveAsync(
                    value => value is TupleTerm tuple &&
                    tuple.Items.Count == SchedulerPublicApiNames.ReplyArity &&
                    tuple.Items[0].Equals(Term.A(SchedulerPublicApiNames.Reply)) &&
                    tuple.Items[SchedulerPublicApiNames.ReferenceIndex].Equals(reference)
                    ? value : null,
                    SchedulerControlCalls.Deadline
                );
                SchedulerControlCalls.Require(reply is TupleTerm message &&
                    message.Items[SchedulerPublicApiNames.ReplyStatusIndex].Equals(Term.A(SchedulerPublicApiNames.Success)));
                await RequireDown(context, reference);
            }

            return Nil.Value;
        });
        await SchedulerControlCalls.Normal(actor);
    }

    private static async Task RequireIdentity(ProcessContext context, Term expected)
    {
        Term actual = await context.Runtime.Modules.Call(
            context,
            SchedulerAffinityHostNames.Erlang,
            SchedulerAffinityHostNames.Info,
            Term.A(SchedulerAffinityHostNames.Identity)
        );
        SchedulerControlCalls.Require(actual.Equals(expected));
    }

    private static async Task RequireDown(ProcessContext context, Term reference)
    {
        Term? down = await context.ReceiveAsync(
            value => value is TupleTerm tuple &&
            tuple.Items.Count == SchedulerPublicApiNames.DownArity &&
            tuple.Items[0].Equals(Term.A(SchedulerAffinityHostNames.Down)) &&
            tuple.Items[SchedulerPublicApiNames.ReferenceIndex].Equals(reference)
            ? value : null,
            SchedulerControlCalls.Deadline
        );
        SchedulerControlCalls.Require(down is TupleTerm message &&
            message.Items[SchedulerPublicApiNames.DownReasonIndex].Equals(Term.A(SchedulerAffinityHostNames.Normal)));
    }
}
