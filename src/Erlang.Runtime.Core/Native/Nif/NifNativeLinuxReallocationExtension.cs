using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeLinuxReallocationExtension : IDisposable
{
    private readonly FileStream file;
    private NifNativeReallocationBridgeTable* table;
    private nint library;
    internal string Hash { get; }
    internal byte[] Descriptor => NifNativeLinuxNamespaceApi.Descriptor(file);

    internal NifNativeLinuxReallocationExtension(nint identity, string path, string hash)
    {
        Hash = hash;
        file = NifNativeCapabilityProfile.OpenVerified(path, hash);
        try
        {
            library = NifNativeLinuxNamespaceApi.Open(identity, Descriptor);
            table = (NifNativeReallocationBridgeTable*)NativeMemory.Alloc((nuint)sizeof(NifNativeReallocationBridgeTable));
            if (table == null)
                throw new OutOfMemoryException();
            *table = new NifNativeReallocationBridgeTable
            {
                Version = NifNativeReallocationNames.Version,
                Size = (uint)sizeof(NifNativeReallocationBridgeTable),
                Reallocate = &NifNativePrimitiveCallbacks.Reallocate
            };
            var register = (delegate* unmanaged[Cdecl]<NifNativeReallocationBridgeTable*, int>)NifNativeLinuxNamespaceApi.Symbol(library, NifNativeReallocationNames.Register);
            if (register(table) != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        }
        catch (Exception creationFailure)
        {
            try
            {
                Dispose();
            }
            catch (Exception cleanupFailure)
            {
                NifNativeLinuxNamespaceRetention.Retain(this);
                throw new AggregateException(creationFailure, cleanupFailure);
            }
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            NifNativePlatformLoader.Close(library);
            library = 0;
            NativeMemory.Free(table);
            table = null;
            file.Dispose();
        }
        catch
        {
            NifNativeLinuxNamespaceRetention.Retain(this);
            throw;
        }
    }
}
