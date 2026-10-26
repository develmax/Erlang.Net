using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapValue503NativeModuleHandle
{
    [DllImport(
        MapValue503CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapValue503CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
