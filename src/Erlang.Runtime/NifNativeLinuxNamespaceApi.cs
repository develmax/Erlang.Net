using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal static unsafe class NifNativeLinuxNamespaceApi
{
    internal static bool ExperimentalBaseCopies => Environment.GetEnvironmentVariable(NifNativeLinuxNamespaceNames.ExperimentalBaseCopiesVariable) == "1";

    internal static nint Open(nint domain, byte[] path)
    {
        byte[] terminated = new byte[checked(path.Length + 1)];
        path.CopyTo(terminated, 0);
        nint handle;
        fixed (byte* pointer = terminated)
            handle = Dlmopen(ExperimentalBaseCopies ? NifNativeLinuxNamespaceNames.BaseNamespace : domain, pointer, NifNativeLinuxNamespaceNames.Immediate | (ExperimentalBaseCopies ? NifNativeLinuxNamespaceNames.DeepBind : 0));
        if (handle == 0)
        {
            string reason = Marshal.PtrToStringUTF8(Dlerror()) ?? NifNativeLinuxNamespaceNames.Unavailable;
            throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, new InvalidOperationException(reason));
        }

        return handle;
    }

    internal static nint Identity(nint handle)
    {
        if (Dlinfo(handle, NifNativeLinuxNamespaceNames.NamespaceRequest, out nint identity) != 0 || identity == 0 && !ExperimentalBaseCopies)
            throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, new InvalidOperationException(NifNativeLinuxNamespaceNames.Unavailable));

        return identity;
    }

    internal static nint Symbol(nint handle, string name)
    {
        try
        {
            return NifNativePlatformLoader.Symbol(handle, name);
        }
        catch (InvalidOperationException lookupFailure) when (lookupFailure.Message == NifNativeDiagnostics.BridgeContract)
        {
            throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, lookupFailure);
        }
    }

    internal static FileStream ExperimentalCopy(FileStream source, string expectedHash)
    {
        FileStream copy = Capsule([]);
        try
        {
            source.Position = 0;
            source.CopyTo(copy);
            copy.Position = 0;
            string actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(copy));
            if (!actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
            copy.Position = 0;

            return copy;
        }
        catch
        {
            copy.Dispose();
            throw;
        }
    }

    internal static FileStream Capsule(byte[] contents)
    {
        int descriptor = MemfdCreate(NifNativeLinuxNamespaceNames.CapsuleMemoryFile, NifNativeLinuxNamespaceNames.CloseOnExec);
        if (descriptor < 0)
            throw new IOException(NifNativeLinuxNamespaceNames.Unavailable);
        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            var file = new FileStream(handle, FileAccess.ReadWrite);
            try
            {
                file.Write(contents);
                file.Position = 0;

                return file;
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    internal static byte[] Descriptor(FileStream file)
        => System.Text.Encoding.ASCII.GetBytes(NifNativeNames.LinuxDescriptorPrefix + file.SafeFileHandle.DangerousGetHandle().ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlmopen", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Dlmopen(nint identity, byte* path, int flags);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlinfo", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Dlinfo(nint handle, int request, out nint identity);

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Dlerror();

    [DllImport(NifNativeLinuxNamespaceNames.Libc, EntryPoint = "memfd_create", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern int MemfdCreate([MarshalAs(UnmanagedType.LPUTF8Str)] string name, uint flags);
}
