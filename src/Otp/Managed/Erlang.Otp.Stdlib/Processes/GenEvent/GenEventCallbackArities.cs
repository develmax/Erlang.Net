namespace Erlang;

internal static class GenEventCallbackArities
{
    internal const int Init = 1;
    internal const int HandleEvent = 2;
    internal const int HandleCall = 2;
    internal const int HandleInfo = 2;
    internal const int Terminate = 2;
    internal const int CodeChange = 3;
    internal const int LegacyFormatStatus = 2;
    internal const int FormatStatus = 1;
    internal const int LegacyFormatLog = 1;
    internal const int FormatLog = 2;
}
