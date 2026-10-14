using System.Numerics;

namespace Erlang;

internal static class BeamCompactReader
{
    public static BeamCompactValue Read(BeamCursor cursor) => Read(cursor, 0);

    private static BeamCompactValue Read(BeamCursor cursor, int depth)
    {
        if (depth > BeamClrLimits.MaximumSizePrefixDepth)
        {
            throw new InvalidDataException(BeamDiagnostics.SizePrefixDepth);
        }
        byte prefix = cursor.ReadByte();
        int tag = prefix & BeamWire.TagMask;
        if ((prefix & BeamWire.LongFlag) == 0)
        {
            return new(
                tag,
                prefix >> BeamWire.CompactSmallPayloadShift,
                true,
                0
            );
        }
        if ((prefix & BeamWire.LargeFlag) == 0)
        {
            return new(
                tag,
                ((prefix >> BeamWire.CompactMediumHighShift) << BeamWire.CompactMediumWordShift) | cursor.ReadByte(),
                true,
                0
            );
        }
        int lengthCode = prefix >> BeamWire.CompactLargeSizeShift;
        int length;
        if (lengthCode < BeamWire.ExtendedSizeCode)
        {
            length = lengthCode + BeamWire.ShortLargeBase;
        }
        else
        {
            BeamCompactValue size = Read(cursor, depth + 1);
            if (size.WireTag != BeamWire.UnsignedTag || !size.FitsNativeWord || size.Value < 0
                || size.Value >= BeamClrLimits.MaximumInputBytes - BeamWire.ExtendedLargeBase)
            {
                throw new InvalidDataException(BeamDiagnostics.ExtendedSizePrefix);
            }
            length = (int)size.Value + BeamWire.ExtendedLargeBase;
        }
        ReadOnlySpan<byte> payload = cursor.Read(length).Span;
        var value = new BigInteger(payload, isUnsigned: tag != BeamWire.IntegerTag, isBigEndian: true);
        bool word = length <= BeamWire.WordBytes && value >= long.MinValue && value <= long.MaxValue;
        if (tag == BeamWire.IntegerTag)
        {
            BigInteger bound = BigInteger.One << (BeamWire.SmallIntegerValueBits - 1);
            word = word && value >= -bound && value < bound;
        }

        return new(
            tag,
            value,
            word,
            length
        );
    }
}
