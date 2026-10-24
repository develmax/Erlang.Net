namespace Erlang.Tests;

internal static class ProcessInfoDefaultSubsetHostChecks
{
    internal static async Task Run()
    {
        await using var first = new ProcessRuntime(1);
        await using var second = new ProcessRuntime(1);
        var finishForeign = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var foreign = second.Spawn(async _ =>
        {
            await finishForeign.Task;

            return Nil.Value;
        });
        try
        {
            var actor = first.Spawn(async context =>
            {
                Term plain = await Query(context, context.Self);
                Term[] fields = Cons.Items(plain).ToArray();
                Require(Keys(fields).SequenceEqual(ProcessInfoDefaultHostNames.LogicalKeys.Skip(1)));
                Require(!Keys(fields).Intersect(ProcessInfoDefaultHostNames.ResourceKeys).Any());
                Require((await QueryItem(context, context.Self, ProcessInfoNames.RegisteredName)).Equals(Nil.Value));
                foreach (string key in ProcessInfoDefaultHostNames.ResourceKeys)
                    await BadArgument(QueryItem(context, context.Self, key));
                first.Register(ProcessInfoDefaultSubsetHostNames.Registration, context.Self);
                fields = Cons.Items(await Query(context, context.Self)).ToArray();
                Require(Keys(fields).SequenceEqual(ProcessInfoDefaultHostNames.LogicalKeys));
                Require(fields[0].Equals(Term.Tuple(Term.A(ProcessInfoNames.RegisteredName),
                    Term.A(ProcessInfoDefaultSubsetHostNames.Registration))));
                var fixedLocal = new Pid(context.Self.Node, context.Self.Id, context.Self.Creation);
                Require(Keys(Cons.Items(await Query(context, fixedLocal))).SequenceEqual(ProcessInfoDefaultHostNames.LogicalKeys));
                await BadArgument(Query(context, Term.I(0)));
                await BadArgument(Query(context, foreign.Pid));
                await BadArgument(Query(context, new Pid(ProcessInfoDefaultHostNames.RemoteNode, context.Self.Id)));
                var stale = new Pid(context.Self.Node, context.Self.Id, unchecked(context.Self.Creation + 1));
                Require((await Query(context, stale)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
                var dead = first.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
                await dead.Completion.WaitAsync(ProcessInfoDefaultSubsetHostNames.Deadline);
                Require((await Query(context, dead.Pid)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));

                return Nil.Value;
            });
            Require((await actor.Completion.WaitAsync(ProcessInfoDefaultSubsetHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
        finally
        {
            finishForeign.TrySetResult();
        }
        await foreign.Completion.WaitAsync(ProcessInfoDefaultSubsetHostNames.Deadline);
    }

    private static ValueTask<Term> Query(ProcessContext context, Term target)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target
        );

    private static ValueTask<Term> QueryItem(ProcessContext context, Term target, string key)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target,
            Term.A(key)
        );

    private static IEnumerable<string> Keys(IEnumerable<Term> fields)
        => fields.Select(field => ((Atom)((TupleTerm)field).Items[0]).Name);

    private static async Task BadArgument(ValueTask<Term> operation)
    {
        try
        {
            await operation;
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(ProcessInfoDefaultSubsetHostNames.Failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessInfoDefaultSubsetHostNames.Failure);
    }
}
