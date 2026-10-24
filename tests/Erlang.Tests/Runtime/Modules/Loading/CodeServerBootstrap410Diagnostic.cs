using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeServerBootstrap410Diagnostic
{
    internal static async Task Capture(ProcessRuntime runtime, string output)
    {
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term pids = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
            CodeServerOriginal410Names.Erts, CodeServerBootstrap410DiagnosticNames.Processes));
        var rows = new List<object>();
        foreach (Term pid in Cons.Items(pids))
        {
            Term info = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerOriginal410Names.Erts,
                CodeServerBootstrap410DiagnosticNames.ProcessInfo,
                pid,
                Cons.From(CodeServerBootstrap410DiagnosticNames.Keys.Select(Term.A))
            ));
            rows.Add(new { Pid = OwnedFunctionOriginal407Terms.Typed(pid), Info = OwnedFunctionOriginal407Terms.Typed(info) });
        }
        File.WriteAllText(Path.Combine(output, CodeServerBootstrap410DiagnosticNames.File), JsonSerializer.Serialize(rows));
    }
}
