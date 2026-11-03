namespace Erlang.Tests;

internal static class CountersOwnerAccounting683Checks
{
    internal static async Task AwaitEmpty(ProcessRuntime runtime, TimeSpan deadline)
    {
        using var limit = new CancellationTokenSource(deadline);
        // Array retirement signals physical free before registry unlink and
        // owner accounting finish. Observe both actual states before VM close.
        while (true)
        {
            RuntimeCountersReport421 report = runtime.CountersStorage421;
            if (report.LiveArrays == 0 && report.OwnedNativeBytes == 0
                && report.ActiveInvocations == 0 && report.Created == report.Freed)
                return;

            await Task.Delay(CountersOwnerAccounting683Names.PollInterval, limit.Token);
        }
    }
}
