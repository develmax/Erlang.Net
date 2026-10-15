namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<ProcessInfoDefaultProjection> QueryDefaultLogicalProcessInfo(
        ProcessContext requester,
        Term target
    )
    {
        if (!ReferenceEquals(requester.Runtime, this) || target is not Pid pid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        RequireProcessInfoTarget(pid);
        Term snapshot = await QueryProcessInfo(
            requester,
            pid,
            ProcessInfoDefaultCatalog.LogicalItems,
            false
        );
        requester.Cancellation.ThrowIfCancellationRequested();
        if (snapshot.Equals(ProcessInfoAtoms.Undefined))
            return new(false, Array.Empty<Term>(), Array.Empty<string>());
        Term[] fields = Cons.Items(snapshot).Where(field => field is not TupleTerm
        { Items: [Atom { Name: ProcessInfoNames.RegisteredName }, Nil] }).ToArray();

        return new(
            true,
            Array.AsReadOnly(fields),
            ProcessInfoDefaultCatalog.MissingPhysicalKeys
        );
    }
}
