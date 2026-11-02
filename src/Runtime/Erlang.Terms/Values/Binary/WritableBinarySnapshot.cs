namespace Erlang;

internal sealed record WritableBinarySnapshot(byte[] Bytes, int ApparentBytes, bool CurrentWriter);
