using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativePlatformPolicy
{
    internal static bool IsSupported => IntPtr.Size == NifNativeAbi.Pointer64Bytes && Supports(
        OperatingSystem.IsWindows(),
        OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD(),
        RuntimeInformation.ProcessArchitecture,
        IntPtr.Size
    );

    // ABI structure/adapter capability is distinct from complete VM admission.
    // The native-cell/source profile still rejects a 32-bit managed runtime.
    internal static bool Supports(
        bool windows,
        bool unix,
        Architecture architecture,
        int pointerBytes
    )
        => windows
            ? pointerBytes == NifNativeAbi.Pointer32Bytes && architecture == Architecture.X86
                || pointerBytes == NifNativeAbi.Pointer64Bytes && architecture is Architecture.X64 or Architecture.Arm64
            : unix && pointerBytes == NifNativeAbi.Pointer64Bytes;

    // Other Unix hosts use the ordinary process-global OTP symbol model.
    internal static bool UseLinuxNamespace => OperatingSystem.IsLinux()
        && RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64
        && LinuxNamespacesAvailable;

    private static readonly bool LinuxNamespacesAvailable = HasLinuxNamespaces();

    private static bool HasLinuxNamespaces()
    {
        if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad(NifNativeNames.LoaderLibrary, out nint handle))
            return false;
        try
        {
            return NativeLibrary.TryGetExport(handle, "dlmopen", out _)
                && NativeLibrary.TryGetExport(handle, "dlinfo", out _);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }
}
