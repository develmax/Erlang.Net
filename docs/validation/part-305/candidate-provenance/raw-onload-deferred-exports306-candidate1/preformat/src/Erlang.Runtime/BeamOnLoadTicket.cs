namespace Erlang;

public sealed class BeamOnLoadTicket
{
    internal BeamOnLoadTicket(ModuleRegistry owner, PreparedBeamImage image, BeamPendingGeneration generation)
    {
        Owner = owner;
        Prepared = image;
        Generation = generation;
        CodeImage = image.ExecutionIdentity.Image;
    }

    internal ModuleRegistry Owner
    {
        get;
    }
    internal PreparedBeamImage Prepared
    {
        get;
    }
    internal BeamPendingGeneration Generation
    {
        get;
    }
    internal ModuleCodeImage CodeImage
    {
        get;
    }
    internal bool HasDeferredExports { get; set; } = true;

    internal BeamOnLoadPhase CurrentPhase
    {
        get; set;
    }
    internal ProcessHandle? CallbackOwner
    {
        get; set;
    }
    internal TaskCompletionSource<BeamOnLoadCallbackOutcome> OutcomeCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource CallbackUnwound { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task CallbackQuiescence => CallbackUnwound.Task;
    public string Module => Prepared.Name;
    public BeamOnLoadPhase Phase => Owner.PendingOnLoadPhase(this);
    public Task<BeamOnLoadCallbackOutcome> Outcome => OutcomeCompletion.Task;
}
