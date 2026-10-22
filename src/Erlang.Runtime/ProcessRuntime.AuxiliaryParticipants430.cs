namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal int AuxiliaryParticipants430(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();

            return actorDispatcher.AuxiliaryParticipants430();
        }
    }
}
