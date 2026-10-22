namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal int AuxiliaryParticipants430()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);

            // Each registered lane owns a real Auxiliary420 queue and executes
            // its callbacks. Online changes do not create/remove participants.
            // There is no extra auxiliary owner in this backend and no claim
            // that ThreadPool pumps are dedicated ERTS OS threads.
            return lanes.Length;
        }
    }
}
