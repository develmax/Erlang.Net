using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeEntryReader
{
    internal static NifNativeAbiEntry Read(nint pointer)
    {
        if (pointer == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.NullNativeEntry);
        // Read only the two-int prefix before accessing this profile's modern layout.
        if (Marshal.ReadInt32(pointer) != NifNativeAbi.MajorVersion ||
            Marshal.ReadInt32(pointer, sizeof(int)) != NifNativeAbi.MinorVersion)
            throw new NotSupportedException(NifNativeDiagnostics.UnsupportedNativeEntry);
        if (Marshal.SizeOf<NifNativeAbiEntry>() != NifNativeAbi.EntryBytes ||
            Marshal.SizeOf<NifNativeAbiFunction>() != NifNativeAbi.FunctionBytes)
            throw new PlatformNotSupportedException(NifNativeDiagnostics.ManagedLayout);

        return Marshal.PtrToStructure<NifNativeAbiEntry>(pointer);
    }
}
