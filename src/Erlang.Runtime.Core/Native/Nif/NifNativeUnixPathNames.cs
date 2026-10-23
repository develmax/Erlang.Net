namespace Erlang;

internal static class NifNativeUnixPathNames
{
    internal const string Library = "libc";
    internal const string Open = "open";
    internal const string GetWorkingDirectory = "getcwd";
    internal const int CloseOnExec = 0x80000;
    internal const int RangeError = 34;
    internal const int MissingFile = 2;
    internal const int NotDirectory = 20;
    internal const int AccessDenied = 13;
    internal const int InitialDirectoryBytes = 4096;
    internal const byte Separator = (byte)'/';
}
