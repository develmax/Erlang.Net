namespace Erlang.Tests;

internal static class RegexNative411LifecycleChecks
{
    internal static async Task RealCancellation()
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var process = runtime.Spawn(async context =>
            {
                var pattern = RegexNative411Access.Pattern(await RegexNative411Access.Call(context, RegexNative411Names.Compile, RegexNative411Names.Bytes(RegexNative411Names.BacktrackPattern)));
                ready.TrySetResult();

                return await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    RegexNative411Names.Bytes(new string('a', 500000) + RegexNative411Names.BacktrackSuffix),
                    pattern
                );
            });
            await ready.Task.WaitAsync(RegexNative411Names.Deadline);
            using var deadline = new CancellationTokenSource(RegexNative411Names.Deadline);
            while (engine.Report.Yields == 0 || engine.Report.Active == 0)
            {
                RegexNative411Access.Require(!process.Completion.IsCompleted);
                await Task.Delay(1, deadline.Token);
            }
            RegexNative411Access.Require(!process.Completion.IsCompleted);
            await using var killer = new RuntimeNodeFoundationHostActor(runtime);
            await killer.Invoke(context =>
 {
     RegexNative411Access.Require(!process.Completion.IsCompleted && engine.Report.Active > 0);
     runtime.Exit(context, process.Pid, Term.A(RegexNative411Names.Kill));

     return true;
 });
            var reason = await process.Completion.WaitAsync(RegexNative411Names.Deadline);
            RegexNative411Access.Require(reason.Equals(Term.A(RegexNative411Names.Killed)));
            var report = engine.Report;
            RegexNative411Access.Require(report.Active == 0 && report.MatchDataCreated > 0 && report.MatchDataCreated == report.MatchDataFreed);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        RegexNative411Access.Empty(engine);
    }

    internal static Task DisposeClose() => ClosePath(0);

    internal static Task HaltClose() => ClosePath(1);

    internal static Task InitializerClose() => ClosePath(2);

    private static async Task ClosePath(int mode)
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        Term? retained = null;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            retained = RegexNative411Access.Pattern(await RegexNative411Access.Call(context, RegexNative411Names.Compile, RegexNative411Names.Bytes(RegexNative411Names.SingleValue)));

            return true;
        });
        if (mode == 0) await runtime.DisposeAsync();
        else if (mode == 1)
        {
            await runtime.RequestImmediateHalt(0);
            await runtime.HaltQuiescence.WaitAsync(RegexNative411Names.Deadline);
        }
        else
        {
            await actor.Invoke(context =>
 {
     runtime.Exit(context, runtime.InitializationPid, Term.A(RegexNative411Names.Kill));

     return true;
 });
            await runtime.DisposeAsync();
        }
        RegexNative411Access.Empty(engine);
        GC.KeepAlive(retained);
    }
}
