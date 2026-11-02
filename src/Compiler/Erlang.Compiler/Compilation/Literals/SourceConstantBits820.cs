using System.Numerics;

namespace Erlang.Compiler;

internal static class SourceConstantBits820
{
    private const int AlwaysSuitableBits = 256;
    private const int ApproximationChunkBits = 64;

    internal static bool TryRead(Expr.Bits expression, out Term value)
    {
        var segments = new List<(Term Value, Term? Size, BitSegment Segment)>();
        foreach (BitSegment segment in expression.Segments)
        {
            // Match the pinned v3_core constant_bin scalar-only admission.
            // In particular, never fold native endian or giant sparse fields.
            if (segment.Endian is not (BitByteOrders.Big or BitByteOrders.Little)
                || segment.Value is not Expr.Literal literal
                || segment.Size is not (null or Expr.Literal))
            {
                value = null!;

                return false;
            }
            Term? size = (segment.Size as Expr.Literal)?.Value;
            if (!Suitable(literal.Value, size, segment))
            {
                value = null!;

                return false;
            }
            segments.Add((literal.Value, size, segment));
        }
        try
        {
            value = BitConstruction.Create(segments);

            return true;
        }
        catch (ErlangException)
        {
            // A known failed language construction must fail at the original
            // runtime point, preserving earlier expressions and source location.
            value = null!;

            return false;
        }
    }

    private static bool Suitable(Term value, Term? size, BitSegment segment)
    {
        if (BitUnicode.IsUtf(segment.Type))
            return size is null && (value is Integer || segment.IsStringLiteral);
        if (segment.Type is not (BitSegmentTypes.Integer or BitSegmentTypes.Float)) return false;
        if (value is not (Integer or FloatTerm) && !segment.IsStringLiteral) return false;
        BigInteger width = size switch
        {
            null => segment.Type == BitSegmentTypes.Integer ? BitSyntaxDefaults.IntegerWidth : BitSyntaxDefaults.FloatWidth,
            Integer number => number.Value,
            _ => -1
        };
        width *= segment.Unit;
        if (width < 0) return false;
        if (width <= AlwaysSuitableBits) return true;
        if (value is not Integer integer) return false;
        long bits = BigInteger.Abs(integer.Value).GetBitLength();
        long approximate = ApproximationChunkBits + ((bits + ApproximationChunkBits - 1) / ApproximationChunkBits) * ApproximationChunkBits;

        return width <= 2 * new BigInteger(approximate);
    }
}
