using System.Text;

namespace Erlang.Tests;

internal static class CodeServerBootstrap410LifecycleChecks
{
    internal static async Task Run()
    {
        await using (var unconfigured = new ProcessRuntime())
        {
            bool refused = false;
            try
            {
                _ = await unconfigured.StartOtpBootAsync([]);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && unconfigured.WhereIs(CodeServerOriginal410Names.Init).Equals(unconfigured.InitializationPid));
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            bool observed = false;
            try
            {
                _ = await unconfigured.StartOtpBootAsync([], canceled.Token);
            }
            catch (OperationCanceledException)
            {
                observed = true;
            }
            Require(observed && unconfigured.IsAlive(unconfigured.InitializationPid)
                && unconfigured.WhereIs(CodeServerOriginal410Names.Init).Equals(unconfigured.InitializationPid));
        }
        await using ProcessRuntime runtime = await CodeServerRuntime410Checks.Create();
        string output = Path.GetFullPath(Path.Combine(CodeServerOriginal410Names.OutputRoot,
            Guid.NewGuid().ToString(CodeServerOriginal410Names.GuidFormat)));
        Directory.CreateDirectory(output);
        BitString[] arguments = CodeServerOriginal410Fixture.Create(output);
        Term id = Term.Tuple(
            CodeServerOriginal410Fixture.Text(CodeServerBootstrap410LifecycleNames.CancelledBoot),
            CodeServerOriginal410Fixture.Text(CodeServerOriginal410Names.Version)
        );
        Term script = Term.Tuple(
            Term.A(CodeServerOriginal410Names.Script),
            id,
            Term.List(
            Term.Tuple(
                Term.A(CodeServerOriginal410Names.Apply),
                Term.Tuple(Term.A(CodeServerOriginal410Names.Init),
                Term.A(CodeServerBootstrap410LifecycleNames.WaitUntilStarted), Nil.Value)
            ))
        );
        File.WriteAllBytes(
            Path.Combine(output, CodeServerOriginal410Names.BootBase + CodeServerOriginal410Names.BootExtension),
            ExternalTermFormat.Encode(script)
        );
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        Task<RuntimeOtpBootSession> startup = runtime.StartOtpBootAsync(arguments, cancellation.Token);
        DateTime deadline = DateTime.UtcNow.AddSeconds(4);
        while (runtime.WhereIs(CodeServerOriginal410Names.Init) is not Pid selected || selected.Equals(runtime.InitializationPid))
        {
            Require(DateTime.UtcNow < deadline && !startup.IsCompleted);
            await Task.Delay(1);
        }
        Term processes;
        await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
        {
            while (true)
            {
                Term actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
                    CodeServerOriginal410Names.Init, CodeServerBootstrap410LifecycleNames.ScriptId));
                if (actual.Equals(id)) break;
                Require(DateTime.UtcNow < deadline && !startup.IsCompleted);
            }
            processes = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
                CodeServerOriginal410Names.Erts, CodeServerBootstrap410DiagnosticNames.Processes));
        }
        Require(!startup.IsCompleted);
        cancellation.Cancel();
        bool stopped = false;
        try
        {
            _ = await startup;
        }
        catch (OperationCanceledException)
        {
            stopped = true;
        }
        Require(stopped && Cons.Items(processes).Cast<Pid>().All(pid => !runtime.IsAlive(pid)));
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(CodeServerBootstrap410LifecycleNames.Missing);
    }
}
