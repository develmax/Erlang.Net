namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    private void CleanupPublication597(
        ProcessContext context,
        ReCode411? producer,
        ReMagicHost597? failedHost,
        Exception? primary
    )
    {
        Exception? hostFailure = null, producerFailure = null, exitFailure = null;
        try
        {
            failedHost?.Dispose();
        }
        catch (Exception error)
        {
            hostFailure = error;
        }
        try
        {
            if (producer is not null) ReleaseGuest(producer);
        }
        catch (Exception error)
        {
            producerFailure = error;
        }
        try
        {
            Exit(context);
        }
        catch (Exception error)
        {
            exitFailure = error;
        }
        if (hostFailure is not null || producerFailure is not null || exitFailure is not null)
            throw new AggregateException(new[] { primary, hostFailure, producerFailure, exitFailure }.OfType<Exception>());
    }
}
