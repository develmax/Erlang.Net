namespace Erlang;

internal static class ErlangProcessQueryRegistrations
{
    internal static Term NodeOf(Term value) => Term.A(value switch
    {
        Pid pid => pid.Node,
        PortTerm port => port.Node,
        ReferenceTerm reference => reference.Node,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    });

    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, ErlangFunction body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            body
        );
        Add(ErlangProcessQueryNames.Node, 0, (context, _) => ValueTask.FromResult<Term>(context.Runtime.NodeAtom));
        Add(
            ErlangProcessQueryNames.Node,
            1,
            (_, arguments) => ValueTask.FromResult(NodeOf(arguments[0]))
        );
        Add(ErlangProcessQueryNames.Processes, 0, (context, _) => ValueTask.FromResult(Cons.From(context.Runtime.ProcessSnapshot())));
        Add(
            ErlangProcessQueryNames.Registered,
            0,
            (context, _) => ValueTask.FromResult(Cons.From(context.Runtime.RegisteredSnapshot().Select(Term.A)))
        );
        Add(
            ErlangProcessQueryNames.Alive,
            1,
            (context, arguments) => context.Runtime.QueryProcessAlive499(context, arguments[0])
        );
        Add(
            ErlangProcessQueryNames.Yield,
            0,
            async (context, _) =>
        {
            context.Cancellation.ThrowIfCancellationRequested();
            context.Runtime.AdmitSignals(context);
            await Task.Yield();
            context.Cancellation.ThrowIfCancellationRequested();

            return Term.A(ErlangBooleanAtoms.True);
        }
        );
        Add(
            ErlangProcessQueryNames.Bump,
            1,
            async (context, arguments) =>
        {
            if (arguments[0] is not Integer value || value.Value < 0 || value.Value > ProcessSchedulingLimits.MaximumSmallBump)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            await context.BumpReductionsAsync((int)System.Numerics.BigInteger.Min(value.Value, ProcessSchedulingLimits.NativeMaximumBump));

            return Term.A(ErlangBooleanAtoms.True);
        }
        );
    }
}
