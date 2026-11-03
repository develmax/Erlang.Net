namespace Erlang.Tests;

internal static class ManagedCaptureEvidence520Checks
{
    internal static async Task CleanupFailuresRetainPrimary()
    {
        var primary = new InvalidOperationException(nameof(CleanupFailuresRetainPrimary));
        var retirement = new IOException(nameof(ManagedCaptureCleanup520));
        var artifact = new InvalidDataException(nameof(ManagedCaptureEvidence520Checks));
        int attempts = 0;
        try
        {
            await ManagedCaptureCleanup520.DisposeAsync(
                primary,
                () => Task.FromException(retirement),
                new Action[] { () =>
 {
 attempts++;
 throw artifact;
 }, () => attempts++ }
            );
            throw new InvalidOperationException();
        }
        catch (AggregateException failure)
        {
            Check(attempts == 2 && failure.InnerExceptions.Count == 3);
            Check(ReferenceEquals(failure.InnerExceptions[0], primary));
            Check(ReferenceEquals(failure.InnerExceptions[1], retirement));
            Check(ReferenceEquals(failure.InnerExceptions[2], artifact));
        }
        await ManagedCaptureCleanup520.DisposeAsync(primary, null, Array.Empty<Action>());
    }

    internal static async Task WitnessAttemptPrecedesFailure()
    {
        int writes = 0;
        try
        {
            await ManagedCaptureEvidence520.RequireReturnedAsync(() =>
 {
     writes++;

     return Task.CompletedTask;
 }, false);
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException failure)
        {
            Check(writes == 1 && failure.Message == ManagedContractBatch513Names.WorkerUnexpectedFailure);
        }
        var diagnostic = new IOException(nameof(WitnessAttemptPrecedesFailure));
        try
        {
            await ManagedCaptureEvidence520.RequireReturnedAsync(() => Task.FromException(diagnostic), false);
            throw new InvalidOperationException();
        }
        catch (AggregateException failure)
        {
            Check(failure.InnerExceptions.Count == 2);
            Check(failure.InnerExceptions[0].Message == ManagedContractBatch513Names.WorkerUnexpectedFailure);
            Check(ReferenceEquals(failure.InnerExceptions[1], diagnostic));
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(nameof(ManagedCaptureEvidence520Checks));
    }
}
