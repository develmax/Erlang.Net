namespace Erlang;

internal sealed partial class NifImageBindings
{
    private bool IsGuestNativeBody535(string name, int arity, ErlangFunction body)
        => nativeOwner is NifNativeLibraryOwner native
            && native.OwnsGuestBinding535(
                Image,
                name,
                arity,
                body
            );

    internal bool HasGuestNative535(string name, int arity)
    {
        lock (gate)
            return phase == NifImageBindingPhase.Ready && ready!.TryGetValue((name, arity), out var body)
                && IsGuestNativeBody535(name, arity, body);
    }
}
