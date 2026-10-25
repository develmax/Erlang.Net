namespace Erlang;

public static partial class RuntimeNativePrimitiveProviders
{
    public static void RegisterCatalog(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(RuntimeNativePrimitiveNames.ForeignRegistry, nameof(registry));

        Register(runtime, registry, RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore);
        RuntimeBootstrapAllocatorGather434.Register(runtime, registry);
        RuntimeBootstrapAtomics.Register(runtime, registry);
        RuntimeBootstrapBeamfileMd5423.Register(runtime, registry);
        RuntimeBootstrapCheckProcessCode423.Register(runtime, registry);
        RuntimeBootstrapDdllError416.Register(runtime, registry);
        RuntimeBootstrapDebugCode427.Register(runtime, registry);
        RuntimeBootstrapDebugDirty424.Register(runtime, registry);
        RuntimeBootstrapDebugDispatch432.Register(runtime, registry);
        RuntimeBootstrapDebugSharing425.Register(runtime, registry);
        RuntimeBootstrapDirtyQuery423.Register(runtime, registry);
        RuntimeBootstrapDistChannel429.Register(runtime, registry);
        RuntimeBootstrapDistDecode423.Register(runtime, registry);
        RuntimeBootstrapDistributionProfile429.Register(runtime, registry);
        RuntimeBootstrapDistSpawn430.Register(runtime, registry);
        RuntimeBootstrapEtsAll420.Register(runtime, registry);
        RuntimeBootstrapEtsCompiled419.Register(runtime, registry);
        RuntimeBootstrapEtsCounter417.Register(runtime, registry);
        RuntimeBootstrapEtsCursor420.Register(runtime, registry);
        RuntimeBootstrapEtsDeletion.Register(runtime, registry);
        RuntimeBootstrapEtsFixation420.Register(runtime, registry);
        RuntimeBootstrapEtsHeir419.Register(runtime, registry);
        RuntimeBootstrapEtsInfo420.Register(runtime, registry);
        RuntimeBootstrapEtsInspection420.Register(runtime, registry);
        RuntimeBootstrapEtsMutations414.Register(runtime, registry);
        RuntimeBootstrapEtsQueries414.Register(runtime, registry);
        RuntimeBootstrapEtsRename418.Register(runtime, registry);
        RuntimeBootstrapEtsReplace420.Register(runtime, registry);
        RuntimeBootstrapEtsTraversal416.Register(runtime, registry);
        RuntimeBootstrapFileSystem.Register(runtime, registry);
        RuntimeBootstrapHamtMap430.Register(runtime, registry);
        RuntimeBootstrapInternalGarbageCollection428.Register(runtime, registry);
        RuntimeBootstrapInternalPorts416.Register(runtime, registry);
        RuntimeBootstrapInternalScalars413.Register(runtime, registry);
        RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry);
        RuntimeBootstrapInternalTrace425.Register(runtime, registry);
        RuntimeBootstrapMapKeys426.Register(runtime, registry);
        RuntimeBootstrapProcessAlive425.Register(runtime, registry);
        RuntimeBootstrapScheduler.Register(runtime, registry);
    }
}
