namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    internal nint ReadInitializingPrivateData557() => Volatile.Read(ref privateData);

    private int InvokeLoadCallback557(NifNativeCallEnvironment environment, nuint information)
    {
        // enif_priv_data reads lib->priv_data, including writes made earlier
        // during this same load callback through its void** argument.
        environment.BindInitializingPrivateData557(this);
        fixed (nint* data = &privateData)
            return entry.Load == 0 ? 0
                : ((delegate* unmanaged[Cdecl]<nint, nint*, nuint, int>)entry.Load)(environment.Pointer, data, information);
    }
}
