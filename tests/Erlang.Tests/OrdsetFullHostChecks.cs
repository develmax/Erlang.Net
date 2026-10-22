using Erlang.Differential;

namespace Erlang.Tests;

internal static class OrdsetFullHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(OrdsetFullHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(OrdsetFullHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), OrdsetFullHostNames.Fixture);
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(context => runtime.Modules.Call(context, artifact.Definition.Name, OrdsetFullHostNames.Entry).AsTask());
        if (!value.Equals(Term.A(OrdsetFullHostNames.Success)))
            throw new InvalidOperationException(OrdsetFullHostNames.Failure);
    }
}
