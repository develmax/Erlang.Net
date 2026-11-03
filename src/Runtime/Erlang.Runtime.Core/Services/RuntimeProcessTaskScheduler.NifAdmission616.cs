namespace Erlang;

internal sealed partial class RuntimeProcessTaskScheduler
{
    // Return the real task so NIF admission can observe rejected/faulted queue
    // delivery. Existing fire-and-forget Continue callers remain unchanged.
    internal Task ContinueNifAdmission616(RuntimeActorContinuation request) => Task.Factory.StartNew(
        static value => ((RuntimeActorContinuation)value!).Body(),
        request,
        CancellationToken.None,
        TaskCreationOptions.DenyChildAttach,
        this
    );
}
