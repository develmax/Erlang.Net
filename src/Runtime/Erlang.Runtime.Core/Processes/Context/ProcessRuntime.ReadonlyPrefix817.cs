namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Invoked only after acquiring the real signal owner. Never reenter from
    // retained R/M, never prepare backing while those gates are held.
    private bool TryAdmitReadonlyPrefix817(ProcessContext owner)
    {
        if (System.Threading.Monitor.IsEntered(gate) || owner.Mailbox.DeferredGateHeld
            || ReadonlyTraceOutputScope817.HasActiveScope817) return false;
        while (true)
        {
            ProcessContext? tracer;
            ReadonlyTracePrefix817? prefix;
            lock (gate)
            {
                if (owner.Terminated || IsHaltRequested) return false;
                tracer = ProcessTracing.CaptureReadonlyReceiveRoute817(owner);
                if (tracer is null) return false;
                prefix = owner.Mailbox.CaptureReadonlyPrefix817(owner);
            }
            if (prefix is null) return false;
            Exception? primary = null;
            try
            {
                if (!prefix.Prepare(this, owner, tracer).GetAwaiter().GetResult()) return false;
                lock (gate)
                {
                    if (owner.Terminated || IsHaltRequested) return false;
                    if (!ReferenceEquals(ProcessTracing.CaptureReadonlyReceiveRoute817(owner), tracer)
                        || !owner.Mailbox.IsCurrentReadonlyPrefix817(owner, prefix)) continue;
                    owner.Mailbox.AdmitReadonlyPrefix817(ProcessTracing, prefix);

                    return true;
                }
            }
            catch (Exception error)
            {
                primary = error;
                throw;
            }
            finally
            {
                try
                {
                    prefix.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch (Exception cleanup) when (primary is not null)
                {
                    throw new AggregateException(primary, cleanup);
                }
            }
        }
    }
}
