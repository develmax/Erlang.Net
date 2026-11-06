using Erlang;

/// <summary>Installs shared ETS primitives for an explicit library selection.</summary>
internal static class RpcCompatibilityBootstrap
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
        RuntimeBootstrapEtsRename418.Register(runtime, registry);
        RuntimeBootstrapEtsHeir419.Register(runtime, registry);
        RuntimeBootstrapEtsCompiled419.Register(runtime, registry);
        RuntimeBootstrapEtsAll420.Register(runtime, registry);
        RuntimeBootstrapEtsFixation420.Register(runtime, registry);
        RuntimeBootstrapEtsCursor420.Register(runtime, registry);
        RuntimeBootstrapEtsReplace420.Register(runtime, registry);
        RuntimeBootstrapEtsInspection420.Register(runtime, registry);
        RuntimeBootstrapEtsInfo420.Register(runtime, registry);
    }
}
