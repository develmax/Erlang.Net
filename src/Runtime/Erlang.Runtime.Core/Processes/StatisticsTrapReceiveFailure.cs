using System.Runtime.ExceptionServices;

namespace Erlang;

// Ephemeral admitted request only; no recorder/worker permanent backlink.
internal sealed class StatisticsTrapReceiveFailure
{
    private readonly TaskCompletionSource<ExceptionDispatchInfo> failure = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<ExceptionDispatchInfo> Failure => failure.Task;

    internal void Record(Exception error) => failure.TrySetResult(ExceptionDispatchInfo.Capture(error));

    internal void Observe(Task operation)
    {
        _ = operation.ContinueWith(
            completed =>
        {
            if (completed.IsFaulted)
                // Wake with one ACTUAL error. Finish joins the entire task and
                // preserves ALL distinct real failures; a Task wrapper is not a
                // new independent failure and must not win the EDI race.
                Record(completed.Exception!.InnerExceptions[0]);
            else if (completed.IsCanceled)
                Record(new TaskCanceledException(completed));
        },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    internal void ThrowIfFailed()
    {
        if (failure.Task.IsCompletedSuccessfully)
            failure.Task.Result.Throw();
    }
}
