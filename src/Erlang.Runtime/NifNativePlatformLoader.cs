using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativePlatformLoader
{
    internal static void VerifyPlatform()
    {
        NifNativeLifecycleGate.RequireDefaultProvider();
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64 ||
            !(OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            throw new PlatformNotSupportedException(NifNativeDiagnostics.UnsupportedPlatform);
    }

    internal static nint Open(
        FileStream? file,
        string path,
        bool global,
        bool useDescriptor = true
    ) => Open(
        file,
        NifNativeLibraryPath.FromHostPath(path),
        global,
        useDescriptor
    );

    internal static nint Open(
        FileStream? file,
        NifNativeLibraryPath path,
        bool global,
        bool useDescriptor = true
    )
    {
        VerifyPlatform();
        if (OperatingSystem.IsWindows())
        {
            nint windowsHandle = LoadLibraryEx(path.DisplayPath, 0, NifNativeWindowsLoaderNames.AlteredSearchPath);
            if (windowsHandle == 0)
            {
                var failure = new Win32Exception(Marshal.GetLastPInvokeError());
                throw new DllNotFoundException(failure.Message, failure);
            }

            return windowsHandle;
        }
        // Reviewed images use the verified descriptor. Ordinary host loading preserves
        // the lexical pathname and its native relative dependency resolution.
        NifNativeLibraryPath loadPath = useDescriptor
            ? NifNativeLibraryPath.FromHostPath(NifNativeNames.LinuxDescriptorPrefix + (file ?? throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract)).SafeFileHandle.DangerousGetHandle().ToString())
            : path;
        byte[] bytes = loadPath.UnixNullTerminated();
        nint handle;
        fixed (byte* pointer = bytes)
            handle = DlopenBytes(pointer, NifNativeNames.LinuxImmediate | (global ? NifNativeNames.LinuxGlobal : 0));
        if (handle == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

        return handle;
    }

    internal static nint OpenNifLibrary(FileStream? file, string path, bool useDescriptor = true) =>
        OpenNifLibrary(file, NifNativeLibraryPath.FromHostPath(path), useDescriptor);

    internal static nint OpenNifLibrary(FileStream? file, NifNativeLibraryPath path, bool useDescriptor = true)
    {
        try
        {
            return Open(
                file,
                path,
                global: false,
                useDescriptor: useDescriptor
            );
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
    private static extern nint DlopenBytes(byte* path, int flags);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Dlsym(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlclose", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Dlclose(nint handle);
}
