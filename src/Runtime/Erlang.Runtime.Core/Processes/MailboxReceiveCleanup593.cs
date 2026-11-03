namespace Erlang;

internal static class MailboxReceiveCleanup593
{
    internal static void Dispose(
        IDisposable? selection,
        IDisposable? candidate,
        IDisposable? operation,
        Exception? primary,
        IDisposable? otherOperation = null
    )
    {
        Exception? first = null, second = null, third = null, fourth = null;
        try
        {
            selection?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            candidate?.Dispose();
        }
        catch (Exception error)
        {
            second = error;
        }
        try
        {
            operation?.Dispose();
        }
        catch (Exception error)
        {
            third = error;
        }
        try
        {
            otherOperation?.Dispose();
        }
        catch (Exception error)
        {
            fourth = error;
        }
        if (first is not null || second is not null || third is not null || fourth is not null)
            throw new AggregateException(new[] { primary, first, second, third, fourth }.OfType<Exception>());
    }
}
