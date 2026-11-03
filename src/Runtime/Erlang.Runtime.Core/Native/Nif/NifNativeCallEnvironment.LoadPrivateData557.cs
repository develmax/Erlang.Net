namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    private NifNativeLibraryOwner? initializingOwner557;

    internal void BindInitializingPrivateData557(NifNativeLibraryOwner owner)
        => initializingOwner557 = owner;
}
