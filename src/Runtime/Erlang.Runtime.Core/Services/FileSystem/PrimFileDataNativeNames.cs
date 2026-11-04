namespace Erlang;

internal static class PrimFileDataNativeNames
{
    internal const string Library = "kernel32.dll";
    internal const string Delete = "DeleteFileW";
    internal const string MakeDirectory = "CreateDirectoryW";
    internal const string Rename = "MoveFileExW";
    internal const string RemoveDirectory = "RemoveDirectoryW";
    internal const string Write = "WriteFile";
    internal const string Position = "SetFilePointerEx";
    internal const uint MoveCopyAllowed = 0x2;
    internal const uint MoveWriteThrough = 0x8;
    internal const int FileExists = 80;
    internal const int AlreadyExists = 183;
    internal const int AccessDenied = 5;
    internal const int WriteFault = 29;
    internal const uint AppendOffsetWord = uint.MaxValue;
}
