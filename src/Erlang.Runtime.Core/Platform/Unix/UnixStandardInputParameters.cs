namespace Erlang;

internal static class UnixStandardInputParameters
{
    public const string TerminalPath = "/dev/tty";
    public const string NativeLibrary = "libc";
    public const string IoctlEntryPoint = "ioctl";
    public const uint LinuxInputInjectionRequest = 0x5412;
    public const uint BsdInputInjectionRequest = 0x80017472;
}
