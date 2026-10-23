namespace Erlang;

internal static class BeamLoaderRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.Prepare,
            2,
            (context, arguments) => ValueTask.FromResult(BeamLoaderApi.Prepare(context, arguments[0], arguments[1]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.HasOnLoad,
            1,
            (context, arguments) => ValueTask.FromResult(BeamLoaderApi.HasOnLoad(context, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.Finish,
            1,
            (context, arguments) => context.Runtime.Modules.ModifySourceCodeAsync(context, () => BeamLoaderApi.Finish(context, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.CallOnLoad,
            1,
            (context, arguments) => ValueTask.FromResult(BeamLoaderApi.DynamicCallOnLoad(context, arguments[0]))
        );
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.FinishOnLoad,
            2,
            (context, arguments) => BeamLoaderApi.FinishOnLoadAsync(context, arguments[0], arguments[1])
        );
        registry.Register(
            ErlangModuleNames.Module,
            BeamLoaderNames.LoadModule,
            2,
            (context, arguments) => context.Runtime.Modules.ModifySourceCodeAsync(context, () => BeamLoaderApi.LoadModule(context, arguments[0], arguments[1]))
        );
    }
}
