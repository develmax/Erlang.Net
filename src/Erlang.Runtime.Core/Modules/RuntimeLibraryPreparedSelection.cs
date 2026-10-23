using System.Collections.Frozen;

namespace Erlang;

internal sealed class RuntimeLibraryPreparedSelection
{
    internal RuntimeLibraryPreparedSelection(
        RuntimeLibrarySelection request,
        IDictionary<string, PreparedBeamImage> images,
        IDictionary<string, ModuleCodeImage> bootstrapImages,
        IReadOnlyList<string> onLoadOrder
    )
    {
        Request = request;
        Images = images.ToFrozenDictionary(StringComparer.Ordinal);
        BootstrapImages = bootstrapImages.ToFrozenDictionary(StringComparer.Ordinal);
        OnLoadOrder = Array.AsReadOnly(onLoadOrder.ToArray());
    }

    internal RuntimeLibrarySelection Request { get; }
    internal IReadOnlyDictionary<string, PreparedBeamImage> Images { get; }
    internal IReadOnlyDictionary<string, ModuleCodeImage> BootstrapImages { get; }
    internal IReadOnlyList<string> OnLoadOrder { get; }
}
