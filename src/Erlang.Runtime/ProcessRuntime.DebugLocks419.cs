namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool HasDisabledLockCounting419 => !DebugLockNames419.BuildHasLockCounting;

    internal void RequireDebugLocksActor419(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugLockNames419.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!HasDisabledLockCounting419) throw new InvalidOperationException(DebugLockNames419.WrongProfile);
        }
    }
}
