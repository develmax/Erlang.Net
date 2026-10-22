namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly List<Exception> systemTaskServiceFailures428 = new();
    private readonly HashSet<(Pid Target, ProcessSchedulingPriority Priority)> dirtySignalNotifications428 = new();

    internal void NotifyDirtySignalHandler428(ProcessContext target, ProcessSchedulingPriority? priority = null)
    {
        lock (gate)
        {
            if (systemTasksClosed428 || disposed || target.Terminated || !target.Mailbox.HasPendingSignals428
                || !InDirtyState428(target)) return;
            var requested = priority ?? target.SchedulingPriorityLevel;
            var selected = requested switch
            {
                ProcessSchedulingPriority.Maximum => dirtySystemHandlers428[2],
                ProcessSchedulingPriority.High => dirtySystemHandlers428[1],
                _ => dirtySystemHandlers428[0]
            };
            // An explicit small runtime which did not select original handlers
            // retains existing normal-turn cooperation, never invents a role.
            if (selected.Actor is not { Terminated: false } handler) return;
            var key = (target.Self, selected.Priority);
            if (!dirtySignalNotifications428.Add(key)) return;
            try
            {
                SendSystem(handler.Self, target.Self, Term.A(SystemTaskNames428.System));
            }
            catch
            {
                dirtySignalNotifications428.Remove(key);
                throw;
            }
        }
    }

    internal void DirtyTurnEnded428(ProcessContext target)
    {
        try
        {
            NotifyDirtySignalHandler428(target);
        }
        catch (Exception failure)
        {
            // Never abandon a dispatcher pump as an unobserved Task fault.
            // Retain the actual error for shutdown and stop this VM explicitly.
            lock (gate)
            {
                systemTaskServiceFailures428.Add(failure);
                if (!disposed)
                    try
                    {
                        RequestImmediateHalt(1);
                    }
                    catch (Exception stopFailure)
                    {
                        systemTaskServiceFailures428.Add(stopFailure);
                    }
            }
        }
    }

}
