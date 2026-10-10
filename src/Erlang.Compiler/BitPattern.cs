using System.Numerics;

namespace Erlang.Compiler;

public sealed record BitPattern(IReadOnlyList<BitPatternSegment> Segments) : Pattern
{
    internal static BitPattern FromExpression(Expr.Bits bits)
    {
        var segments = new List<BitPatternSegment>();
        for (int i = 0; i < bits.Segments.Count; i++)
        {
            var segment = bits.Segments[i];
            if (segment.IsStringLiteral)
                throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, BitPatternDiagnostics.StringModifiers, 0);
            bool whole = segment.Type == BitSegmentTypes.Binary && (segment.Size is null || segment.Size is Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } });
            if (whole && i != bits.Segments.Count - 1)
                throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, BitPatternDiagnostics.UnsizedBinaryNotLast, 0);
            var value = Parser.ToPattern(segment.Value);
            if (segment.Type == BitSegmentTypes.Float && value is Pattern.Literal { Value: Integer integer })
            {
                if (!integer.TryToDouble(out double number))
                    throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, BitPatternDiagnostics.FloatLiteralOutOfRange, 0);
                value = new Pattern.Literal(new FloatTerm(number));
            }
            if (value is not Pattern.Variable && !((segment.Type == BitSegmentTypes.Integer || BitUnicode.IsUtf(segment.Type)) && value is Pattern.Literal { Value: Integer }) && !(segment.Type == BitSegmentTypes.Float && value is Pattern.Literal { Value: FloatTerm }))
                throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, BitPatternDiagnostics.UnsupportedSegmentValue, 0);
            segments.Add(new(value, segment));
        }

        return new(segments);
    }

    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        if (value is not BitString input)
            return false;

        return ReadFields(
            input,
            input.ToArray(),
            0,
            bindings,
            context,
            keyScope,
            false,
            out int consumed,
            out bool matched
        ) && matched && consumed == input.BitLength;
    }

    // Modified: consuming generator scan follows eval_bits:bin_gen/8.
    // Failed fields retain prior bindings but still consume their encoded bits.
    internal bool ReadGenerator(
        BitString input,
        byte[] bytes,
        int start,
        Dictionary<string, Term> bindings,
        ProcessContext context,
        Dictionary<string, Term> keyScope,
        out int consumed,
        out bool matched
    ) => ReadFields(
        input,
        bytes,
        start,
        bindings,
        context,
        keyScope,
        true,
        out consumed,
        out matched
    );

    private bool ReadFields(
        BitString input,
        byte[] bytes,
        int start,
        Dictionary<string, Term> bindings,
        ProcessContext? context,
        Dictionary<string, Term>? keyScope,
        bool generator,
        out int consumed,
        out bool matched
    )
    {
        int position = start;
        consumed = 0;
        matched = true;
        var sizeScope = new Dictionary<string, Term>(keyScope ?? bindings, StringComparer.Ordinal);

        int ReadBit()
        {
            int bit = (bytes[position / BitStorageLayout.BitsPerByte] >> (BitStorageLayout.MostSignificantBitIndex - position % BitStorageLayout.BitsPerByte)) & 1;
            position++;

            return bit;
        }
        Span<byte> prefix = stackalloc byte[UtfSegmentLimits.MaximumEncodedBytes];

        foreach (var segment in Segments)
        {
            var spec = segment.Specification;

            bool integer = spec.Type == BitSegmentTypes.Integer, floating = spec.Type == BitSegmentTypes.Float;

            if (BitUnicode.IsUtf(spec.Type))
            {
                if (spec.Size is not null || spec.Unit != 1)
                    return false;
                int available = Math.Min((input.BitLength - position) / BitStorageLayout.BitsPerByte, UtfSegmentLimits.MaximumEncodedBytes);
                prefix.Clear();
                for (int i = 0; i < available * BitStorageLayout.BitsPerByte; i++)
                    if (((bytes[(position + i) / BitStorageLayout.BitsPerByte] >> (BitStorageLayout.MostSignificantBitIndex - (position + i) % BitStorageLayout.BitsPerByte)) & 1) != 0)
                        prefix[i / BitStorageLayout.BitsPerByte] |= (byte)(1 << (BitStorageLayout.MostSignificantBitIndex - i % BitStorageLayout.BitsPerByte));
                var scalar = BitUnicode.Decode(
                    prefix[..available],
                    spec.Type,
                    spec.Endian,
                    out int utfConsumed
                );
                if (scalar is null)
                    return false;
                position += utfConsumed * BitStorageLayout.BitsPerByte;
                if (segment.Value.Match(
                    scalar,
                    bindings,
                    context,
                    keyScope
                ))
                {
                    foreach (string name in Semantics.Variables(segment.Value))
                        sizeScope[name] = bindings[name];
                }
                else if (generator)
                    matched = false;
                else
                    return false;
                continue;
            }

            Term? size;
            try
            {
                size = spec.Size is null ? null : Execution.PatternKey(spec.Size, sizeScope, context);
            }
            catch (ErlangException)
            {
                return false;
            }
            bool whole = spec.Type == BitSegmentTypes.Binary && (size is null || size is Atom { Name: BitSizeAtoms.All });
            BigInteger length;
            if (whole)
                length = input.BitLength - position;
            else if (size is null && (integer || floating))
                length = (integer ? BitSyntaxDefaults.IntegerWidth : BitSyntaxDefaults.FloatWidth) * spec.Unit;
            else if (size is Integer width && width.Value >= 0)
                length = width.Value * spec.Unit;
            else
                return false;

            if (length > input.BitLength - position)
                return false;
            int count = (int)length;
            if (floating && count is not (0 or FloatSegmentWidths.Half or FloatSegmentWidths.Single or FloatSegmentWidths.Double))
                return false;
            if (whole && count % spec.Unit != 0)
                return false;

            Term extracted;
            if (integer)
            {
                BigInteger number = BigInteger.Zero;
                bool little = spec.Endian == BitByteOrders.Little || spec.Endian == BitByteOrders.Native && BitConverter.IsLittleEndian;
                if (!little)
                    for (int i = 0; i < count; i++)
                        number = (number << 1) | ReadBit();
                else
                    for (int offset = 0; offset < count; offset += BitStorageLayout.BitsPerByte)
                    {
                        int chunk = 0;
                        for (int i = 0; i < Math.Min(BitStorageLayout.BitsPerByte, count - offset); i++)
                            chunk = (chunk << 1) | ReadBit();
                        number |= new BigInteger(chunk) << offset;
                    }
                if (spec.Signed && count > 0 && (number & (BigInteger.One << (count - 1))) != 0)
                    number -= BigInteger.One << count;
                extracted = new Integer(number);
            }
            else
            {
                var part = new byte[(int)(((long)count + BitStorageLayout.ByteRoundingOffset) / BitStorageLayout.BitsPerByte)];
                for (int i = 0; i < count; i++)
                    if (ReadBit() != 0)
                        part[i / BitStorageLayout.BitsPerByte] |= (byte)(1 << (BitStorageLayout.MostSignificantBitIndex - i % BitStorageLayout.BitsPerByte));
                if (floating)
                {
                    var number = BitFloat.Decode(part, count, spec.Endian);
                    if (number is null)
                    {
                        if (!generator)
                            return false;
                        matched = false;
                        continue;
                    }
                    extracted = number;
                }
                else
                    extracted = new BitString(part, count);
            }
            if (segment.Value.Match(
                extracted,
                bindings,
                context,
                keyScope
            ))
            {
                foreach (string name in Semantics.Variables(segment.Value))
                    sizeScope[name] = bindings[name];
            }
            else if (generator)
                matched = false;
            else
                return false;
        }

        consumed = position - start;

        return true;
    }
}
