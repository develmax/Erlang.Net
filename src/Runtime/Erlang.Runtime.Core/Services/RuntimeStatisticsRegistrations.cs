namespace Erlang;

internal static class RuntimeStatisticsRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            RuntimeStatisticsNames.Function,
            1,
            (context, arguments) =>
        {
            if (arguments[0] is not Atom key)
                throw InvalidArguments(context, arguments);
            if (key.Name == RuntimeStatisticsNames.Io)
                return ValueTask.FromResult(context.Runtime.Statistics.SampleIo());
            if (key.Name == RuntimeStatisticsNames.ContextSwitches)
                return ValueTask.FromResult(context.Runtime.Statistics.SampleContextSwitches());
            if (key.Name is RuntimeStatisticsNames.Reductions or RuntimeStatisticsNames.ExactReductions)
                return ValueTask.FromResult(context.Runtime.Statistics.Sample(key.Name));
            if (key.Name is RuntimeStatisticsNames.WallClock or RuntimeStatisticsNames.Runtime or RuntimeStatisticsNames.GarbageCollection)
            {
                try
                {
                    return ValueTask.FromResult(context.Runtime.Statistics.Sample(key.Name));
                }
                catch (Exception exception) when (exception is NotSupportedException or System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    throw new ErlangException(RuntimeStatisticsNames.NotSupported);
                }
            }
            if (key.Name is RuntimeStatisticsNames.SchedulerWallTime or RuntimeStatisticsNames.SchedulerWallTimeAll)
                return context.Runtime.SampleSchedulerWallTime(context, key.Name == RuntimeStatisticsNames.SchedulerWallTimeAll);
            if (key.Name == RuntimeStatisticsNames.MicrostateAccounting)
                return context.Runtime.SampleMicrostates(context);
            if (RuntimeSchedulerStatistics.IsQueue(key.Name))
                return ValueTask.FromResult(context.Runtime.SampleSchedulerQueues(key.Name));
            if (RuntimeStatisticsNames.IsPhysical(key.Name))
                throw new ErlangException(RuntimeStatisticsNames.NotSupported);

            throw InvalidArguments(context, arguments);
        }
        );
    }

    private static ErlangException InvalidArguments(ProcessContext context, IReadOnlyList<Term> arguments) => new(ErlangErrorReasons.BadArgument)
    {
        ExplicitStackTrace = true,
        StackTraceTerm = context.CaptureErrorStack(
            Cons.From(arguments),
            new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeStatisticsNames.ErrorModule), Term.A(RuntimeStatisticsNames.ErrorFormatter))]),
            RuntimeStatisticsNames.Function
        )
    };
}
