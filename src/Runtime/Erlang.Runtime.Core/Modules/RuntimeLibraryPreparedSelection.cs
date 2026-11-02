using System.Collections.Frozen;

namespace Erlang;

internal sealed class RuntimeLibraryPreparedSelection : IDisposable
{
    // Preserve the original metadata constructor; runtime preparation uses
    // the explicit binding snapshot constructor below and validates it at publish.
    internal RuntimeLibraryPreparedSelection(
        RuntimeLibrarySelection request,
        IDictionary<string, PreparedBeamImage> images,
        IDictionary<string, ModuleCodeImage> bootstrapImages,
        IReadOnlyList<string> onLoadOrder
    ) : this(
        request,
        images,
        bootstrapImages,
        onLoadOrder,
        new Dictionary<(string Module, string Function, int Arity), ErlangFunction>()
    )
    {
    }

    internal RuntimeLibraryPreparedSelection(
        RuntimeLibrarySelection request,
        IDictionary<string, PreparedBeamImage> images,
        IDictionary<string, ModuleCodeImage> bootstrapImages,
        IReadOnlyList<string> onLoadOrder,
        IReadOnlyDictionary<(string Module, string Function, int Arity), ErlangFunction> bootstrapBindings802
    )
    {
        Request = request;
        Images = images.ToFrozenDictionary(StringComparer.Ordinal);
        BootstrapImages = bootstrapImages.ToFrozenDictionary(StringComparer.Ordinal);
        OnLoadOrder = Array.AsReadOnly(onLoadOrder.ToArray());
        BootstrapBindings802 = bootstrapBindings802.ToFrozenDictionary();
    }

    internal RuntimeLibrarySelection Request { get; }
    internal IReadOnlyDictionary<string, PreparedBeamImage> Images { get; }
    internal IReadOnlyDictionary<string, ModuleCodeImage> BootstrapImages { get; }
    internal IReadOnlyList<string> OnLoadOrder { get; }
    internal IReadOnlyDictionary<(string Module, string Function, int Arity), ErlangFunction> BootstrapBindings802 { get; }

    public void Dispose()
    {
        foreach (PreparedBeamImage image in Images.Values) image.Dispose();
    }
}
