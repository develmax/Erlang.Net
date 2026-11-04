namespace Erlang;

internal static class RamFileDriverProtocol
{
    internal const string Name = "ram_file_drv";
    internal const byte Open = 1;
    internal const byte Read = 2;
    internal const byte Seek = 3;
    internal const byte Write = 4;
    internal const byte Sync = 9;
    internal const byte Truncate = 14;
    internal const byte Pread = 17;
    internal const byte Pwrite = 18;
    internal const byte DataSync = 19;
    internal const byte Get = 30;
    internal const byte Size = 37;
    internal const byte Advise = 38;
    internal const byte Allocate = 39;
    internal const byte OkResponse = 0;
    internal const byte ErrorResponse = 1;
    internal const byte DataResponse = 2;
    internal const byte NumberResponse = 3;
    internal const int ReadMode = 1;
    internal const int WriteMode = 2;
    internal const int WordBytes = 4;
    internal const int ResponseHeaderBytes = 5;
    internal const int Beginning = 0;
    internal const int Current = 1;
    internal const int End = 2;
    internal const string Invalid = "einval";
    internal const string BadDescriptor = "ebadf";
    internal const string Denied = "eacces";
    internal const string NoMemory = "enomem";
}
