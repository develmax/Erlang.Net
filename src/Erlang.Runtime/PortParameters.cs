namespace Erlang;

internal static class PortParameters
{
    public const int ReadBufferBytes = 4096;
    public const int ByteBits = 8;
    public const int MaximumUnicodeCodepoint = 0x10ffff;
    public const int MaximumPacketBytes = 16 * 1024 * 1024;
    public const int FileNotFoundError = 2;
    public const int WindowsPathNotFoundError = 3;
    public const int PacketOne = 1;
    public const int PacketTwo = 2;
    public const int PacketFour = 4;
    public const string WindowsShell = "cmd.exe";
    public const string WindowsShellSwitch = "/c";
    public const string UnixShell = "/bin/sh";
    public const string UnixShellSwitch = "-c";
}
