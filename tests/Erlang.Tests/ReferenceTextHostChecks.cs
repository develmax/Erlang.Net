namespace Erlang.Tests;

public static class ReferenceTextHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        ReferenceTerm? last = null;
        for (int index = 0; index < ReferenceTextHostParameters.Allocations; index++)
        {
            last = runtime.MakeReference();
            if ((last.Words[1] & ReferenceTextHostParameters.SecondWordReservedMask) != 0)
                throw new InvalidOperationException(ReferenceTextHostDiagnostics.Carry);
        }
        if (last is null || last.Words[0] != 1 || last.Words[1] != ReferenceTextHostParameters.ExpectedCarry || last.Words[2] != 0)
            throw new InvalidOperationException(ReferenceTextHostDiagnostics.Carry);
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var text = await runtime.Modules.Call(
                    context,
                    ReferenceTextHostNames.Module,
                    ReferenceTextHostNames.ToList,
                    last
                );
                var parsed = await runtime.Modules.Call(
                    context,
                    ReferenceTextHostNames.Module,
                    ReferenceTextHostNames.FromList,
                    text
                );
                if (!last.Equals(parsed))
                    throw new InvalidOperationException(ReferenceTextHostDiagnostics.Roundtrip);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion.WaitAsync(ReferenceTextHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
