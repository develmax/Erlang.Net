using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

internal static class MapFamily504NativeModuleHandle
{
    [DllImport(
        MapFamily504CallbackNames.WindowsKernelLibrary,
        EntryPoint = MapFamily504CallbackNames.WindowsModuleQuery,
        CharSet = CharSet.Unicode,
        ExactSpelling = true
    )]
    internal static extern nint Find(string absoluteLibraryPath);
}
