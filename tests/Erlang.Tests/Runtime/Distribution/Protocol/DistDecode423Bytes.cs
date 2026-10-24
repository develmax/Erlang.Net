namespace Erlang.Tests;

internal static class DistDecode423Bytes
{
    internal const int UnknownInternalAtomIndex = 0xFFFFFF;

    internal static byte[] InternalAtom(int index, bool wide) => wide
        ? [(byte)131, (byte)75, (byte)(index >> 16), (byte)(index >> 8), (byte)index]
        : [(byte)131, (byte)73, (byte)(index >> 8), (byte)index];

    internal static readonly byte[] StaticLocalFun = [131, 121, 104, 144, 50, 150, 112, 0, 0, 0, 81, 1, 49, 72, 11, 244, 24, 192, 63, 115, 122, 255, 2, 245, 199, 215, 176, 124, 0, 0, 0, 0, 0, 0, 0, 0, 119, 19, 98, 101, 97, 109, 95, 100, 105, 115, 116, 95, 100, 101, 99, 111, 100, 101, 52, 50, 51, 121, 88, 84, 39, 164, 97, 0, 121, 84, 166, 240, 78, 98, 1, 138, 64, 95, 88, 106, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    internal static readonly byte[] StaticNormalFun = [131, 112, 0, 0, 0, 85, 1, 49, 72, 11, 244, 24, 192, 63, 115, 122, 255, 2, 245, 199, 215, 176, 124, 0, 0, 0, 0, 0, 0, 0, 0, 119, 19, 98, 101, 97, 109, 95, 100, 105, 115, 116, 95, 100, 101, 99, 111, 100, 101, 52, 50, 51, 97, 0, 98, 1, 138, 64, 95, 88, 119, 13, 110, 111, 110, 111, 100, 101, 64, 110, 111, 104, 111, 115, 116, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    internal static readonly byte[] CacheZero = [131, 82, 0];
    internal static readonly byte[] CacheLast = [131, 82, 254];
    internal static readonly byte[] CacheTooFar = [131, 82, 255];
    internal static readonly byte[] Pid = [131, 88, 82, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 7];
    internal static readonly byte[] Port = [131, 120, 82, 0, 0, 0, 0, 0, 0, 0, 0, 21, 0, 0, 0, 7];
    internal static readonly byte[] Reference = [131, 90, 0, 3, 82, 0, 0, 0, 0, 7, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3];
    internal static readonly byte[] InvalidNode = [131, 88, 82, 1, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 7];
    internal static readonly byte[] Trailing = [255, 82, 255];
}
