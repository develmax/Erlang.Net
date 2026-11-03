namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    private NifImageCall? nativeGuestResult535;

    internal void BindNativeGuestResult535(NifImageCall native)
    {
        if (!native.GuestNative535 || nativeGuestResult535 is not null || IsDisposed)
            throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);
        nativeGuestResult535 = native;
        BindingKind507 = OwnedBindingKind507.NativeGuest535;
    }

    internal static bool TryAdmitNativeGuestResult535(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        Term value,
        out Term result
    )
    {
        result = null!;
        // The nearest captured invocation must own this exact running image
        // call. An older guest frame cannot authorize an opaque nested call.
        OwnedTermInvocation506? invocation = Active;
        if (invocation is not { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.NativeGuest535 }
            || !ReferenceEquals(invocation.Runtime, runtime)
            || invocation.nativeGuestResult535?.CanRetainGuestResult535(image) != true)
            return false;
        // Publish exact graph aliases before the native environment releases
        // its roots. Genuine copies and opaque exports keep their own paths.
        result = invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), value);

        return true;
    }
}
