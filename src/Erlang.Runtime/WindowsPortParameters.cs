namespace Erlang;

internal static class WindowsPortParameters
{
    public const string Library = "kernel32.dll";
    public const string PipePrefix = "\\\\.\\pipe\\erlang-net-";
    public const string NullDevice = "NUL";
    public const uint GenericRead = 0x80000000;
    public const uint GenericWrite = 0x40000000;
    public const uint OpenExisting = 3;
    public const uint NormalFile = 0x80;
    public const uint Overlapped = 0x40000000;
    public const uint PipeInbound = 1;
    public const uint PipeTimeoutMilliseconds = 2000;
    public const uint StartUseStandardHandles = 0x100;
    public const uint ExtendedStartup = 0x80000;
    public const uint UnicodeEnvironment = 0x400;
    public const uint NoWindow = 0x8000000;
    public const nuint HandleListAttribute = 0x20002;
    public const int StandardError = -12;
    public const uint DuplicateSameAccess = 2;
    public const uint WaitObject = 0;
    public const uint WaitTimeout = 258;
    public const uint WaitFailed = 0xffffffff;
    public const int ExitPollMilliseconds = 10;
    public const int StreamBufferBytes = 4096;
    public const int PathBufferCharacters = 32768;
    public const string CurrentDirectorySearchPath = ".";
    public const string ExecutableExtension = ".exe";
    public static readonly string[] SearchExtensions = [string.Empty, ".com", ExecutableExtension, ".bat"];
}
