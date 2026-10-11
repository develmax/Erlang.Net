using System.Buffers.Binary;

namespace Erlang;

internal static class ErlangBlockHash
{
    private const uint GoldenRatio = 0x9e3779b9;
    private const int BlockBytes = 12;
    private const int WordBytes = 4;
    private const int ByteBits = 8;
    private const int LengthByteOffset = 1;
    private const int FirstMixRight = 13;
    private const int FirstMixLeft = 8;
    private const int SecondMixRight = 13;
    private const int ThirdMixRight = 12;
    private const int SecondMixLeft = 16;
    private const int FourthMixRight = 5;
    private const int FifthMixRight = 3;
    private const int ThirdMixLeft = 10;
    private const int SixthMixRight = 15;

    public static uint Hash(ReadOnlySpan<byte> bytes, uint seed)
    {
        unchecked
        {
            uint a = GoldenRatio, b = GoldenRatio, c = seed;
            int offset = 0;
            while (bytes.Length - offset >= BlockBytes)
            {
                a += BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
                b += BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + WordBytes)..]);
                c += BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + WordBytes * 2)..]);
                Mix(ref a, ref b, ref c);
                offset += BlockBytes;
            }
            c += (uint)bytes.Length;
            var tail = bytes[offset..];
            for (int index = 0; index < tail.Length; index++)
            {
                if (index < WordBytes)
                    a += (uint)tail[index] << (index * ByteBits);
                else if (index < WordBytes * 2)
                    b += (uint)tail[index] << ((index - WordBytes) * ByteBits);
                else
                    c += (uint)tail[index] << ((index - WordBytes * 2 + LengthByteOffset) * ByteBits);
            }
            Mix(ref a, ref b, ref c);

            return c;
        }
    }

    public static void Mix(ref uint a, ref uint b, ref uint c)
    {
        unchecked
        {
            a -= b;
            a -= c;
            a ^= c >> FirstMixRight;
            b -= c;
            b -= a;
            b ^= a << FirstMixLeft;
            c -= a;
            c -= b;
            c ^= b >> SecondMixRight;
            a -= b;
            a -= c;
            a ^= c >> ThirdMixRight;
            b -= c;
            b -= a;
            b ^= a << SecondMixLeft;
            c -= a;
            c -= b;
            c ^= b >> FourthMixRight;
            a -= b;
            a -= c;
            a ^= c >> FifthMixRight;
            b -= c;
            b -= a;
            b ^= a << ThirdMixLeft;
            c -= a;
            c -= b;
            c ^= b >> SixthMixRight;
        }
    }
}
