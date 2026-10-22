using Erlang.Differential;

namespace Erlang.Tests;

internal static class DynamicApplyHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(DynamicApplyHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(DynamicApplyHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), DynamicApplyHostNames.Fixture);
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(context => runtime.Modules.Call(context, artifact.Definition.Name, DynamicApplyHostNames.Entry).AsTask());
        Require(value is TupleTerm);
        var rows = ((TupleTerm)value).Items;
        Require(rows.Count == DynamicApplyHostNames.CapturedCases + 1 && rows[^1].Equals(Term.A(DynamicApplyHostNames.Success)));
        for (int index = 0; index < DynamicApplyHostNames.CapturedCases; index++)
        {
            Require(rows[index] is TupleTerm { Items.Count: 3 });
            var capture = (TupleTerm)rows[index];
            Term[] frames = Cons.Items(capture.Items[2]).ToArray();
            Require(frames.Length != 0 && frames.All(frame => frame is TupleTerm { Items.Count: 4 }));
            string caller = index == DynamicApplyHostNames.CapturedCases - 1 ? DynamicApplyHostNames.NonTail : DynamicApplyHostNames.Tail;
            bool retained = frames.Cast<TupleTerm>().Any(frame => frame.Items[0].Equals(Term.A(DynamicApplyHostNames.Module)) && frame.Items[1].Equals(Term.A(caller)));
            Require(retained == (index != 0));
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DynamicApplyHostNames.Failure);
    }
}
