namespace Erlang;

internal static class ErlangProcessQueryRegistrations
{
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
            (_, arguments) => ValueTask.FromResult<Term>(Term.A(arguments[0] switch
            {
                Pid pid => pid.Node,
                PortTerm port => port.Node,
                ReferenceTerm reference => reference.Node,
                _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
            }))
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
            (context, arguments) =>
        {
            if (arguments[0] is not Pid pid || pid.Node != context.Runtime.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return ValueTask.FromResult<Term>(Term.A(context.Runtime.IsAlive(pid) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
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
