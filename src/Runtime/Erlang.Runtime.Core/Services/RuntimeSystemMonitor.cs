namespace Erlang;

internal sealed class RuntimeSystemMonitor(ProcessRuntime runtime, object runtimeGate, Dictionary<Pid, ProcessContext> processes)
{
    private readonly object eventGate = new();
    private readonly HashSet<Pid> highQueues = new();
    private readonly Queue<(Pid Receiver, Term Message)> notifications = new();
    private Pid? receiver;
    private SystemMonitorOptions options = new(null, 0);
    private bool dispatching;
    private bool stopped;
    private Task dispatcher = Task.CompletedTask;

    public Term Get()
    {
        lock (runtimeGate)
        {
            lock (eventGate)
                return Settings();
        }
    }

    public Term Configure(Term target, Term optionList)
    {
        lock (runtimeGate)
        {
            runtime.ThrowIfHalted();
            if (optionList is not Cons && optionList is not Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (target is Atom { Name: SystemMonitorNames.Undefined } || optionList is Nil)
            {
                lock (eventGate)
                {
                    Term previous = Settings();
                    receiver = null;
                    options = new(null, 0);
                    highQueues.Clear();

                    return previous;
                }
            }
            if (target is not Pid pid || pid.Node != runtime.Node || !processes.ContainsKey(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            SystemMonitorOptions wanted = SystemMonitorOptions.Parse(optionList);
            lock (eventGate)
            {
                Term previous = Settings();
                receiver = pid;
                options = wanted;
                if (wanted.Disable is null)
                    highQueues.Clear();

                return previous;
            }
        }
    }

    public void QueueChanged(Pid process, int admittedCount)
    {
        lock (eventGate)
        {
            if (stopped || receiver is null || options.Disable is not long disable)
                return;
            bool high = highQueues.Contains(process);
            bool? transition = !high && (ulong)admittedCount >= options.Enable ? true : high && admittedCount <= disable ? false : null;
            if (transition is not bool above)
                return;
            if (above)
                highQueues.Add(process);
            else
                highQueues.Remove(process);
            if (process.Equals(receiver))
                return;
            notifications.Enqueue((receiver, Term.Tuple(
                Term.A(SystemMonitorNames.Notification),
                process,
                Term.A(SystemMonitorNames.LongQueue),
                CoreModules.Boolean(above)
            )));
            if (!dispatching)
            {
                dispatching = true;
                dispatcher = Task.Run(Dispatch);
            }
        }
    }

    internal void BusyPort(Pid process, PortTerm port)
    {
        lock (eventGate)
        {
            if (stopped || receiver is null || !options.BusyPort || process.Equals(receiver))
                return;
            notifications.Enqueue((receiver, Term.Tuple(
                Term.A(SystemMonitorNames.Notification),
                process,
                Term.A(SystemMonitorNames.BusyPort),
                port
            )));
            if (!dispatching)
            {
                dispatching = true;
                dispatcher = Task.Run(Dispatch);
            }
        }
    }

    public void Terminated(Pid process)
    {
        lock (eventGate)
        {
            highQueues.Remove(process);
            if (receiver?.Equals(process) == true)
            {
                receiver = null;
                options = new(null, 0);
                highQueues.Clear();
            }
        }
    }

    private Term Settings() => receiver is null ? Term.A(SystemMonitorNames.Undefined) : Term.Tuple(receiver, options.ToTerm());

    internal Task StopForHalt()
    {
        lock (eventGate)
        {
            stopped = true;
            receiver = null;
            options = new(null, 0);
            highQueues.Clear();
            notifications.Clear();

            return dispatcher;
        }
    }

    private void Dispatch()
    {
        while (true)
        {
            (Pid Receiver, Term Message) next;
            lock (eventGate)
            {
                if (!notifications.TryDequeue(out next))
                {
                    dispatching = false;

                    return;
                }
            }
            runtime.SendSystem(next.Receiver, next.Message);
        }
    }
}
