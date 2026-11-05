using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativePlatformPolicy
{
    internal static bool IsSupported => Supports(
        OperatingSystem.IsWindows(),
        OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD(),
        RuntimeInformation.ProcessArchitecture,
        IntPtr.Size
    );

    // Windows x86 admission includes native-word storage and GC execution checks.
    // Other 32-bit hosts remain outside the validated adapter profile.
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
