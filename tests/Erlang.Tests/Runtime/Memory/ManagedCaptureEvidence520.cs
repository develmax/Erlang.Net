namespace Erlang.Tests;

internal static class ManagedCaptureEvidence520
{
    internal static async Task RequireReturnedAsync(Func<Task> writeWitness, bool returned)
    {
        try
        {
            await writeWitness();
        }
        catch (Exception diagnostic)
        {
            if (!returned)
                throw new AggregateException(
                    new InvalidOperationException(ManagedContractBatch513Names.WorkerUnexpectedFailure),
                    diagnostic
                );

            throw;
        }
        if (!returned)
            throw new InvalidOperationException(ManagedContractBatch513Names.WorkerUnexpectedFailure);
    }
}
