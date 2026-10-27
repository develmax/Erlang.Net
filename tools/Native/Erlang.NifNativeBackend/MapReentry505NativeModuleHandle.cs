using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapReentry505NativeModuleHandle
{
    [DllImport(
        MapReentry505CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapReentry505CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
