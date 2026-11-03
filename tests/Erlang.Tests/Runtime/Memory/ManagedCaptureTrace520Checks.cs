namespace Erlang.Tests;

internal static class ManagedCaptureTrace520Checks
{
    internal static async Task TraceFailuresNeverSkipRetirement()
    {
        var primary = new InvalidOperationException(nameof(TraceFailuresNeverSkipRetirement));
        var start = new IOException(nameof(ManagedCaptureTrace520Checks));
        var retirement = new IOException(nameof(ManagedCaptureCleanup520));
        var artifact = new IOException(nameof(ManagedCaptureAuthority520));
        var end = new IOException(nameof(ManagedCaptureTrace520Names));
        int attempts = 0;
        try
        {
            await ManagedCaptureCleanup520.DisposeAsync(
                primary,
                () =>
 {
     attempts++;

     return Task.FromException(retirement);
 },
                new Action[] { () =>
 {
 attempts++;
 throw artifact;
 }, () => attempts++ },
                () =>
 {
     attempts++;
     throw start;
 },
                clean =>
 {
     Check(!clean);
     attempts++;
     throw end;
 }
            );
            throw new InvalidOperationException();
        }
        catch (AggregateException failure)
        {
            Check(attempts == 5 && failure.InnerExceptions.Count == 5);
            Check(ReferenceEquals(failure.InnerExceptions[0], primary));
            Check(ReferenceEquals(failure.InnerExceptions[1], start));
            Check(ReferenceEquals(failure.InnerExceptions[2], retirement));
            Check(ReferenceEquals(failure.InnerExceptions[3], artifact));
            Check(ReferenceEquals(failure.InnerExceptions[4], end));
        }
    }

    internal static async Task TraceFailureDoesNotMislabelCleanRetirement()
    {
        var primary = new InvalidOperationException(nameof(TraceFailureDoesNotMislabelCleanRetirement));
        var start = new IOException(nameof(ManagedCaptureTrace520Checks));
        bool retired = false;
        bool reportedClean = false;
        try
        {
            await ManagedCaptureCleanup520.DisposeAsync(
                primary,
                () =>
 {
     retired = true;

     return Task.CompletedTask;
 },
                Array.Empty<Action>(),
                () => throw start,
                clean => reportedClean = clean
            );
            throw new InvalidOperationException();
        }
        catch (AggregateException failure)
        {
            Check(retired && reportedClean && failure.InnerExceptions.Count == 2);
            Check(ReferenceEquals(failure.InnerExceptions[0], primary));
            Check(ReferenceEquals(failure.InnerExceptions[1], start));
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(nameof(ManagedCaptureTrace520Checks));
    }
}
