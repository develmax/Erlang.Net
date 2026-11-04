namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallManagedFilePrimitive()
        => InstallPreloaded(registry =>
        {
            if (!FileNameEncodingNative.TryCapture(
                FileNameEncodingNames.Module,
                FileNameEncodingNames.Function,
                0,
                out ErlangFunction body
            ))
            {
                throw new InvalidOperationException(LibrarySelectionNames.Primitive);
            }
            registry.RegisterReviewedManagedLibrary789(
                FileNameEncodingNames.Module,
                FileNameEncodingNames.Function,
                0,
                body
            );
        });
}
