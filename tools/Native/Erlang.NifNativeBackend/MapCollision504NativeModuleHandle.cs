using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapCollision504NativeModuleHandle
{
    [DllImport(
        MapCollision504CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapCollision504CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
