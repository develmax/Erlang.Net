namespace Erlang;

internal static class ProcessInfoAtoms
{
    public static readonly Atom Links = Term.A(ProcessInfoNames.Links);
    public static readonly Atom TrapExit = Term.A(ProcessInfoNames.TrapExit);
    public static readonly Atom GroupLeader = Term.A(ProcessInfoNames.GroupLeader);
    public static readonly Atom RegisteredName = Term.A(ProcessInfoNames.RegisteredName);
    public static readonly Atom Undefined = Term.A(ProcessRegistryAtoms.Undefined);

    public static bool IsSupported(Atom key) => key.Name is ProcessInfoNames.Links or ProcessInfoNames.TrapExit or ProcessInfoNames.GroupLeader or
        ProcessInfoNames.RegisteredName or ProcessInfoNames.LastCalls or ProcessInfoNames.Dictionary or ProcessInfoNames.Messages or
        ProcessInfoNames.MessageQueueLength or ProcessInfoNames.Monitors or ProcessInfoNames.MonitoredBy or ProcessInfoNames.Status or
        ProcessInfoNames.CurrentFunction or ProcessInfoNames.CurrentStackTrace;
}
