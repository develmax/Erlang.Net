namespace Erlang;

internal static unsafe class NifNativeReferenceBridgeTable
{
    internal static void Bind(ref NifNativeBridgeTable table, nint independentAllocator)
    {
        table.MakeReference = &NifNativeReferenceCallbacks.Make;
        table.AllocateEnvironment = (delegate* unmanaged[Cdecl]<nint>)independentAllocator;
        table.FreeEnvironment = &NifNativeEnvironmentCallbacks.Free;
        table.ClearEnvironment = &NifNativeEnvironmentCallbacks.Clear;
        table.CopyTerm = &NifNativeEnvironmentCallbacks.Copy;
    }
}
