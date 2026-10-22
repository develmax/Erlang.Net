namespace Erlang;

internal sealed class ProcessSystemTaskOwner428
{
    internal LinkedList<SystemTaskRequest428> Pending { get; } = new();
    internal LinkedList<SystemTaskRequest428>[] Ready { get; } = [new(), new(), new(), new()];
    internal List<Exception> Failures { get; } = new();
    internal TaskCompletionSource Retired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal SystemTaskRequest428? Active { get; set; }
    internal bool PumpQueued { get; set; }
    internal bool Closing { get; set; }
    internal bool RetirementStarted { get; set; }

    internal int HighestPriority
    {
        get
        {
            int highest = Active is null ? -1 : (int)Active.Priority;
            for (int i = Ready.Length - 1; i >= 0; i--) if (Ready[i].Count != 0) return Math.Max(i, highest);

            return highest;
        }
    }
}
