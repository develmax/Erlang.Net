namespace Erlang;

internal sealed partial class ProcessTracing
{
    internal Term RefuseNamedMonitor425(Term descriptor)
    {
        lock (gate)
        {
            try
            {
                // The actual owner resolver validates strong resource ownership,
                // matching weak metadata, session liveness and directory membership.
                _ = ResolveTraceSession425(descriptor, false);
            }
            catch (TraceValidation425)
            {
                // system_monitor has plain BADARG in the pinned source; it does
                // not reuse trace_info's session error_info metadata.
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            throw new NotSupportedException(InternalProcessMonitorNames425.Unsupported);
        }
    }
}
