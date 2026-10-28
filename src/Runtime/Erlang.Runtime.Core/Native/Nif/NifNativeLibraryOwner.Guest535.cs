using System.Collections.Frozen;

namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    private FrozenDictionary<(string Name, int Arity), ErlangFunction> guestBindings535
        = FrozenDictionary<(string Name, int Arity), ErlangFunction>.Empty;

    private void SealGuestBindings535(IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> produced)
        => Volatile.Write(ref guestBindings535, produced.ToFrozenDictionary(pair => pair.Key, pair => pair.Value));

    // Called under the image bindings gate; do not acquire the native-owner gate.
    // Publication completed initialization and a NifImageCall owns physical custody.
    internal bool OwnsGuestBinding535(
        ModuleCodeImage selectedImage,
        string name,
        int arity,
        ErlangFunction body
    )
        => !Volatile.Read(ref disposed) && Volatile.Read(ref initialized) && ReferenceEquals(image, selectedImage)
            && Volatile.Read(ref guestBindings535).TryGetValue((name, arity), out var captured)
            && ReferenceEquals(captured, body);
}
