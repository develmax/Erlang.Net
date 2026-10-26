using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapCreation501NativeModuleHandle
{
    [DllImport(
        MapCreation501CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapCreation501CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
