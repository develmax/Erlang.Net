namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireProcessInfoTarget(Pid target)
    {
        lock (gate)
        {
            if (target.LocalIdentity is not null)
            {
                if (!ReferenceEquals(target.LocalIdentity, LocalIdentity))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return;
            }
            if (target.WireIdentity.Node != LocalIdentity.Snapshot.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
}
