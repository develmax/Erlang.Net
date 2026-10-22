using Erlang.Differential;

namespace Erlang.Tests;

internal static class OrdsetCoreHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(OrdsetCoreHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(OrdsetCoreHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), OrdsetCoreHostNames.Fixture);
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(context => runtime.Modules.Call(context, artifact.Definition.Name, OrdsetCoreHostNames.Entry).AsTask());
        if (!value.Equals(Term.A(OrdsetCoreHostNames.Success)))
            throw new InvalidOperationException(OrdsetCoreHostNames.Failure);
    }
}
