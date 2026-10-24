namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal bool TryAutoMonitorNode460(ProcessContext owner, Atom node)
    {
        lock (gate)
        {
            if (closed || !runtime.IsOwnedLiveNodeProcess(owner) || !DistributionNodeIdentity.IsValid(node))
                return false;
            if (TryMonitorNode458(
                owner,
                node,
                true,
                false
            ))
                return true;
            if (runtime.WhereIs(RuntimeNodeControlNames.Kernel) is not Pid kernel || !runtime.IsAlive(kernel))
                return false;
            TupleTerm handle;
            try
            {
                handle = BeginPending(node);
            }
            catch (InvalidOperationException failure) when (failure.Message == DistributionPendingDiagnostics.Capacity)
            {
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            }
            if (!TryMonitorNode458(
                owner,
                node,
                true,
                false
            ))
            {
                AbortPending(handle);

                return false;
            }
            runtime.SendSystem(kernel, Term.Tuple(Term.A(RuntimeNodeControlNames.AutoConnect), node, handle));

            return true;
        }
    }
}
