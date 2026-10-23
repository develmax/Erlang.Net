namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Atom ChangeSchedulingPriority(ProcessContext context, Term value)
    {
        ProcessSchedulingPriority priority = ProcessPriorityValues.Parse(value);
        lock (gate)
        {
            RequireAlive(context.Self);
            Atom previous = context.SchedulingPriority;
            context.SchedulingPriorityLevel = priority;
            actorDispatcher.PriorityChanged(context);

            return previous;
        }
    }
}
