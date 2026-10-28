namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private NifNativeTermValue AdoptMagicValue576(NifNativeTermValue value)
    {
        if (guestRoots516 is not { } roots
            || value.StaticTerm is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.NativeResource524 } resource)
            return value;

        // A fresh enif_make_resource call supplies a fresh portable wrapper;
        // its actual process wrapper must be returned/stored by this handoff.
        // An already-owned same-process alias is preserved by adoption.
        return new(roots.AdoptGuestInto507(roots.Reserve(), resource));
    }
}
