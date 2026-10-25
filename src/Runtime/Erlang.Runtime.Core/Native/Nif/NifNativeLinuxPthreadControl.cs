using System.Runtime.InteropServices;
using System.Text.Json;

namespace Erlang;

internal static unsafe class NifNativeLinuxPthreadControl
{
    private static readonly nint libc = NativeLibrary.Load(NifNativeLinuxPthreadControlNames.Libc);
    private static readonly nint[] providers = NifNativeLinuxPthreadControlNames.Symbols.Select(name => NativeLibrary.GetExport(libc, name)).ToArray();

    internal static void Bind(nint bridge)
    {
        var register = (delegate* unmanaged[Cdecl]<nint*, uint, int>)NifNativeLinuxNamespaceApi.Symbol(bridge, NifNativeLinuxPthreadControlNames.Register);
        fixed (nint* values = providers)
            if (register(values, (uint)providers.Length) != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
        var provider = (delegate* unmanaged[Cdecl]<uint, nint>)NifNativeLinuxNamespaceApi.Symbol(bridge, NifNativeLinuxPthreadControlNames.Provider);
        for (uint index = 0; index < providers.Length; index++)
            if (provider(index) != providers[index])
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
    }

    internal static string BindingWitness(nint bridge, nint capsule)
    {
        var rows = new List<object>();
        for (int index = 0; index < providers.Length; index++)
        {
            string name = NifNativeLinuxPthreadControlNames.Symbols[index];
            nint wrapper = NifNativeLinuxNamespaceApi.Symbol(bridge, name);
            nint lookup = NifNativeLinuxNamespaceApi.Symbol(capsule, name);
            rows.Add(new { Name = name, BaseProvider = Address(providers[index]), Wrapper = Address(wrapper), CapsuleLookup = Address(lookup), CapsuleLookupMatchesWrapper = lookup == wrapper, BaseDiffersFromWrapper = providers[index] != wrapper, BaseOwner = Owner(providers[index]), WrapperOwner = Owner(wrapper) });
        }

        return JsonSerializer.Serialize(rows);
    }

    internal static string CallsWitness(nint bridge)
    {
        var calls = (delegate* unmanaged[Cdecl]<uint, ulong>)NifNativeLinuxNamespaceApi.Symbol(bridge, NifNativeLinuxPthreadControlNames.Calls);

        return JsonSerializer.Serialize(NifNativeLinuxPthreadControlNames.Symbols.Select((name, index) => new { Name = name, Calls = calls((uint)index) }).ToArray());
    }

    private static string Address(nint value) => ((nuint)value).ToString("X", System.Globalization.CultureInfo.InvariantCulture);

    private static object Owner(nint symbol)
    {
        if (Dladdr(symbol, out NifNativeLinuxSymbolOwner owner) == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);

        return new { Path = Marshal.PtrToStringUTF8(owner.Path), Base = Address(owner.ImageBase) };
    }

    [DllImport(NifNativeNames.LoaderLibrary, EntryPoint = "dladdr", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Dladdr(nint symbol, out NifNativeLinuxSymbolOwner owner);
}
