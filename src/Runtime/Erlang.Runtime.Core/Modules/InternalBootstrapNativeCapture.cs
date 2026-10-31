namespace Erlang;

// Real installer-backed primitives may be captured privately before any public
// native provider publication. This never registers or synthesizes a body.
internal static class InternalBootstrapNativeCapture
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        ModuleRegistry registry,
        string module,
        string function,
        int arity,
        out ErlangFunction body,
        out OwnedBindingKind507 kind
    )
    {
        body = null!;
        kind = OwnedBindingKind507.Opaque;
        if (owner is null || !ReferenceEquals(owner.Modules, registry))
            return false;
        if (module == AtomicsNames413.Internal && function == AtomicsNames413.New && arity == 2)
        {
            if (IntPtr.Size != sizeof(ulong))
                return false;
            var constructor = new AtomicsConstructorBinding413(owner, registry);
            body = constructor.Body;
            // Exactly the real constructor delegate and owning registry, matching
            // its genuine public installer. No other bootstrap family is upgraded.
            kind = OwnedBindingKind507.RuntimeRead535;

            return true;
        }
        if (module == InternalPortNames416.Module && InternalPortNames416.Signatures.Contains((function, arity)))
        {
            body = new InternalPortBinding416(owner, function, arity).Body;

            return true;
        }
        if (module == SystemTaskNames428.Module && SystemTaskNames428.Signatures.Contains((function, arity)))
        {
            body = function == InternalGarbageCollectionNames428.Function
                ? new InternalGarbageCollectionBinding428(owner).Body
                : new InternalSystemTaskBinding428(owner, function, arity).Body;

            return true;
        }
        if (module == RuntimeBootstrapFileNames.InternalModule && (function, arity) is
            (RuntimeBootstrapFileNames.IsSystemProcess, 1) or (RuntimeBootstrapFileNames.SpawnSystemProcess, 3))
        {
            body = new InternalSystemProcessPrimitiveBinding(
                owner,
                registry,
                function,
                arity
            ).Body;

            return true;
        }
        if (module == EtsInspectionNames420.Module && EtsInspectionNames420.Signatures.Contains((function, arity)))
        {
            body = new EtsInspectionBinding420(owner, registry, (function, arity)).Body;

            return true;
        }
        if (module == RuntimeBootstrapSchedulerNames.Module
            && function == RuntimeBootstrapSchedulerNames.Function && arity == RuntimeBootstrapSchedulerNames.Arity)
        {
            body = new RuntimeBootstrapSchedulerBinding(owner, registry).Body;

            return true;
        }

        return false;
    }
}
