using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativePlatformLoader
{
    internal static void VerifyPlatform()
    {
        NifNativeLifecycleGate.RequireDefaultProvider();
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64 ||
            !(OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            throw new PlatformNotSupportedException(NifNativeDiagnostics.UnsupportedPlatform);
    }

    internal static nint Open(FileStream file, string path, bool global)
    {
        VerifyPlatform();
        if (OperatingSystem.IsWindows())
        {
            nint windowsHandle = LoadLibraryEx(path, 0, NifNativeWindowsLoaderNames.AlteredSearchPath);
            if (windowsHandle == 0)
            {
                var failure = new Win32Exception(Marshal.GetLastPInvokeError());
                throw new DllNotFoundException(failure.Message, failure);
            }

            return windowsHandle;
        }
        // The verified descriptor prevents pathname replacement between hashing and dlopen.
        // This deliberately bounded fixture has no relative private dependencies.
        string descriptorPath = NifNativeNames.LinuxDescriptorPrefix + file.SafeFileHandle.DangerousGetHandle().ToString();
        nint handle = Dlopen(descriptorPath, NifNativeNames.LinuxImmediate | (global ? NifNativeNames.LinuxGlobal : 0));
        if (handle == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

        return handle;
    }

    internal static nint OpenNifLibrary(FileStream file, string path)
    {
        try
        {
            return Open(file, path, global: false);
        }
        catch (Exception failure) when (failure is DllNotFoundException or BadImageFormatException ||
            failure is InvalidOperationException && failure.Message == NifNativeDiagnostics.BridgeContract)
        {
            throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, failure);
        }
    }

    internal static nint NifInitializer(nint handle)
    {
        try
        {
            return Symbol(handle, NifNativeNames.Initializer);
        }
        catch (Exception failure) when (failure is EntryPointNotFoundException ||
            failure is InvalidOperationException && failure.Message == NifNativeDiagnostics.BridgeContract)
        {
            throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.BadLibrary, failure);
        }
    }

    internal static nint Symbol(nint handle, string name)
    {
        nint pointer = OperatingSystem.IsWindows() ? NativeLibrary.GetExport(handle, name) : Dlsym(handle, name);
        if (pointer == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

        return pointer;
    }

    internal static void Close(nint handle)
    {
        if (handle == 0)
            return;
        if (OperatingSystem.IsWindows())
            NativeLibrary.Free(handle);
        else if (Dlclose(handle) != 0)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
    }

    [DllImport(
        NifNativeWindowsLoaderNames.Library,
        EntryPoint = "LoadLibraryExW",
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        SetLastError = true
    )]
    private static extern nint LoadLibraryEx(string path, nint file, uint flags);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Dlsym(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlclose", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Dlclose(nint handle);
}
