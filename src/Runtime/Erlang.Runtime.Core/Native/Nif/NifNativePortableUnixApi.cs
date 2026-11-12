using System.Runtime.InteropServices;

namespace Erlang;

// Constants and libc entry points are compiled from the target OS headers in C.
// No Linux RTLD/O_CLOEXEC values or libdl sonames cross this boundary.
internal static unsafe class NifNativePortableUnixApi
{
    private static readonly object Gate = new();
    private static nint library;
    private static string? path;
    private static delegate* unmanaged[Cdecl]<byte*, int, nint> open;
    private static delegate* unmanaged[Cdecl]<nint, byte*, nint> symbol;
    private static delegate* unmanaged[Cdecl]<nint, int> close;
    private static delegate* unmanaged[Cdecl]<nint> error;
    private static delegate* unmanaged[Cdecl]<byte*, int> openFile;
    private static delegate* unmanaged[Cdecl]<byte*, nuint, int> workingDirectory;

    internal static nint EnsureLoaded(string? selected = null)
    {
        NifNativeLifecycleGate.RequireDefaultProvider();
        string requested = Path.GetFullPath(selected ?? Path.Combine(AppContext.BaseDirectory, NifNativePortableNames.DefaultBridge));
        lock (Gate)
        {
            if (library != 0)
            {
                if (selected is not null && !string.Equals(path, requested, StringComparison.Ordinal))
                    throw new InvalidOperationException(NifNativePortableNames.BridgeMismatch);

                return library;
            }
            nint candidate = NativeLibrary.Load(requested);
            try
            {
                var candidateOpen = (delegate* unmanaged[Cdecl]<byte*, int, nint>)NativeLibrary.GetExport(candidate, NifNativePortableNames.Open);
                var candidateSymbol = (delegate* unmanaged[Cdecl]<nint, byte*, nint>)NativeLibrary.GetExport(candidate, NifNativePortableNames.Symbol);
                var candidateClose = (delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(candidate, NifNativePortableNames.Close);
                var candidateError = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(candidate, NifNativePortableNames.Error);
                var candidateOpenFile = (delegate* unmanaged[Cdecl]<byte*, int>)NativeLibrary.GetExport(candidate, NifNativePortableNames.OpenFile);
                var candidateDirectory = (delegate* unmanaged[Cdecl]<byte*, nuint, int>)NativeLibrary.GetExport(candidate, NifNativePortableNames.WorkingDirectory);
                open = candidateOpen;
                symbol = candidateSymbol;
                close = candidateClose;
                error = candidateError;
                openFile = candidateOpenFile;
                workingDirectory = candidateDirectory;
                path = requested;
                library = candidate;

                // Process-owned, including failed later NIF admissions: published native
                // function pointers must never refer to an unloaded bridge.
                return library;
            }
            catch
            {
                NativeLibrary.Free(candidate);
                throw;
            }
        }
    }

    internal static nint Open(byte[] terminated, bool global)
    {
        EnsureLoaded();
        fixed (byte* pointer = terminated)
        {
            nint handle = open(pointer, global ? 1 : 0);
            if (handle == 0)
                throw new DllNotFoundException(Marshal.PtrToStringUTF8(error()) ?? NifNativeDiagnostics.BridgeContract);

            return handle;
        }
    }

    internal static nint Symbol(nint handle, string name)
    {
        EnsureLoaded();
        byte[] terminated = System.Text.Encoding.UTF8.GetBytes(name + '\0');
        fixed (byte* pointer = terminated)
            return symbol(handle, pointer);
    }

    internal static void Close(nint handle)
    {
        EnsureLoaded();
        if (close(handle) != 0)
            throw new InvalidOperationException(Marshal.PtrToStringUTF8(error()) ?? NifNativeDiagnostics.BridgeContract);
    }

    internal static int OpenFile(byte* filename)
    {
        EnsureLoaded();

        return openFile(filename);
    }

    internal static int WorkingDirectory(byte* buffer, nuint size)
    {
        EnsureLoaded();

        return workingDirectory(buffer, size);
    }
}
