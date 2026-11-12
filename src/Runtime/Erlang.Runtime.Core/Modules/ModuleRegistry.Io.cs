namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallManagedIoPrimitive()
    {
        InstallPreloaded(registry =>
        {
            var key = (IoModuleNames.Module, IoModuleNames.Format, 2);
            if (functions.TryGetValue(key, out ErlangFunction? bootstrap))
            {
                if (!IoFormatRequests.IsBootstrap(bootstrap) || sourceOwners.ContainsKey(key))
                {
                    throw new InvalidOperationException(LibrarySelectionNames.Owner);
                }
                functions.Remove(key);
                runtimeErrorBindings.Remove(key);
                ownedBindings507.Remove(key);
            }
            if (!IoPrintableRangeNative.TryCapture(
                owningRuntime,
                IoPrintableRangeNativeNames.Module,
                IoPrintableRangeNativeNames.Function,
                0,
                out ErlangFunction body
            ))
            {
                throw new InvalidOperationException(LibrarySelectionNames.Primitive);
            }
            registry.RegisterReviewedManagedLibrary789(
                IoPrintableRangeNativeNames.Module,
                IoPrintableRangeNativeNames.Function,
                0,
                body
            );
        });
    }
}
