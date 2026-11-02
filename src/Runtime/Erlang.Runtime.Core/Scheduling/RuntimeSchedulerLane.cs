namespace Erlang;

internal sealed class RuntimeSchedulerLane(int identity)
{
    internal int Identity { get; } = identity;
    internal bool Online { get; set; } = true;
    internal bool WallTimeEnabled { get; set; }
    internal long WallTimeOrigin { get; set; }
    internal long WallTimeActiveStarted { get; set; }
    internal long WallTimeActiveTicks { get; set; }
    internal LinkedList<RuntimeSchedulerAuxiliary420> Auxiliary420 { get; } = new();
    internal bool AuxiliaryRunning420 { get; set; }
    internal bool HasRunningWork420 => Running is not null || AuxiliaryRunning420;
    internal bool Pumping { get; set; }
    internal RuntimeProcessTaskScheduler? Running { get; set; }
    internal LinkedList<RuntimeProcessTaskScheduler> Maximum { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> High { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> Ordinary { get; } = new();
    internal LinkedList<RuntimeProcessTaskScheduler> Cleanup { get; } = new();

    internal bool Empty => Maximum.Count == 0 && High.Count == 0 && Ordinary.Count == 0 && Cleanup.Count == 0 && Auxiliary420.Count == 0;

    internal LinkedList<RuntimeProcessTaskScheduler> Queue(ProcessSchedulingPriority priority) => priority switch
    {
        ProcessSchedulingPriority.Maximum => Maximum,
        ProcessSchedulingPriority.High => High,
        _ => Ordinary
    };
}
