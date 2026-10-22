namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly DirtySystemHandlerSlot428[] dirtySystemHandlers428 =
    [new(ProcessSchedulingPriority.Normal), new(ProcessSchedulingPriority.High), new(ProcessSchedulingPriority.Maximum)];
    private bool systemTasksClosed428;

    private void RequireDirtyHandlerRole428(ProcessContext caller)
    {
        RequireBeamfileActor418(caller);
        if (systemTasksClosed428 || !dirtySystemHandlers428.Any(slot => ReferenceEquals(slot.Actor, caller)))
            throw new ErlangException(SystemTaskNames428.NotSupported);
    }

    private void StartDirtySystemHandlersBeforeBoot428()
    {
        if (Modules.SourceOwner(SystemTaskNames428.HandlerModule, SystemTaskNames428.Start, 0) is not PreparedBeamImage image
            || !DirtySystemHandlerSource428.Matches(image, Modules)) return;
        StartDirtySystemHandlers428(image);
    }

    internal void StartDirtySystemHandlers428(PreparedBeamImage image)
    {
        // Registry identity and actual byte hashing precede runtime.gate.
        if (!ReferenceEquals(Modules.SourceOwner(SystemTaskNames428.HandlerModule, SystemTaskNames428.Start, 0), image)
            || !DirtySystemHandlerSource428.Matches(image, Modules))
            throw new InvalidOperationException(SystemTaskNames428.Startup);
        var reservation = new object();
        ProcessContext?[] started = new ProcessContext?[dirtySystemHandlers428.Length];
        ProcessContext parent;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            if (dirtySystemHandlers428.All(slot => slot.Actor is { Terminated: false } && ReferenceEquals(slot.Source, image))) return;
            if (systemTasksClosed428 || dirtySystemHandlers428.Any(slot => slot.Actor is not null || slot.Reservation is not null))
                throw new InvalidOperationException(SystemTaskNames428.RoleConflict);
            parent = processes[InitializationPid];
            foreach (var slot in dirtySystemHandlers428)
            {
                slot.Reservation = reservation;
                slot.Source = image;
            }
        }
        try
        {
            lock (gate)
            {
                int index = 0;
                foreach (var slot in dirtySystemHandlers428)
                {
                    var selected = slot;
                    ProcessHandle handle = SpawnTraced(
                        async child =>
                    {
                        lock (gate)
                        {
                            if (systemTasksClosed428 || !ReferenceEquals(selected.Reservation, reservation)
                                || !ReferenceEquals(selected.Actor, child))
                                throw new InvalidOperationException(SystemTaskNames428.Startup);
                        }

                        return await BeamRegisterInterpreter.Execute(
                            child,
                            image.Code,
                            SystemTaskNames428.Start,
                            [],
                            image.ExecutionIdentity
                        );
                    },
                        parent,
                        false,
                        SequenceSpawnDescription.Module(
                            Term.A(SystemTaskNames428.HandlerModule),
                            Term.A(SystemTaskNames428.Start),
                            Nil.Value,
                            Nil.Value
                        ),
                        initialPriority: selected.Priority,
                        systemProcess: true,
                        initialMessageQueueData: ProcessMessageQueueDataMode411.OffHeap
                    );
                    selected.Actor = processes[handle.Pid];
                    started[index++] = selected.Actor;
                }
            }
        }
        catch
        {
            // An actual partially started privileged set cannot be left serving requests.
            // Closing all roles before termination prevents recursive fatal handling.
            lock (gate)
            {
                systemTasksClosed428 = true;
                foreach (var slot in dirtySystemHandlers428) slot.Clear();
                foreach (var child in started) if (child is not null) Terminate(child, Term.A(ProcessExitReasons.Shutdown));
            }
            throw;
        }
    }

    private ProcessContext DirtySystemHandler428(ProcessSchedulingPriority priority)
    {
        var slot = priority switch
        {
            ProcessSchedulingPriority.Maximum => dirtySystemHandlers428[2],
            ProcessSchedulingPriority.High => dirtySystemHandlers428[1],
            _ => dirtySystemHandlers428[0]
        };

        return slot.Actor is { Terminated: false } actor ? actor
            : throw new NotSupportedException(SystemTaskNames428.Startup);
    }

    private void DirtySystemHandlerEnded428(ProcessContext target)
    {
        var slot = dirtySystemHandlers428.FirstOrDefault(item => ReferenceEquals(item.Actor, target));
        if (slot is null) return;
        slot.Clear();
        if (!disposed && !systemTasksClosed428) RequestImmediateHalt(1);
    }

    private void CloseDirtySystemHandlers428()
    {
        systemTasksClosed428 = true;
        dirtySignalNotifications428.Clear();
        foreach (var slot in dirtySystemHandlers428) slot.Clear();
    }
}
