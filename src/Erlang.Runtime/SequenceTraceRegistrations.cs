namespace Erlang;

internal static class SequenceTraceRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.Set,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.SequenceTracing.Set(context, arguments[0], arguments[1]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.Info,
            1,
            (context, arguments) => ValueTask.FromResult(context.Runtime.SequenceTracing.Info(context, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.Print,
            1,
            (context, arguments) => ValueTask.FromResult(context.Runtime.SequenceTracing.Print(context, null, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.Print,
            2,
            (context, arguments) => ValueTask.FromResult(context.Runtime.SequenceTracing.Print(context, arguments[0], arguments[1]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.SystemFlag,
            2,
            (context, arguments) => arguments[0] is Atom { Name: SequenceTraceNames.SystemTracer }
            ? ValueTask.FromResult(context.Runtime.SequenceTracing.SetTracer(arguments[1]))
            : CpuTopologyStateRegistrations.TrySystemFlag(
                context,
                arguments[0],
                arguments[1],
                out Term result
            )
            ? ValueTask.FromResult(result)
            : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        registry.Register(
            ErlangModuleNames.Module,
            SequenceTraceNames.SystemInfo,
            1,
            (context, arguments) => arguments[0] is Atom { Name: SequenceTraceNames.SystemTracer }
            ? ValueTask.FromResult<Term>(Term.Tuple(Term.A(SequenceTraceNames.SystemTracer), context.Runtime.SequenceTracing.Tracer()))
            : arguments[0] is Atom { Name: AtomRegistryInfoNames.AtomCount }
            ? ValueTask.FromResult<Term>(Term.I(AtomNameRegistry.Count))
            : arguments[0] is Atom { Name: RuntimeLocalNodeAdmissionNames.Creation } ? ValueTask.FromResult<Term>(Term.I(context.Runtime.LocalIdentity.Snapshot.Creation))
            : arguments[0] is Atom { Name: RuntimeActorDispatchNames.Schedulers }
            ? ValueTask.FromResult<Term>(Term.I(context.Runtime.ActorDispatchCapacity))
            : CpuTopologyStateRegistrations.TrySystemInfo(context, arguments[0], out Term result)
            ? ValueTask.FromResult(result)
            : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
    }
}
