namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal ValueTask<Term> ConnectOrdered455(
        ProcessContext caller,
        Term identity,
        Term destination,
        DriverParentPeerTransport422 transport
    )
    {
        Pid? target = destination as Pid;
        Task<Term> operation;
        lock (gate)
        {
            var row = Require(identity);
            operation = EnqueueSignal454(
                row,
                caller.Self,
                false,
                async () =>
            {
                lock (gate)
                {
                    _ = Require(row.Identity);
                    if (target is null || !runtime.DriverTargetInternal422(target)
                        || !runtime.AddPortLink(target, row.Identity)) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    row.Links.Add(target);
                }
                await transport.SetConnected(row.Native, target).ConfigureAwait(false);
                lock (gate)
                {
                    if (!row.Closing && !row.PhysicallyStopped) row.Connected = target;
                }

                return Term.A(ErlangBooleanAtoms.True);
            }
            );
        }

        return new(operation.WaitAsync(caller.Cancellation));
    }
}
