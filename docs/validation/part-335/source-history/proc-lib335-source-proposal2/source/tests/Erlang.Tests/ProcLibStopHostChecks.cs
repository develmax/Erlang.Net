using Erlang.Otp;
using Erlang.Differential;

namespace Erlang.Tests;

public static class ProcLibStopHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var callback = new CounterServer();
        var server = await GenServer.Start(runtime, callback, Term.I(0));
        var client = runtime.Spawn(async context =>
        {
            Term result = await GenServer.Stop(context, server.Pid, Term.A(ProcessExitReasons.Normal));
            if (!result.Equals(Term.A(OracleOutcomeTags.Success)))
                throw new InvalidOperationException(ProcLibStopCaseNames.Mismatch);
            if (await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                throw new InvalidOperationException(ProcLibStopCaseNames.Mismatch);

            return result;
        });
        Term clientReason = await client.Completion;
        Term serverReason = await server.Completion;
        if (!clientReason.Equals(Term.A(ProcessExitReasons.Normal)) ||
            !serverReason.Equals(Term.A(ProcessExitReasons.Normal)) || !callback.Terminated)
            throw new InvalidOperationException(ProcLibStopCaseNames.Mismatch);
    }
}
