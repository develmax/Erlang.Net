namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Immutable dispatcher identity only: no runtime.gate acquisition from dispatcher.gate.
    internal bool OwnsActorDispatcher428(RuntimeActorDispatcher dispatcher)
        => ReferenceEquals(actorDispatcher, dispatcher);
}
