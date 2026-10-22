namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool ReadDirtyExecution423(ProcessContext caller, Term argument)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(caller);
            caller.Cancellation.ThrowIfCancellationRequested();
            if (argument is not Pid pid || !ReferenceEquals(pid.LocalIdentity, LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return processes.TryGetValue(pid, out var target) && dirtyNative419 is not null && dirtyNative419.IsExecuting423(target);
        }
    }
}
