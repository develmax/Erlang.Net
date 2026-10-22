namespace Erlang;

public sealed partial class ModuleRegistry
{
    private PreparedBeamImage? bootstrapTtyImage;

    internal bool IsBootstrapTtyImage(ModuleCodeImage image)
    {
        lock (gate) return bootstrapTtyImage is not null
            && ReferenceEquals(bootstrapTtyImage.ExecutionIdentity.Image, image);
    }

    internal void InstallBootstrapTty(PreparedBeamImage image)
    {
        using var mutation = EnterSourceMutation();
        lock (gate)
        {
            if (!ReferenceEquals(image.Owner, this) || image.Name != PrimTtyNames.Module
                || image.Descriptor.DeclaredNifs is not { } declarations
                || !declarations.ToHashSet().SetEquals(PrimTtyNames.Signatures))
                throw new ArgumentException(PrimTtyNames.Foreign);
            if (bootstrapTtyImage is not null && !ReferenceEquals(bootstrapTtyImage, image))
                throw new InvalidOperationException(PrimTtyNames.Foreign);
            bootstrapTtyImage = image;
        }
    }
}
