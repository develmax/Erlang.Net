namespace Erlang;

internal sealed partial class ProcessTracing
{
    private readonly Dictionary<ProcessTracePattern, RuntimeProcessPayload641> patternRoots778 = new(ReferenceEqualityComparer.Instance);
    private readonly List<RuntimeProcessPayload641> retiredPatternRoots778 = new();
    private readonly List<ProcessTracing> retiredPatternViews778 = new();
    private bool patternRootsClosed778;

    // Configuration prepares this independent context-null owner BEFORE R.
    // Only the actual registry installation transfers ownership here.
    private void InstallPatternRoot778(ProcessTracePattern pattern, RuntimeProcessPayload641 owner)
    {
        if (!System.Threading.Monitor.IsEntered(gate) || patternRootsClosed778)
            throw new InvalidOperationException(ProcessTraceRootNames778.Invalid);
        patternRoots778.EnsureCapacity(checked(patternRoots778.Count + 1));
        patternRoots778.Add(pattern, owner.Borrow641(null));
    }

    private RuntimeProcessPayload641? BorrowPatternRoot778(ProcessTracePattern? pattern)
    {
        if (!System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(ProcessTraceRootNames778.Invalid);

        return pattern is not null && patternRoots778.TryGetValue(pattern, out var owner) ? owner.Borrow641(null) : null;
    }

    // Called AFTER the replaced pattern has left all of this view's fields.
    // No native close/callback occurs under R.
    private void QueueUnusedPatternRoot778(ProcessTracePattern? pattern)
    {
        if (pattern is null || ReferenceEquals(sendPattern, pattern) || ReferenceEquals(receivePattern, pattern)
            || patterns.Values.Any(value => ReferenceEquals(value, pattern))) return;
        if (!patternRoots778.TryGetValue(pattern, out var owner)) return;
        retiredPatternRoots778.EnsureCapacity(checked(retiredPatternRoots778.Count + 1));
        retiredPatternRoots778.Add(owner);
        patternRoots778.Remove(pattern);
    }

    private void DetachLocalPatternRoots778()
    {
        retiredPatternRoots778.EnsureCapacity(checked(retiredPatternRoots778.Count + patternRoots778.Count));
        patterns.Clear();
        sendPattern = null;
        receivePattern = null;
        foreach (var pair in patternRoots778) retiredPatternRoots778.Add(pair.Value);
        patternRoots778.Clear();
    }

    // Root lifecycle calls this under its original final R only AFTER trace
    // producers are logically closed/prepared. Keep this ahead of session clear.
    // It atomically detaches native owners; it is not physical completion.
    internal void StopPatternRoots778()
    {
        lock (gate)
        {
            ProcessTracing[] views = LiveTraceViews425();
            retiredPatternViews778.EnsureCapacity(checked(retiredPatternViews778.Count + views.Length));
            // All fallible capacity growth precedes ANY destructive view/state change.
            retiredPatternRoots778.EnsureCapacity(checked(retiredPatternRoots778.Count + patternRoots778.Count));
            foreach (ProcessTracing view in views)
                view.retiredPatternRoots778.EnsureCapacity(checked(view.retiredPatternRoots778.Count + view.patternRoots778.Count));
            foreach (ProcessTracing view in views)
            {
                view.patternRootsClosed778 = true;
                view.DetachLocalPatternRoots778();
                retiredPatternViews778.Add(view);
            }
            patternRootsClosed778 = true;
            DetachLocalPatternRoots778();
        }
    }

    // Outside R. Releases every detached owner independently. Close641 schedules
    // actual native settlement in the EXISTING runtime641 owner/domain; it does
    // not wait for outstanding event borrows or acknowledge physical completion.
    // Existing VM payload/native drains subsequently JOIN all those completions.
    internal void RetirePatternRoots778()
    {
        runtime.AssertSequenceTracePreparation();
        var owners = new List<RuntimeProcessPayload641>();
        lock (gate)
        {
            int count = retiredPatternRoots778.Count;
            foreach (ProcessTracing view in retiredPatternViews778)
                count = checked(count + view.retiredPatternRoots778.Count + view.patternRoots778.Count);
            // No queue/dictionary is cleared until the ENTIRE detached batch fits.
            owners.EnsureCapacity(count);
            foreach (RuntimeProcessPayload641 owner in retiredPatternRoots778) owners.Add(owner);
            retiredPatternRoots778.Clear();
            foreach (ProcessTracing view in retiredPatternViews778)
            {
                // Closed-session views remain owned here even if an earlier local
                // detach failed before its dictionary was drained.
                foreach (var pair in view.patternRoots778) owners.Add(pair.Value);
                view.patternRoots778.Clear();
                view.patterns.Clear();
                view.sendPattern = null;
                view.receivePattern = null;
                foreach (RuntimeProcessPayload641 owner in view.retiredPatternRoots778) owners.Add(owner);
                view.retiredPatternRoots778.Clear();
            }
            retiredPatternViews778.Clear();
        }
        List<Exception>? failures = null;
        foreach (RuntimeProcessPayload641 owner in owners)
        {
            try
            {
                owner.Close641();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
