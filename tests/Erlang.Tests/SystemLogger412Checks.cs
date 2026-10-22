using Erlang;

namespace Erlang.Tests;

internal static class SystemLogger412Checks
{
    private static Task<Term> Get(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            SystemLogger412Names.Erlang,
            SystemLogger412Names.Info,
            Term.A(SystemLogger412Names.Flag)
        ));

    private static Task<Term> Set(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime, Term value)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            SystemLogger412Names.Erlang,
            SystemLogger412Names.Set,
            Term.A(SystemLogger412Names.Flag),
            value
        ));

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(SystemLogger412Names.Failure);
    }

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime();
        await using var other = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var peer = new RuntimeNodeFoundationHostActor(other);
        Term logger = Term.A(SystemLogger412Names.Logger), undefined = Term.A(SystemLogger412Names.Undefined);
        Require((await Get(actor, runtime)).Equals(logger));
        Require((await Set(actor, runtime, undefined)).Equals(logger) && (await Get(actor, runtime)).Equals(undefined));
        Pid self = (await actor.Ready).Self;
        Require((await Set(actor, runtime, self)).Equals(undefined) && (await Get(actor, runtime)).Equals(self));
        Require((await Set(actor, runtime, self)).Equals(self));
        Require((await Get(peer, other)).Equals(logger));
        Require((await Set(actor, runtime, logger)).Equals(self));
    }

    internal static async Task Identity()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Pid dead;
        await using (var victim = new RuntimeNodeFoundationHostActor(runtime)) dead = (await victim.Ready).Self;
        Require((await Set(actor, runtime, dead)).Equals(Term.A(SystemLogger412Names.Logger)));
        Require((await Get(actor, runtime)).Equals(dead));
        await using var foreign = new ProcessRuntime();
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        foreach ((Pid target, bool foreignDomain) in new[] { ((await foreignActor.Ready).Self, true),
            (new Pid(SystemLogger412Names.ForeignNode, SystemLogger412Names.RemoteId, SystemLogger412Names.RemoteCreation), false) })
        {
            bool refused = false;
            try
            {
                _ = await Set(actor, runtime, target);
            }
            catch (ErlangException error)
            {
                refused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
            }
            catch (ArgumentException) when (foreignDomain)
            {
                refused = true;
            }
            Require(refused && (await Get(actor, runtime)).Equals(dead));
        }
        _ = await Set(actor, runtime, Term.A(SystemLogger412Names.Logger));
    }

    internal static async Task Invalid()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term value in new Term[] { Term.A(SystemLogger412Names.Invalid), Term.I(0), Nil.Value,
            Term.Tuple(Term.A(SystemLogger412Names.Logger)), new BitString([]) })
        {
            bool refused = false;
            try
            {
                _ = await Set(actor, runtime, value);
            }
            catch (ErlangException error)
            {
                refused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
            }
            Require(refused && (await Get(actor, runtime)).Equals(Term.A(SystemLogger412Names.Logger)));
        }
        await actor.InvokeAsync(async context =>
        {
            Term old = await runtime.Modules.Call(
                context,
                SystemLogger412Names.Erlang,
                SystemLogger412Names.Set,
                Term.A(SystemLogger412Names.Backtrace),
                Term.I(2)
            );
            _ = await runtime.Modules.Call(
                context,
                SystemLogger412Names.Erlang,
                SystemLogger412Names.Set,
                Term.A(SystemLogger412Names.Backtrace),
                old
            );

            return true;
        });
    }
}
