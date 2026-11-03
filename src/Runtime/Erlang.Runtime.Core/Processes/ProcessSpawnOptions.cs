namespace Erlang;

internal sealed class ProcessSpawnOptions
{
    internal List<Term> SchedulerOptions { get; } = new();
    internal ushort? FullSweepAfter { get; private set; }
    internal ProcessMessageQueueDataMode411 MessageQueueData { get; private set; } = ProcessMessageQueueData411.Default;

    internal int ResolveScheduler(int configured)
    {
        int result = ProcessSchedulerNames.Unbound;
        foreach (var option in SchedulerOptions)
        {
            if (option is not TupleTerm { Items: [Atom, var value] })
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            result = ProcessSchedulerValues.Parse(value, configured);
        }

        return result;
    }

    public ProcessSchedulingPriority Priority { get; private set; } = ProcessSchedulingPriority.Normal;

    public bool AsyncDistribution
    {
        get; private set;
    }
    public bool Link
    {
        get; private set;
    }
    public bool PriorityLink
    {
        get; private set;
    }
    public bool Monitor
    {
        get; private set;
    }
    public ProcessMonitorOptions MonitorOptions { get; private set; } = ProcessMonitorOptions.Default;

    internal static ProcessSpawnOptions ParseRemote(Term value)
    {
        Term[] allOptions = Cons.Items(value).ToArray();
        ProcessSpawnResourceValidation540 resources = ProcessSpawnResourceOptions540.Analyze(
            allOptions,
            IntPtr.Size * ProcessSpawnResourceNames540.BitsPerByte
        );
        if (resources.Invalid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var result = new ProcessSpawnOptions();
        foreach (Term option in allOptions)
        {
            if (ProcessSpawnResourceOptions540.IsResourceOption(option))
                continue;
            if (option is TupleTerm { Items: [Atom { Name: ProcessSchedulerNames.Binding }, ..] })
            {
                result.SchedulerOptions.Add(option);
                continue;
            }
            var current = Parse(Term.List(option));
            if (option is TupleTerm { Items: [Atom { Name: ProcessAsyncDistributionNames.Flag }, _] })
                result.AsyncDistribution = current.AsyncDistribution;
            if (option is TupleTerm { Items: [Atom { Name: ProcessPriorityNames.Flag }, _] })
                result.Priority = current.Priority;
            if (option is TupleTerm { Items: [Atom { Name: ProcessFullSweepNames.Flag }, _] })
                result.FullSweepAfter = current.FullSweepAfter;
            if (option is TupleTerm { Items: [Atom { Name: ProcessMessageQueueDataNames411.Flag }, _] })
                result.MessageQueueData = current.MessageQueueData;
            result.Link |= current.Link;
            if (option is TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Link }, _] })
                result.PriorityLink |= current.PriorityLink;
            result.Monitor |= current.Monitor;
            if (option is TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Monitor }, _] })
                result.MonitorOptions = current.MonitorOptions;
        }

        if (resources.HasResourceOption)
            throw new NotSupportedException(ProcessSpawnResourceNames540.Unsupported);

        return result;
    }

    public static ProcessSpawnOptions Parse(Term value, int configuredSchedulers = int.MaxValue)
    {
        Term[] allOptions = Cons.Items(value).ToArray();
        ProcessSpawnResourceValidation540 resources = ProcessSpawnResourceOptions540.Analyze(
            allOptions,
            IntPtr.Size * ProcessSpawnResourceNames540.BitsPerByte
        );
        if (resources.Invalid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var result = new ProcessSpawnOptions();
        foreach (Term option in allOptions)
        {
            if (ProcessSpawnResourceOptions540.IsResourceOption(option))
                continue;
            switch (option)
            {
                case TupleTerm { Items: [Atom { Name: ProcessAsyncDistributionNames.Flag }, Atom { Name: ErlangBooleanAtoms.True }] }:
                    result.AsyncDistribution = true;
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessAsyncDistributionNames.Flag }, Atom { Name: ErlangBooleanAtoms.False }] }:
                    result.AsyncDistribution = false;
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessMessageQueueDataNames411.Flag }, var queueValue] }:
                    result.MessageQueueData = ProcessMessageQueueData411.Parse(queueValue);
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessFullSweepNames.Flag }, var sweepValue] }:
                    result.FullSweepAfter = ProcessFullSweepAfter.ParseProcess(sweepValue);
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessSchedulerNames.Binding }, var schedulerValue] }:
                    _ = ProcessSchedulerValues.Parse(schedulerValue, configuredSchedulers);
                    result.SchedulerOptions.Add(option);
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessPriorityNames.Flag }, var priorityValue] }:
                    result.Priority = ProcessPriorityValues.Parse(priorityValue);
                    break;
                case Atom { Name: ProcessSpawnOptionNames.Link }:
                    result.Link = true;
                    break;
                case Atom { Name: ProcessSpawnOptionNames.Monitor }:
                    result.Monitor = true;
                    result.MonitorOptions = ProcessMonitorOptions.Default;
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Link }, var flags] }:
                    result.PriorityLink = false;
                    foreach (Term flag in Cons.Items(flags))
                        if (flag is Atom { Name: ProcessSpawnOptionNames.Priority })
                            result.PriorityLink = true;
                        else
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                    result.Link = true;
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Monitor }, var flags] }:
                    bool priority = false;
                    string? alias = null;
                    Term tag = ProcessMonitorOptions.Default.Tag;
                    foreach (Term flag in Cons.Items(flags))
                    {
                        if (flag is Atom { Name: ProcessMonitorOptionNames.Priority })
                            priority = true;
                        else if (flag is TupleTerm { Items: [Atom { Name: ProcessMonitorOptionNames.Tag }, var custom] })
                            tag = custom;
                        else if (flag is TupleTerm { Items: [Atom { Name: ProcessMonitorOptionNames.Alias }, Atom { Name: ProcessAliasNames.Explicit or ProcessMonitorOptionNames.Demonitor or ProcessMonitorOptionNames.ReplyDemonitor } mode] })
                            alias = mode.Name;
                        else
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                    }
                    result.Monitor = true;
                    result.MonitorOptions = new(priority, alias, tag);
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }

        if (resources.HasResourceOption)
            throw new NotSupportedException(ProcessSpawnResourceNames540.Unsupported);

        return result;
    }
}
