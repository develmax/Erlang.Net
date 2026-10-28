using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapMaterialized505NativeModuleHandle
{
    [DllImport(
        MapMaterialized505CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapMaterialized505CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
