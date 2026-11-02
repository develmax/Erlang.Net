namespace Erlang;

internal sealed partial class NativeResourceTypeTable527
{
    // Caller owns actual publication writer and names monitor. No C runs here.
    internal NativeResourceUpgradeSnapshot544 CreateUpgradeSnapshot544(
        NativeResourceLoadTransaction527 transaction,
        NifNativeResourceImage524 image,
        NifResourceImagePin524 loadingPin
    )
    {
        var types = new List<NativeResourceType524>();
        foreach (var type in committed)
            if (type.Module527 == image.Image.Module) types.Add(type);

        return new(
            transaction,
            domain,
            image,
            loadingPin,
            types.ToArray()
        );
    }
}
