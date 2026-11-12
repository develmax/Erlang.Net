namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallManagedErrorLoggerPrimitive()
        => InstallPreloaded(registry =>
        {
            if (!WarningMapNative.TryCapture(
                owningRuntime,
                WarningMapNativeNames.Module,
                WarningMapNativeNames.Function,
                0,
                out ErlangFunction body))
            {
                throw new InvalidOperationException(LibrarySelectionNames.Primitive);
            }
            registry.RegisterReviewedManagedLibrary789(
                WarningMapNativeNames.Module,
                WarningMapNativeNames.Function,
                0,
                body);
        });
}
