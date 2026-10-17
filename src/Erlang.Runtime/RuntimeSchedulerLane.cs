namespace Erlang;

internal sealed class RuntimeSchedulerLane(int identity)
{
    internal int Identity { get; } = identity;
    internal bool Online { get; set; } = true;
    internal bool Pumping { get; set; }
    internal RuntimeProcessTaskScheduler? Running { get; set; }
    internal LinkedList<RuntimeProcessTaskScheduler> Maximum { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> High { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> Ordinary { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> Cleanup { get; } = new();

    internal bool Empty => Maximum.Count == 0 && High.Count == 0 && Ordinary.Count == 0 && Cleanup.Count == 0;

    internal LinkedList<RuntimeProcessTaskScheduler> Queue(ProcessSchedulingPriority priority) => priority switch
    {
        ProcessSchedulingPriority.Maximum => Maximum,
        ProcessSchedulingPriority.High => High,
        _ => Ordinary
    };
}
