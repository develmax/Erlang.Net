namespace Erlang;

internal sealed class ProcessSpawnOptions
{
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
        var result = new ProcessSpawnOptions();
        foreach (Term option in Cons.Items(value))
        {
            var current = Parse(Term.List(option));
            if (option is TupleTerm { Items: [Atom { Name: ProcessPriorityNames.Flag }, _] })
                result.Priority = current.Priority;
            result.Link |= current.Link;
            result.PriorityLink |= current.PriorityLink;
            result.Monitor |= current.Monitor;
            if (option is TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Monitor }, _] })
                result.MonitorOptions = current.MonitorOptions;
        }

        return result;
    }

    public static ProcessSpawnOptions Parse(Term value)
    {
        var result = new ProcessSpawnOptions();
        foreach (Term option in Cons.Items(value))
        {
            switch (option)
            {
                case TupleTerm { Items: [Atom { Name: ProcessAsyncDistributionNames.Flag }, Atom { Name: ErlangBooleanAtoms.True }] }:
                    result.AsyncDistribution = true;
                    break;
                case TupleTerm { Items: [Atom { Name: ProcessAsyncDistributionNames.Flag }, Atom { Name: ErlangBooleanAtoms.False }] }:
                    result.AsyncDistribution = false;
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

        return result;
    }
}
