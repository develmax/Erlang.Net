using Erlang;

namespace HelloHybrid;

internal static class PlatformDemo
{
    public static async Task Verify(ProcessRuntime runtime)
    {
        Erlang.Generated.ErlangModule_platform_demo.Register(runtime.Modules);
        var expected = Term.Tuple(Term.A(PlatformDemoNames.Ready), Term.I(100), Term.List(Term.I(2), Term.I(3)));
        var checkedResult = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Term actual = await runtime.Modules.Call(context, PlatformDemoNames.Module, PlatformDemoNames.Run);
                if (!actual.Equals(expected))
                    throw new InvalidOperationException(actual.ToString());
                checkedResult.TrySetResult();

                return actual;
            }
            catch (Exception error)
            {
                checkedResult.TrySetException(error);
                throw;
            }
        });
        await checkedResult.Task;
        Term reason = await process.Completion;
        if (!reason.Equals(Term.A(PlatformDemoNames.Normal)))
            throw new InvalidOperationException(reason.ToString());
    }
}
