using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeLinuxNamespace : IDisposable
{
    private readonly object gate = new();
    private readonly NifNativeLinuxBridgeInput bridge;
    private readonly NifNativeEnvironmentAllocator allocator;
    private readonly nint identity;
    private NifNativeLinuxReallocationExtension? reallocation;
    private NifNativeBridgeTable* table;
    private nint library;
    private int capsules;
    private readonly List<string> pthreadBindings = new();
    internal string PthreadWitness => System.Text.Json.JsonSerializer.Serialize(new { Bindings = pthreadBindings, Calls = NifNativeLinuxPthreadControl.CallsWitness(library) });
    internal nint Reader { get; private set; }

    internal NifNativeLinuxNamespace(ProcessRuntime runtime, ModuleCodeImage image, NifNativeCapabilityProfile profile)
    {
        bridge = NifNativeLinuxBridgeDeployment.Open(profile);
        allocator = () =>
        {
            try
            {
                return new NifNativeCallEnvironment(runtime, image).Pointer;
            }
            catch (Exception failure)
            {
                NifNativeCallbackScope.Fail(failure);

                return 0;
            }
        };
        try
        {
            library = NifNativeLinuxNamespaceApi.Open(NifNativeLinuxNamespaceNames.NewNamespace, bridge.Descriptor);
            identity = NifNativeLinuxNamespaceApi.Identity(library);
            NifNativeLinuxPthreadControl.Bind(library);
            table = (NifNativeBridgeTable*)NativeMemory.Alloc((nuint)sizeof(NifNativeBridgeTable));
            if (table == null)
                throw new OutOfMemoryException();
            *table = NifNativeExportRegistration.CreateTable();
            NifNativeReferenceBridgeTable.Bind(ref *table, Marshal.GetFunctionPointerForDelegate(allocator));
            var register = (delegate* unmanaged[Cdecl]<NifNativeBridgeTable*, int>)NifNativeLinuxNamespaceApi.Symbol(library, NifNativeNames.BridgeRegister);
            if (register(table) != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            Reader = NifNativeLinuxNamespaceApi.Symbol(library, NifNativeNames.BridgeReadTerm);
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

    internal NifNativeLinuxCapsuleLease Open(NifNativeCapabilityProfile profile, FileStream? verifiedTarget)
    {
        lock (gate)
        {
            if (library == 0)
                throw new ObjectDisposedException(nameof(NifNativeLinuxNamespace), NifNativeLinuxNamespaceNames.Disposed);
            using var requestedBridge = NifNativeLinuxBridgeDeployment.Open(profile);
            if (!bridge.Hash.Equals(requestedBridge.Hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(NifNativeLinuxNamespaceNames.BridgeMismatch);
            var dependencies = new List<byte[]> { bridge.Descriptor };
            if (profile.ReallocationEnabled)
            {
                if (profile.ReallocationBridgePath is not { } path || profile.ReallocationBridgeSha256 is not { } hash)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                if (reallocation is null)
                    reallocation = new(identity, path, hash);
                else if (!reallocation.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(NifNativeLinuxNamespaceNames.BridgeMismatch);
                dependencies.Add(reallocation.Descriptor);
            }
            byte[] target = profile.ExpectedLibrarySha256 is not null
                ? NifNativeLinuxNamespaceApi.Descriptor(verifiedTarget ?? throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract))
                : profile.NativeLibraryPath.UnixNullTerminated()[..^1];
            dependencies.Add(target);
            FileStream capsule = NifNativeLinuxNamespaceApi.Capsule(NifNativeElfCapsule.Create(dependencies.ToArray()));
            nint handle = 0;
            try
            {
                handle = NifNativeLinuxNamespaceApi.Open(identity, NifNativeLinuxNamespaceApi.Descriptor(capsule));
                if (NifNativeLinuxNamespaceApi.Identity(handle) != identity)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                pthreadBindings.Add(NifNativeLinuxPthreadControl.BindingWitness(library, handle));
                capsules++;

                return new(this, capsule, handle);
            }
            catch (Exception loadFailure)
            {
                try
                {
                    NifNativePlatformLoader.Close(handle);
                }
                catch (Exception cleanupFailure)
                {
                    capsules++;
                    NifNativeLinuxNamespaceRetention.Retain(new NifNativeLinuxCapsuleLease(this, capsule, handle));
                    throw new AggregateException(loadFailure, cleanupFailure);
                }
                capsule.Dispose();
                throw;
            }
        }
    }

    internal void ReleaseCapsule()
    {
        lock (gate)
            capsules--;
    }

    public void Dispose()
    {
        try
        {
            lock (gate)
            {
                if (capsules != 0)
                    throw new InvalidOperationException(NifNativeLinuxNamespaceNames.ActiveImages);
                reallocation?.Dispose();
                reallocation = null;
                NifNativePlatformLoader.Close(library);
                library = 0;
                Reader = 0;
                NativeMemory.Free(table);
                table = null;
                bridge.Dispose();
                GC.KeepAlive(allocator);
            }
        }
        catch
        {
            NifNativeLinuxNamespaceRetention.Retain(this);
            throw;
        }
    }
}
