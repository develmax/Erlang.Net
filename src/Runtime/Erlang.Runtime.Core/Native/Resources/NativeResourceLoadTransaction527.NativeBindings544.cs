namespace Erlang;

internal sealed partial class NativeResourceLoadTransaction527
{
    internal NifNativeBindingTransition544 PrepareBindings544(
        ProcessRuntime runtime,
        NifImageBindingReservation pending,
        NifPreparedImageBindings544 prepared,
        NifNativeLibraryOwner candidate
    ) => image.Image.NifBindings.PrepareNativeTransition544(
        runtime,
        pending,
        prepared,
        candidate,
        table,
        authority
    );
}
