namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Task OwnedTermProcessRetirement506(ProcessContext context, Task preceding)
    {
        // Terminate holds R: stop admission without taking mailbox M.
        context.Mailbox.StopAdmissionForExit606();
        // This is the specific pre-existing queue-observation fence. The
        // preceding argument is the DIRTY physical chain, not ETS storage.
        // Waiting all storage/physical work before releasing queue roots
        // could create an ownership cycle.
        Task queueObservation606 = EtsQueueRetirement419(context, Task.CompletedTask);
        Task queueRoots593 = context.Mailbox.DrainAfterQueueObservations606(queueObservation606);
        if (!OwnedTermsPilot506)
            return Task.WhenAll(preceding, queueRoots593);

        // Terminate has already removed this process under the runtime gate.
        // Close dictionary admission, retaining every active invocation/borrow.
        // Return the physical wait; logical cleanup and DOWN do not await it.
        context.BeginOwnedTermRetirement506();

        return Task.WhenAll(preceding, queueRoots593, context.OwnedTermRetirement506);
    }
}
