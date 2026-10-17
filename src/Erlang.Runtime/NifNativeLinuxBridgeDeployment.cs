using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeLinuxBridgeDeployment
{
    internal static NifNativeLinuxBridgeInput Open(NifNativeCapabilityProfile profile)
    {
        byte[] path = profile.BridgePath is { } selected
            ? Encoding.UTF8.GetBytes(Path.GetFullPath(selected))
            : ExistingHostBridge() ?? Encoding.UTF8.GetBytes(Path.Combine(AppContext.BaseDirectory, NifNativeLinuxNamespaceNames.DefaultBridge));
        var file = NifNativeUnixFile.OpenRead(NifNativeLibraryPath.FromUnixAbsolute(path));
        try
        {
            string hash = Convert.ToHexString(SHA256.HashData(file));
            file.Position = 0;
            if (profile.BridgeSha256 is { } expected && !hash.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(NifNativeDiagnostics.UntrustedLibraryHash);

            return new(file, hash);
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    private static byte[]? ExistingHostBridge()
    {
        nint symbol;
        try
        {
            symbol = NifNativePlatformLoader.Symbol(0, NifNativeNames.BridgeRegister);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        if (Dladdr(symbol, out NifNativeLinuxSymbolOwner owner) == 0 || owner.Path == 0)
            return null;
        var bytes = new List<byte>();
        for (byte* current = (byte*)owner.Path; *current != 0; current++)
            bytes.Add(*current);

        return bytes.Count != 0 && bytes[0] == NifNativeUnixPathNames.Separator ? bytes.ToArray() : null;
    }

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dladdr", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Dladdr(nint symbol, out NifNativeLinuxSymbolOwner owner);
}
