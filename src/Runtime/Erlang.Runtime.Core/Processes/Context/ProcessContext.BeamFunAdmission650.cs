namespace Erlang;

public sealed partial class ProcessContext
{
    private readonly AsyncLocal<BeamFunAdmissionSite650?> beamFunAdmissionSite650 = new();

    internal IDisposable EnterBeamFunAdmission650(FunctionTerm function)
    {
        var previous = beamFunAdmissionSite650.Value;
        beamFunAdmissionSite650.Value = new(function, CaptureExecutionChain());

        return new BeamFunAdmissionScope650(this, previous);
    }

    internal ErlangException CaptureBeamFunAdmissionFailure650(ErlangException error, FunctionTerm function)
    {
        var site = beamFunAdmissionSite650.Value;
        if (site is null || !ReferenceEquals(site.Function, function) || site.Frame is not { } caller)
            return error;

        // A rejected call_fun has an error-site frame in addition to its saved
        // caller continuation. Capture before the caller's handler unwinds.
        return RuntimeExceptionCapture.Source(this, error, caller with
        {
            Parent = caller,
            ContinuationIdentity = null
        });
    }

    internal void CompleteBeamFunAdmission650(FunctionTerm function)
    {
        if (beamFunAdmissionSite650.Value is { } site && ReferenceEquals(site.Function, function))
            beamFunAdmissionSite650.Value = null;
    }

    private sealed class BeamFunAdmissionScope650(ProcessContext context, BeamFunAdmissionSite650? previous) : IDisposable
    {
        public void Dispose() => context.beamFunAdmissionSite650.Value = previous;
    }
}
