using Erlang.Differential;

namespace Erlang.Tests;

public static class RemoteSourceFunImageHostChecks
{
    public static async Task CurrentOldPurgedAndSiblingCaptures()
    {
        using var aCode = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.OldSource);
        using var bCode = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.OldSource);
        using var nextCode = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.NewSource);
        await using var origin = new ProcessRuntime();
        await using var target = new ProcessRuntime();
        Require(origin.Modules.HasExport(ErlangModuleNames.Module, ModuleCodeLifecycleNames.PurgeModule, 1));
        Require(target.Modules.HasExport(ErlangModuleNames.Module, ModuleCodeLifecycleNames.PurgeModule, 1));
        aCode.Definition.Register(origin.Modules);
        bCode.Definition.Register(target.Modules);
        await using var source = new RuntimeNodeFoundationHostActor(origin);
        await using var receiver = new RuntimeNodeFoundationHostActor(target);
        FunctionTerm first = (FunctionTerm)await source.InvokeAsync(async context =>
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.SourceModule,
                RemoteSourceFunHostNames.Make,
                Term.I(1)
            ));
        FunctionTerm second = (FunctionTerm)await source.InvokeAsync(async context =>
            await origin.Modules.Call(
                context,
                RemoteSourceFunHostNames.SourceModule,
                RemoteSourceFunHostNames.Make,
                Term.I(2)
            ));
        var restoredFirst = (FunctionTerm)target.ImportExternalTerm(ExternalTermFormat.Encode(first, new EtfEncodingOptions { FunctionOwner = source.Process.Pid })).Term;
        var restoredSecond = (FunctionTerm)target.ImportExternalTerm(ExternalTermFormat.Encode(second, new EtfEncodingOptions { FunctionOwner = source.Process.Pid })).Term;
        Require(restoredFirst.LocalIdentity!.Unique.Equals(first.LocalIdentity!.Unique));
        Require(restoredFirst.Environment.SequenceEqual(first.Environment));
        await AssertValue(receiver, restoredFirst, 1);
        await AssertValue(receiver, restoredSecond, 2);
        target.Modules.DeleteModule(RemoteSourceFunHostNames.SourceModule);
        nextCode.Definition.Register(target.Modules);
        // Receiver never ran make/1 in either image. Old descriptor selection must
        // still use its fingerprint, not the current image's different body.
        var restoredOld = (FunctionTerm)target.ImportExternalTerm(ExternalTermFormat.Encode(first, new EtfEncodingOptions { FunctionOwner = source.Process.Pid })).Term;
        await AssertValue(receiver, restoredOld, 1);
        await receiver.InvokeAsync(async context =>
            await target.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ModuleCodeLifecycleNames.PurgeModule,
                Term.A(RemoteSourceFunHostNames.SourceModule)
            ));
        await receiver.InvokeAsync(async context =>
        {
            try
            {
                await restoredOld.Invoke(context, []);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), restoredOld)))
            {
                return true;
            }
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        });
        target.Modules.DeleteModule(RemoteSourceFunHostNames.SourceModule);
        await receiver.InvokeAsync(async context =>
            await target.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ModuleCodeLifecycleNames.PurgeModule,
                Term.A(RemoteSourceFunHostNames.SourceModule)
            ));
        await receiver.InvokeAsync(async context =>
        {
            try
            {
                await restoredOld.Invoke(context, []);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)))
            {
                return true;
            }
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        });
    }

    private static Task<bool> AssertValue(RuntimeNodeFoundationHostActor receiver, FunctionTerm callback, int value)
        => receiver.InvokeAsync(async context =>
        {
            Require((await callback.Invoke(context, [])).Equals(Term.Tuple(Term.A(RemoteSourceFunHostNames.Old), Term.I(value))));

            return true;
        });

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
