using System.Runtime.CompilerServices;

namespace Erlang;

internal static class NativeResourceImages524
{
    private static readonly object Gate = new();
    private static readonly ConditionalWeakTable<ModuleCodeImage, NifNativeResourceImage524> Images = new();

    internal static void Register524(NifNativeResourceImage524 image)
    {
        lock (Gate)
        {
            if (Images.TryGetValue(image.Image, out NifNativeResourceImage524? old))
            {
                if (!old.IsDisposed524)
                    throw new InvalidOperationException(NativeResource524Names.ImageUnavailable);
                Images.Remove(image.Image);
            }
            Images.Add(image.Image, image);
        }
    }

    internal static bool TryGet527(ModuleCodeImage image, out NifNativeResourceImage524? result)
    {
        lock (Gate) return Images.TryGetValue(image, out result);
    }

    internal static NifNativeResourceImage524 Get524(ModuleCodeImage image)
    {
        lock (Gate)
            return Images.TryGetValue(image, out NifNativeResourceImage524? result) ? result
                : throw new InvalidOperationException(NativeResource524Names.ImageUnavailable);
    }
}
