namespace Erlang;

internal static class ErlDriverLoadErrors420
{
    internal const int NoError = 0;
    internal const int NoInitialize = -1;
    internal const int FailedInitialize = -2;
    internal const int BadName = -3;
    internal const int NameTooLong = -4;
    internal const int IncorrectVersion = -5;
    internal const int DynamicOffset = -10;
    // Pinned erts/emulator/sys/win32/erl_win_sys.h MAXPATHLEN.
    internal const int MaximumPathBytes = 4096;
    internal const int FileExtensionBytes = 4;
    internal const string FileExtension = ".dll";
    internal const string EntrySymbol = "driver_init";
    internal const string Kernel = "kernel32.dll";
    internal const string ActiveImage = "Driver image still owns active invocations, ports or native workers";
    internal const string ClosedImage = "Driver image is retiring or closed";
}
