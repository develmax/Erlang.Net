namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool IsSystemProcess(Pid pid)
    {
        lock (gate)
        {
            if (pid.LocalIdentity is not null && !ReferenceEquals(pid.LocalIdentity, LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (processes.TryGetValue(pid, out ProcessContext? context))
                return context.IsSystemProcess;
            // The distribution-entry index retains a single live entry per node name,
            // including old creation IDs. Creation is not the native external-PID test.
            if (pid.LocalIdentity is not null || pid.Node == Node)
                return false;

            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
}
