namespace Erlang.Tests;

internal static class TraceOriginIdentity427Checks
{
    private static void Require(bool value) => TraceOriginIdentity427Access.Require(value);

    internal static async Task Identity()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(runtime, registry));
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        ProcessContext own = await actor.Ready, alien = await other.Ready;
        Require(own.Self.Node == alien.Self.Node && own.Self.Creation == alien.Self.Creation && own.Self.Id == alien.Self.Id);
        Term flags = Term.List(Term.A(TraceOriginIdentity427Names.Send));
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Trace,
            alien.Self,
            Term.A(TraceOriginIdentity427Names.True),
            flags
        );
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Trace,
            own.Self,
            Term.A(TraceOriginIdentity427Names.True),
            Term.List(Term.Tuple(Term.A(TraceOriginIdentity427Names.Tracer), alien.Self), Term.A(TraceOriginIdentity427Names.Send))
        );
        Term session = await TraceOriginIdentity427Access.Native(
            actor,
            TraceOriginIdentity427Names.Create,
            Term.A(TraceOriginIdentity427Names.Name),
            Term.A(TraceOriginIdentity427Names.Undefined),
            Nil.Value
        );
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Trace,
            session,
            alien.Self,
            Term.A(TraceOriginIdentity427Names.True),
            flags
        );
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Info,
            session,
            alien.Self,
            Term.A(TraceOriginIdentity427Names.Flags)
        );
        Require((await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            TraceOriginIdentity427Names.Runtime,
            TraceOriginIdentity427Names.Info,
            own.Self,
            Term.A(TraceOriginIdentity427Names.Flags)
        ))).Equals(
                Term.Tuple(Term.A(TraceOriginIdentity427Names.Flags), Nil.Value)));
        // Genuine ETF creation mutation follows the differential regression's
        // external-PID path, rather than manufacturing a foreign local owner.
        byte[] encoded = ((BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            TraceOriginIdentity427Names.Runtime,
            TraceOriginIdentity427Names.Encode,
            own.Self
        ))).ToArray();
        encoded[^1] ^= 1;
        Term external = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            TraceOriginIdentity427Names.Runtime,
            TraceOriginIdentity427Names.Decode,
            new BitString(encoded)
        ));
        Require(external is Pid previous && previous.Node == own.Self.Node
            && previous.Id == own.Self.Id && previous.Creation != own.Self.Creation);
        foreach (Term key in new Term[] { Term.A(TraceOriginIdentity427Names.Flags),
            Term.A(TraceOriginIdentity427Names.Tracer), Term.A(TraceOriginIdentity427Names.BadFlag), Term.I(0) })
        {
            Require((await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                TraceOriginIdentity427Names.Runtime,
                TraceOriginIdentity427Names.Info,
                external,
                key
            )))
                .Equals(Term.A(TraceOriginIdentity427Names.Undefined)));
            Require((await TraceOriginIdentity427Access.Native(
                actor,
                TraceOriginIdentity427Names.Info,
                session,
                external,
                key
            )).Equals(Term.A(TraceOriginIdentity427Names.Undefined)));
        }
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Info,
            session,
            own.Self,
            Term.I(0)
        );
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Info,
            session,
            new Pid(TraceOriginIdentity427Names.RemoteNode, own.Self.Id, own.Self.Creation),
            Term.A(TraceOriginIdentity427Names.Flags)
        );
        await TraceOriginIdentity427Access.Native(actor, TraceOriginIdentity427Names.Destroy, session);
        ProcessHandle dead = runtime.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
        await dead.Completion;
        Term deadTracer = await TraceOriginIdentity427Access.Native(
            actor,
            TraceOriginIdentity427Names.Create,
            Term.A(TraceOriginIdentity427Names.Name),
            dead.Pid,
            Nil.Value
        );
        Require(deadTracer is TupleTerm { Items.Count: 2 });
        Require((await TraceOriginIdentity427Access.Native(actor, TraceOriginIdentity427Names.Destroy, deadTracer))
            .Equals(Term.A(TraceOriginIdentity427Names.True)));
        await TraceOriginIdentity427Access.Bad(
            actor,
            TraceOriginIdentity427Names.Create,
            Term.A(TraceOriginIdentity427Names.Name),
            alien.Self,
            Nil.Value
        );
    }

    internal static async Task Origin()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term[] trace = [Term.I(0), Term.A(TraceOriginIdentity427Names.True), Term.List(Term.A(TraceOriginIdentity427Names.BadFlag))];
        ErlangException native = await TraceOriginIdentity427Access.Bad(actor, TraceOriginIdentity427Names.Trace, trace);
        TraceOriginIdentity427Access.Frame(
            native,
            TraceOriginIdentity427Names.Module,
            TraceOriginIdentity427Names.Trace,
            trace,
            TraceOriginIdentity427Names.BadOption
        );
        Term[] pattern = [Term.A(TraceOriginIdentity427Names.Send), Term.A(TraceOriginIdentity427Names.True), Term.List(Term.A(TraceOriginIdentity427Names.BadFlag))];
        TraceOriginIdentity427Access.Frame(
            await TraceOriginIdentity427Access.Bad(actor, TraceOriginIdentity427Names.Pattern, pattern),
            TraceOriginIdentity427Names.Module,
            TraceOriginIdentity427Names.Pattern,
            pattern,
            TraceOriginIdentity427Names.BadOption
        );
        Term[] invalidSession = [Term.I(0), Term.I(0), Term.A(TraceOriginIdentity427Names.True), Nil.Value];
        TraceOriginIdentity427Access.Frame(
            await TraceOriginIdentity427Access.Bad(actor, TraceOriginIdentity427Names.Trace, invalidSession),
            TraceOriginIdentity427Names.Module,
            TraceOriginIdentity427Names.Trace,
            invalidSession,
            TraceOriginIdentity427Names.Session
        );
        bool old = false;
        try
        {
            await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                TraceOriginIdentity427Names.Runtime,
                TraceOriginIdentity427Names.Trace,
                trace
            ));
        }
        catch (ErlangException legacy)
        {
            TraceOriginIdentity427Access.Frame(
                legacy,
                TraceOriginIdentity427Names.Runtime,
                TraceOriginIdentity427Names.Trace,
                trace,
                TraceOriginIdentity427Names.BadOption
            );
            old = true;
        }
        Require(old);
    }
}
