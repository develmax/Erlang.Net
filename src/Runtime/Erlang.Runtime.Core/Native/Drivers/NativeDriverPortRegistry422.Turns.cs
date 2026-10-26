namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    private Task DispatchTurn(ErlDriverPortIdentity420 identity, Func<Task> callback)
    {
        lock (gate)
        {
            var row = Find(identity);
            if (row is null || row.PhysicallyStopped)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task task = TurnAfter(
                row,
                row.NativeTurns,
                accepted.Task,
                callback
            );
            try
            {
                Track(task);
            }
            catch
            {
                accepted.TrySetCanceled();
                throw;
            }
            row.NativeTurns = task;
            accepted.TrySetResult();

            return task;
        }
    }

    private async Task TurnAfter(
        NativeDriverPortRow422 row,
        Task previous,
        Task accepted,
        Func<Task> callback
    )
    {
        await accepted.ConfigureAwait(false);
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch (ErlangException) { /* A source refusal is not a transport failure. */ }
        await runtime.DispatchDriverPort422(
            dispatcher,
            async () =>
        {
            var turn = new DriverPortTurn422();
            lock (gate)
            {
                if (row.Turn is not null) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                row.Turn = turn;
            }
            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                lock (gate)
                {
                    if (!ReferenceEquals(row.Turn, turn)) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    row.Turn = null;
                }
                runtime.Statistics.RecordReductions(turn.Charged);
            }
        }
        ).ConfigureAwait(false);
    }
}
