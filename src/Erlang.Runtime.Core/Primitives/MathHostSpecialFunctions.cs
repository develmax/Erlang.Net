using System.Runtime.InteropServices;

namespace Erlang;

internal static class MathHostSpecialFunctions
{
    internal static double Erf(double value)
    {
        if (OperatingSystem.IsWindows())
            return WindowsErf(value);
        if (OperatingSystem.IsLinux())
            return LinuxErf(value);
        if (OperatingSystem.IsMacOS())
            return MacErf(value);

        throw new PlatformNotSupportedException(MathNativeNames.UnsupportedPlatform);
    }

    internal static double Erfc(double value)
    {
        if (OperatingSystem.IsWindows())
            return WindowsErfc(value);
        if (OperatingSystem.IsLinux())
            return LinuxErfc(value);
        if (OperatingSystem.IsMacOS())
            return MacErfc(value);

        throw new PlatformNotSupportedException(MathNativeNames.UnsupportedPlatform);
    }

    [DllImport(MathNativeNames.WindowsRuntime, EntryPoint = MathNativeNames.Erf, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern double WindowsErf(double value);

    [DllImport(MathNativeNames.WindowsRuntime, EntryPoint = MathNativeNames.Erfc, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern double WindowsErfc(double value);

    [DllImport(MathNativeNames.LinuxMath, EntryPoint = MathNativeNames.Erf, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern double LinuxErf(double value);

    [DllImport(MathNativeNames.LinuxMath, EntryPoint = MathNativeNames.Erfc, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern double LinuxErfc(double value);

    [DllImport(MathNativeNames.MacRuntime, EntryPoint = MathNativeNames.Erf, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern double MacErf(double value);

    [DllImport(MathNativeNames.MacRuntime, EntryPoint = MathNativeNames.Erfc, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern double MacErfc(double value);
}
