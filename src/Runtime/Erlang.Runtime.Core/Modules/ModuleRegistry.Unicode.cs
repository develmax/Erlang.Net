namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallManagedUnicodePrimitives()
    {
        InstallPreloaded(registry =>
        {
            (string Function, int Arity)[] signatures =
            [
                (UnicodeNativeNames.ToBinary, 2),
                (UnicodeNativeNames.ToList, 2),
                (UnicodeNativeNames.IsSevenBit, 1)
            ];
            foreach (var signature in signatures)
            {
                if (!UnicodeNative.TryCapture(
                    this,
                    UnicodeNativeNames.Module,
                    signature.Function,
                    signature.Arity,
                    out ErlangFunction body
                ))
                {
                    throw new InvalidOperationException(LibrarySelectionNames.Primitive);
                }
                registry.RegisterReviewedManagedLibrary789(
                    UnicodeNativeNames.Module,
                    signature.Function,
                    signature.Arity,
                    body
                );
            }
        });
    }
}
