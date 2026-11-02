namespace Erlang;

public sealed partial class ProcessRuntime
{
    private static async ValueTask WaitForSchedulerObservation(ProcessContext context, Task completion)
    {
        bool previous = context.WaitingForMessage;
        context.WaitingForMessage = true;
        try
        {
            Exception? waitFailure = null;
            try
            {
                await context.Mailbox.WaitForControlCompletion(completion, context.Cancellation);
            }
            catch (Exception error)
            {
                waitFailure = error;
            }
            try
            {
                // A process exit/control failure cannot abandon accepted work.
                // Only actual callback settlement releases this wait; no guest
                // work or thread-affine scope is executed off the actor here.
                await completion.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                Exception failure = waitFailure is null || ReferenceEquals(waitFailure, error)
                    ? error : new AggregateException(waitFailure, error);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
            if (waitFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(waitFailure).Throw();
        }
        finally
        {
            context.WaitingForMessage = previous;
        }
    }
}
