using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Erlang;

internal static unsafe class NifNativeExportRegistration
{
    private static readonly object Gate = new();
    private static nint library;
    private static string? trustedHash;
    private static FileStream? verifiedFile;
    // Deliberately process-owned after publication: global imported addresses may outlive images.
    private static NifNativeBridgeTable* table;

    internal static void EnsureRegistered(NifNativeCapabilityProfile profile)
    {
        // Host-file loading uses the runtime's already-exported ABI, if present. Without it,
        // dlopen reports unresolved imports rather than requiring a per-library test policy.
        if (OperatingSystem.IsWindows() || profile.ExpectedLibrarySha256 is null)
            return;
        if (AssemblyLoadContext.GetLoadContext(typeof(NifNativeExportRegistration).Assembly)?.IsCollectible == true)
            throw new NotSupportedException(NifNativeDiagnostics.BridgeLifetime);
        if (profile.BridgePath is not { } path || profile.BridgeSha256 is not { } hash)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        lock (Gate)
        {
            if (library != 0 && table != null && verifiedFile is not null)
            {
                if (!string.Equals(trustedHash, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

                return;
            }
            var file = NifNativeCapabilityProfile.OpenVerified(path, hash);
            nint handle = 0;
            NifNativeBridgeTable* candidate = null;
            bool published = false;
            try
            {
                handle = NifNativePlatformLoader.Open(file, path, global: true);
                foreach (string symbol in NifNativeNames.BridgeSymbols)
                    if (NifNativePlatformLoader.Symbol(0, symbol) != NifNativePlatformLoader.Symbol(handle, symbol))
                        throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                var reader = (delegate* unmanaged[Cdecl]<nint, nuint*, int>)NifNativePlatformLoader.Symbol(handle, NifNativeNames.BridgeReadTerm);
                var register = (delegate* unmanaged[Cdecl]<NifNativeBridgeTable*, int>)NifNativePlatformLoader.Symbol(handle, NifNativeNames.BridgeRegister);
                candidate = (NifNativeBridgeTable*)NativeMemory.Alloc((nuint)sizeof(NifNativeBridgeTable));
                if (candidate == null)
                    throw new OutOfMemoryException();
                *candidate = CreateTable();
                NifNativeVariadicCursor.Reader = reader;
                if (register(candidate) != 0)
                    throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
                published = true;
                table = candidate;
                trustedHash = hash;
                verifiedFile = file;
                library = handle;
            }
            finally
            {
                if (!published)
                {
                    NifNativeVariadicCursor.Reader = null;
                    NativeMemory.Free(candidate);
                    try
                    {
                        NifNativePlatformLoader.Close(handle);
                    }
                    finally
                    {
                        file.Dispose();
                    }
                }
            }
        }
    }

    internal static NifNativeBridgeTable CreateTable() => new()
    {
        Version = NifNativeNames.BridgeVersion,
        Size = (uint)sizeof(NifNativeBridgeTable),
        PrivateData = &NifNativePrimitiveCallbacks.PrivateData,
        Allocate = &NifNativePrimitiveCallbacks.Allocate,
        Free = &NifNativePrimitiveCallbacks.Free,
        GetInt = &NifNativePrimitiveCallbacks.GetInt,
        BadArgument = &NifNativePrimitiveCallbacks.BadArgument,
        MakeInt = &NifNativePrimitiveCallbacks.MakeInt,
        MakeString = &NifNativePrimitiveCallbacks.MakeString,
        IsException = &NifNativePrimitiveCallbacks.IsException,
        HasPendingException = &NifNativePrimitiveCallbacks.HasPendingException,
        RaiseException = &NifNativePrimitiveCallbacks.RaiseException,
        MakeTuple = &NifNativePrimitiveCallbacks.MakeTuple,
        MakeList = &NifNativePrimitiveCallbacks.MakeList,
        IsAtom = &NifNativeTypeCallbacks.IsAtom,
        IsBinary = &NifNativeTypeCallbacks.IsBinary,
        IsReference = &NifNativeTypeCallbacks.IsReference,
        IsFunction = &NifNativeTypeCallbacks.IsFunction,
        IsPid = &NifNativeTypeCallbacks.IsPid,
        IsPort = &NifNativeTypeCallbacks.IsPort,
        IsEmptyList = &NifNativeTypeCallbacks.IsEmptyList,
        IsList = &NifNativeTypeCallbacks.IsList,
        IsTuple = &NifNativeTypeCallbacks.IsTuple,
        IsNumber = &NifNativeTypeCallbacks.IsNumber,
        IsMap = &NifNativeTypeCallbacks.IsMap,
        GetListCell = &NifNativeListCellCallbacks.Get,
        MakeTupleFromArray = &NifNativeArrayCallbacks.MakeTuple,
        MakeListFromArray = &NifNativeArrayCallbacks.MakeList,
        GetUnsignedLong = &NifNativeNumericCallbacks.GetUInt64,
        GetDouble = &NifNativeNumericCallbacks.GetDouble,
        GetUnsignedInt = &NifNativeNumericCallbacks.GetUnsignedInt,
        GetLong = &NifNativeNumericCallbacks.GetInt64,
        GetInt64 = &NifNativeNumericCallbacks.GetInt64,
        GetUInt64 = &NifNativeNumericCallbacks.GetUInt64,
        MakeUnsignedLong = &NifNativeLinuxNumericCallbacks.MakeUnsignedLong,
        MakeDouble = &NifNativeNumericCallbacks.MakeDouble,
        MakeUnsignedInt = &NifNativeNumericCallbacks.MakeUnsignedInt,
        MakeLong = &NifNativeLinuxNumericCallbacks.MakeLong,
        MakeInt64 = &NifNativeLinuxNumericCallbacks.MakeLong,
        MakeUInt64 = &NifNativeLinuxNumericCallbacks.MakeUnsignedLong,
        GetMapSize = &NifNativeMapSizeCallbacks.Size
    };
}
