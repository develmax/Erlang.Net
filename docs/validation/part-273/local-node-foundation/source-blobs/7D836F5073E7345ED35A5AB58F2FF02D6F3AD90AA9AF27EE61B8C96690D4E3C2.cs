namespace Erlang;

public sealed class RuntimeLocalNodeEvents
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly Dictionary<ProcessContext, int> subscribers = new(ReferenceEqualityComparer.Instance);
    private bool closed;

    internal RuntimeLocalNodeEvents(ProcessRuntime runtime, object gate)
    {
        this.runtime = runtime;
        this.gate = gate;
    }

    public int Monitor(ProcessContext process, bool enable)
    {
        ArgumentNullException.ThrowIfNull(process);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || runtime.IsHaltRequested, RuntimeLocalNodeAdmissionNames.Closed);
            if (!runtime.IsOwnedLiveNodeProcess(process))
                throw new ArgumentException(RuntimeLocalNodeAdmissionNames.Owner, nameof(process));
            int previous = subscribers.GetValueOrDefault(process);
            if (enable)
                subscribers[process] = checked(previous + 1);
            else
                subscribers.Remove(process);

            return previous;
        }
    }

    internal void Notify(string eventName, Atom node)
    {
        lock (gate)
        {
            Term message = Term.Tuple(Term.A(eventName), node);
            foreach (var subscriber in subscribers.ToArray())
                if (runtime.IsOwnedLiveNodeProcess(subscriber.Key))
                    for (int index = 0; index < subscriber.Value; index++)
                        runtime.SendSystem(subscriber.Key.Self, message);
        }
    }

    internal void ProcessTerminated(ProcessContext process)
    {
        lock (gate)
            subscribers.Remove(process);
    }

    internal void Close()
    {
        lock (gate)
        {
            closed = true;
            subscribers.Clear();
        }
    }
}
