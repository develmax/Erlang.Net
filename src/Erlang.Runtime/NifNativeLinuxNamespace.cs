using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeLinuxNamespace : IDisposable
{
    private readonly object gate = new();
    private readonly NifNativeLinuxBridgeInput bridge;
    private readonly NifNativeEnvironmentAllocator allocator;
    private FileStream? experimentalBridgeCopy;
    private byte[] BridgeDescriptor => NifNativeLinuxNamespaceApi.Descriptor(experimentalBridgeCopy ?? bridge.File);
    private readonly nint identity;
    private NifNativeLinuxReallocationExtension? reallocation;
    private NifNativeBridgeTable* table;
    private nint library;
    private int capsules;
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
            if (NifNativeLinuxNamespaceApi.ExperimentalBaseCopies)
            {
                if (profile.ExpectedLibrarySha256 is null || profile.ReallocationEnabled)
                    throw new NotSupportedException(NifNativeLinuxNamespaceNames.ExperimentalUnsupported);
                experimentalBridgeCopy = NifNativeLinuxNamespaceApi.ExperimentalCopy(bridge.File, bridge.Hash);
            }
            library = NifNativeLinuxNamespaceApi.Open(NifNativeLinuxNamespaceNames.NewNamespace, BridgeDescriptor);
            identity = NifNativeLinuxNamespaceApi.Identity(library);
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
            if (NifNativeLinuxNamespaceApi.ExperimentalBaseCopies && (profile.ExpectedLibrarySha256 is null || profile.ReallocationEnabled))
                throw new NotSupportedException(NifNativeLinuxNamespaceNames.ExperimentalUnsupported);
            var dependencies = new List<byte[]> { BridgeDescriptor };
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
            FileStream? experimentalTargetCopy = NifNativeLinuxNamespaceApi.ExperimentalBaseCopies
                ? NifNativeLinuxNamespaceApi.ExperimentalCopy(verifiedTarget ?? throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract), profile.ExpectedLibrarySha256!)
                : null;
            byte[] target = experimentalTargetCopy is not null
                ? NifNativeLinuxNamespaceApi.Descriptor(experimentalTargetCopy)
                : profile.ExpectedLibrarySha256 is not null
                ? NifNativeLinuxNamespaceApi.Descriptor(verifiedTarget ?? throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract))
                : profile.NativeLibraryPath.UnixNullTerminated()[..^1];
            dependencies.Add(target);
            FileStream capsule;
            try
            {
                capsule = NifNativeLinuxNamespaceApi.Capsule(NifNativeElfCapsule.Create(dependencies.ToArray()));
            }
            catch
            {
                experimentalTargetCopy?.Dispose();
                throw;
            }
            nint handle = 0;
            try
            {
                handle = NifNativeLinuxNamespaceApi.Open(identity, NifNativeLinuxNamespaceApi.Descriptor(capsule));
                if (NifNativeLinuxNamespaceApi.Identity(handle) != identity)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                capsules++;

                return new(this, capsule, handle, experimentalTargetCopy);
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
                    NifNativeLinuxNamespaceRetention.Retain(new NifNativeLinuxCapsuleLease(this, capsule, handle, experimentalTargetCopy));
                    throw new AggregateException(loadFailure, cleanupFailure);
                }
                capsule.Dispose();
                experimentalTargetCopy?.Dispose();
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
                experimentalBridgeCopy?.Dispose();
                experimentalBridgeCopy = null;
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
