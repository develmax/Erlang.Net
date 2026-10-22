using System.Runtime.InteropServices;

namespace Erlang;

// This policy is intentionally the pinned driver policy, independent of NIF
// loader admission. Execute only inside the dedicated own driver-host process.
internal static class ErlDriverWindowsLoader420
{
    private const uint AlteredSearchPath = 0x00000008;

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string path, nint reserved, uint flags);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern nint GetProcAddress(nint image, string name);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "FreeLibrary", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(nint image);

    [DllImport(ErlDriverLoadErrors420.Kernel, EntryPoint = "SetDllDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDllDirectory(string path);

    internal static void InitializeSearchPath(string startupDirectory)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!SetDllDirectory(startupDirectory))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
    }

    internal static (nint Image, int Error) Open(string unextendedPath, int nativePathByteCount)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (nativePathByteCount >= ErlDriverLoadErrors420.MaximumPathBytes - ErlDriverLoadErrors420.FileExtensionBytes)
            return (nint.Zero, ErlDriverLoadErrors420.NameTooLong);
        nint image = LoadLibraryEx(unextendedPath + ErlDriverLoadErrors420.FileExtension, nint.Zero, AlteredSearchPath);

        return image == nint.Zero
            ? (image, checked(ErlDriverLoadErrors420.DynamicOffset - Marshal.GetLastPInvokeError()))
            : (image, ErlDriverLoadErrors420.NoError);
    }

    internal static nint InitializeSymbol(nint image) => GetProcAddress(image, ErlDriverLoadErrors420.EntrySymbol);

    internal static int Close(nint image) => FreeLibrary(image)
        ? ErlDriverLoadErrors420.NoError
        : checked(ErlDriverLoadErrors420.DynamicOffset - Marshal.GetLastPInvokeError());
}
