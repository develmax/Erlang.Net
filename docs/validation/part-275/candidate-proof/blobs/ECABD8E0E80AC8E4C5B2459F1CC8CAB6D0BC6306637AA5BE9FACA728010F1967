namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ProcessContext RequireDistributionControllerTarget(Pid pid)
    {
        lock (gate)
        {
            if (pid.LocalIdentity is not null && !ReferenceEquals(pid.LocalIdentity, LocalIdentity) ||
                !IsCurrentLocal(pid) || !processes.TryGetValue(pid, out var process) || !IsOwnedLiveNodeProcess(process))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return process;
        }
    }
}
