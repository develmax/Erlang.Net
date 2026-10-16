using Erlang.Differential;

namespace Erlang.Tests;

public static class RemoteSourceFunLexicalHostChecks
{
    public static async Task FreshReceiverLexicalCaptureBoundaries()
    {
        using var sourceA = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.LexicalSource);
        using var sourceB = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.LexicalSource);
        Require(!ReferenceEquals(sourceA.Definition, sourceB.Definition));
        await using var origin = new ProcessRuntime();
        await using var target = new ProcessRuntime();
        Require(origin.Modules.HasExport(ErlangModuleNames.Module, ModuleCodeLifecycleNames.PurgeModule, 1));
        Require(target.Modules.HasExport(ErlangModuleNames.Module, ModuleCodeLifecycleNames.PurgeModule, 1));
        sourceA.Definition.Register(origin.Modules);
        sourceB.Definition.Register(target.Modules);
        await using var sender = new RuntimeNodeFoundationHostActor(origin);
        await using var receiver = new RuntimeNodeFoundationHostActor(target);
        var callbacks = (TupleTerm)await sender.InvokeAsync(async context => Term.Tuple(
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.LexicalModule,
                RemoteSourceFunHostNames.Make,
                Term.I(10)
            ),
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.LexicalModule,
                RemoteSourceFunHostNames.Make,
                Term.I(20)
            ),
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.LexicalModule,
                RemoteSourceFunHostNames.MakeShadow,
                Term.A(RemoteSourceFunHostNames.Outer)
            ),
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.LexicalModule,
                RemoteSourceFunHostNames.MakeMatch,
                Term.A(RemoteSourceFunHostNames.Outer)
            )
        ));
        // Target has only loaded code. No maker or constructor has run there;
        // every invocation therefore needs lexical metadata, not a warm factory.
        var restored = (TupleTerm)target.ImportExternalTerm(ExternalTermFormat.Encode(callbacks, new EtfEncodingOptions { FunctionOwner = sender.Process.Pid })).Term;
        await receiver.InvokeAsync(async context =>
        {
            var first = (FunctionTerm)restored.Items[0];
            var sibling = (FunctionTerm)restored.Items[1];
            var shadow = (FunctionTerm)restored.Items[2];
            var pattern = (FunctionTerm)restored.Items[3];
            Require(first.Environment.Count == 1 && sibling.Environment.Count == 1);
            Require(shadow.Environment.Count == 0 && pattern.Environment.Count == 1);
            Require((await first.Invoke(context, [Term.I(3)])).Equals(Term.Tuple(Term.I(10), Term.I(4))));
            Require((await sibling.Invoke(context, [Term.I(4)])).Equals(Term.Tuple(Term.I(20), Term.I(5))));
            Require((await shadow.Invoke(context, [Term.A(RemoteSourceFunHostNames.Inner)])).Equals(Term.A(RemoteSourceFunHostNames.Inner)));
            Require((await pattern.Invoke(context, [])).Equals(Term.A(RemoteSourceFunHostNames.Outer)));

            return true;
        });
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
