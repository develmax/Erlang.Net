namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool IsEtsInternalPid419(Pid pid) => ReferenceEquals(pid.LocalIdentity, LocalIdentity);

    // Same gate as table publication; a weak generation never keeps a dead actor alive.
    internal ProcessContext? ReadEtsHeirProcess419(Pid pid)
    {
        lock (gate)
            return !disposed && !IsHaltRequested && IsEtsInternalPid419(pid)
             && processes.TryGetValue(pid, out var target) && !target.Terminated ? target : null;
    }

    internal ProcessContext? ReadEtsHeirProcess419(RuntimeEtsHeir419 heir)
    {
        lock (gate)
        {
            var target = ReadEtsHeirProcess419(heir.Pid);

            return target is not null && heir.Generation.TryGetTarget(out var generation) && ReferenceEquals(target, generation) ? target : null;
        }
    }
}
