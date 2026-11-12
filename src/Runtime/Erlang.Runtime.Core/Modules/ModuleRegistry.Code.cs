namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallManagedCodePrimitives()
    {
        InstallPreloaded(registry =>
        {
            var signatures = InterpreterCodeNativeNames.BadArgumentSignatures
                .Append((InterpreterCodeNativeNames.CoverageSupport, 0));
            foreach (var signature in signatures)
            {
                if (!InterpreterCodeNative.TryCapture(
                    InterpreterCodeNativeNames.Module,
                    signature.Item1,
                    signature.Item2,
                    out ErlangFunction body
                ))
                {
                    throw new InvalidOperationException(LibrarySelectionNames.Primitive);
                }
                registry.RegisterReviewedManagedLibrary789(
                    InterpreterCodeNativeNames.Module,
                    signature.Item1,
                    signature.Item2,
                    body
                );
            }
        });
    }
}
