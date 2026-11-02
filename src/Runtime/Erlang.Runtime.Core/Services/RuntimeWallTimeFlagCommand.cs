namespace Erlang;

internal sealed class RuntimeWallTimeFlagCommand(Pid caller, bool enabled) : RuntimeWallTimeCommand(caller)
{
    internal bool Enabled { get; } = enabled;
    internal TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal override void Fail(Exception error)
    {
        Result.TrySetException(error);
        _ = Result.Task.Exception;
        base.Fail(error);
        _ = Settled.Task.Exception;
    }

    internal override void Abandon(Exception error)
    {
        Result.TrySetException(error);
        _ = Result.Task.Exception;
        base.Abandon(error);
    }
}
