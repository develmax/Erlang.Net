namespace Erlang;

internal static class RuntimeMicrostateExecution
{
    [ThreadStatic] private static RuntimeMicrostateScope? current;
    internal static RuntimeMicrostateScope? Current => current;

    internal static RuntimeMicrostateScope Attach(
        RuntimeMicrostateAccounting owner,
        string type,
        RuntimeMicrostate state,
        RuntimeMicrostateThreadIdentity thread
    )
        => AttachConfiguredWorker(
            owner,
            type,
            state,
            thread,
            null
        );

    internal static RuntimeMicrostateScope AttachConfiguredWorker(
        RuntimeMicrostateAccounting owner,
        string type,
        RuntimeMicrostate state,
        RuntimeMicrostateThreadIdentity thread,
        int? configuredIdentity
    )
    {
        RuntimeMicrostateRecord record = owner.RegisterWorker(type, thread, configuredIdentity);
        RuntimeMicrostateScope? previous = current;
        RuntimeMicrostate? paused = previous?.State;
        if (previous?.Record is { } previousRecord) previousRecord.Owner.Pause(previousRecord);
        try
        {
            var scope = new RuntimeMicrostateScope(
                record,
                previous,
                paused,
                true
            );
            scope.State = state;
            owner.Resume(record, state);
            current = scope;

            return scope;
        }
        catch
        {
            if (previous?.Record is { } old && paused is { } restore) old.Owner.Resume(old, restore);
            throw;
        }
    }

    internal static RuntimeMicrostateScope? EnterState(RuntimeMicrostateAccounting owner, RuntimeMicrostate state)
    {
        if (current?.Record is not { } record || !ReferenceEquals(record.Owner, owner)) return null;
        var scope = new RuntimeMicrostateScope(
            record,
            current,
            null,
            false
        );
        scope.State = state;
        scope.PreviousState = current.State;
        owner.Change(record, state);
        current = scope;

        return scope;
    }

    internal static void Leave(RuntimeMicrostateScope scope)
    {
        if (!ReferenceEquals(current, scope) || scope.Record is not { } record
            || !ReferenceEquals(record.PhysicalThread, RuntimeMicrostateThreadIdentity.Current))
            throw new InvalidOperationException(RuntimeMicrostateNames.ThreadMismatch);
        if (scope.IsAttachment)
        {
            record.Owner.Pause(record);
            if (scope.Previous?.Record is { } old && scope.PreviousState is { } attachmentState)
                old.Owner.Resume(old, attachmentState);
        }
        else if (scope.PreviousState is { } priorState) record.Owner.Change(record, priorState);
        current = scope.Previous;
    }

    internal static void RefreshCurrent(RuntimeMicrostateAccounting owner)
    {
        if (current?.Record is { } record && ReferenceEquals(record.Owner, owner)
            && ReferenceEquals(record.PhysicalThread, RuntimeMicrostateThreadIdentity.Current))
            owner.Change(record, current.State);
    }

}
