namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term DflagUnicodeIo(Term target)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, nameof(ProcessRuntime));
            ThrowIfHalted();
            if (target is not Pid pid || pid.LocalIdentity is not null &&
                !ReferenceEquals(pid.LocalIdentity, LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            // OTP compares the node-level DistEntry, not PID liveness/creation.
            if (ReferenceEquals(pid.LocalIdentity, LocalIdentity) || pid.Node == Node)
                return Term.A(ErlangBooleanAtoms.True);

            bool supported = distributionPending?.HasUnicodeIo(pid.Node) == true;

            return Term.A(supported ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
        }
    }
}
