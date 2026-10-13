namespace Erlang;

internal static class LinuxPortParameters
{
    public const string Library = "libc.so.6";
    public const int FileActionsBytes = 80;
    public const int CloseOnExec = 0x80000;
    public const int NonBlocking = 0x800;
    public const int DuplicateCloseOnExec = 1030;
    public const int GetFlags = 3;
    public const int SetFlags = 4;
    public const int MinimumPipeDescriptor = 5;
    public const int StandardInput = 0;
    public const int StandardOutput = 1;
    public const int StandardError = 2;
    public const int AuxiliaryInput = 3;
    public const int AuxiliaryOutput = 4;
    public const int Interrupted = 4;
    public const int WouldBlock = 11;
    public const int NoChild = 10;
    public const int BrokenPipe = 32;
    public const int BadDescriptor = 9;
    public const int PollDelayMilliseconds = 1;
    public const int ExitPollMilliseconds = 10;
    public const int WaitNoHang = 1;
    public const int SignalMask = 0x7f;
    public const int ExitStatusShift = 8;
    public const int SignaledExitBase = 128;
}
