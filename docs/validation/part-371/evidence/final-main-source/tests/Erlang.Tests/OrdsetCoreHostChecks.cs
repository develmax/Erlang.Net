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
        foreach (int length in new[] { 1, 2, OrdsetCoreHostNames.LongPrefixLength })
        {
            Term improper = Term.A(OrdsetCoreHostNames.Tail);
            for (int index = length; index > 0; index--)
                improper = new Cons(Term.I(index), improper);
            Term trace = await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                artifact.Definition.Name,
                OrdsetCoreHostNames.Trace,
                [Term.A(OrdsetCoreHostNames.Element), improper]
            ).AsTask());
            if (trace is not TupleTerm { Items.Count: 3 } failure
                || !failure.Items[0].Equals(Term.A(ErlangExceptionClasses.Error))
                || !failure.Items[1].Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                throw new InvalidOperationException(OrdsetCoreHostNames.Failure);
            Term[] frames = Cons.Items(failure.Items[2]).ToArray();
            if (frames.Length < 3
                || frames[1] is not TupleTerm { Items.Count: 4 } pending
                || !pending.Items[0].Equals(Term.A(OrdsetCoreHostNames.Module))
                || !pending.Items[1].Equals(Term.A(OrdsetCoreHostNames.AddElement))
                || !pending.Items[2].Equals(Term.I(2))
                || frames[2] is not TupleTerm { Items.Count: 4 } caller
                || !caller.Items[0].Equals(Term.A(artifact.Definition.Name))
                || !caller.Items[1].Equals(Term.A(OrdsetCoreHostNames.Trace)))
                throw new InvalidOperationException(OrdsetCoreHostNames.Failure);
        }
    }
}
