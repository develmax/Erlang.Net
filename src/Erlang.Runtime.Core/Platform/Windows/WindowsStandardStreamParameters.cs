namespace Erlang;

internal static class WindowsStandardStreamParameters
{
    public const string NativeLibrary = "kernel32.dll";
    public const string HandleEntryPoint = "GetStdHandle";
    public const int OutputHandle = -11;
    public const int ErrorHandle = -12;
    public const int InvalidHandle = -1;
    public const int UnbufferedWriteSize = 1;
}
