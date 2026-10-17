namespace Erlang;

internal static class ProcessPriorityValues
{
    private static readonly Atom Low = Term.A(ProcessPriorityNames.Low);
    private static readonly Atom Normal = Term.A(ProcessPriorityNames.Normal);
    private static readonly Atom High = Term.A(ProcessPriorityNames.High);
    private static readonly Atom Maximum = Term.A(ProcessPriorityNames.Maximum);

    internal static ProcessSchedulingPriority Parse(Term value) => value switch
    {
        Atom { Name: ProcessPriorityNames.Low } => ProcessSchedulingPriority.Low,
        Atom { Name: ProcessPriorityNames.Normal } => ProcessSchedulingPriority.Normal,
        Atom { Name: ProcessPriorityNames.High } => ProcessSchedulingPriority.High,
        Atom { Name: ProcessPriorityNames.Maximum } => ProcessSchedulingPriority.Maximum,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    internal static Atom ToAtom(ProcessSchedulingPriority value) => value switch
    {
        ProcessSchedulingPriority.Low => Low,
        ProcessSchedulingPriority.Normal => Normal,
        ProcessSchedulingPriority.High => High,
        _ => Maximum
    };
}
