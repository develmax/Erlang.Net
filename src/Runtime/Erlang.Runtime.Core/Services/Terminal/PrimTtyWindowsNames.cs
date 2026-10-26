namespace Erlang;

internal static class PrimTtyWindowsNames
{
    internal const string KernelLibrary = "kernel32.dll", CrtLibrary = "ucrtbase.dll";
    internal const int StandardInput = -10;
    internal const int StandardOutput = -11;
    internal const int StandardError = -12;
    internal const uint TypeDisk = 1, TypeCharacter = 2, TypePipe = 3;
    internal const uint GenericRead = 0x80000000, GenericWrite = 0x40000000;
    internal const uint OpenExisting = 3, NormalAttributes = 0x80;
    internal const uint BrokenPipe = 109;
    internal const string NullDevice = "NUL";
    internal const string ConsoleRefusal = "This prerequisite kernel accepts only actual redirected Windows handles; console mode mutation is not implemented";
}
