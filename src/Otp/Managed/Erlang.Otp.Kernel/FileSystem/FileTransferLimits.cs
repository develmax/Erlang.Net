namespace Erlang;

internal static class FileTransferLimits
{
    internal const int CopyChunkSize = 65536;
    internal const int MaximumSendfileChunkSize = 1 << 20;
    internal const int BitsPerByte = 8;
    internal const int SendfileUntilEof = 0;
}
