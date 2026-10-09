using System.Numerics;

namespace Erlang.Compiler;

public sealed record BitPatternSegment(Pattern Value, BitSegment Specification);

public sealed record BitPattern(IReadOnlyList<BitPatternSegment> Segments) : Pattern
{
    internal static BitPattern FromExpression(Expr.Bits bits)
    {
        var segments = new List<BitPatternSegment>();
        for (int i = 0; i < bits.Segments.Count; i++)
        {
            var segment = bits.Segments[i];
            bool whole = segment.Type == "binary" && (segment.Size is null || segment.Size is Expr.Literal { Value: Atom { Name: "all" } });
            if (whole && i != bits.Segments.Count - 1) throw new CompileException("ERL004", "Unsized binary pattern segment must be last", 0);
            var value = Parser.ToPattern(segment.Value);
            if (value is not Pattern.Variable && !(segment.Type == "integer" && value is Pattern.Literal { Value: Integer }))
                throw new CompileException("ERL004", "Bit pattern segments support variables and integer literals only", 0);
            segments.Add(new(value, segment));
        }
        return new(segments);
    }

    protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
    {
        if (value is not BitString input) return false;
        byte[] bytes = input.ToArray(); int position = 0;
        var sizeScope = new Dictionary<string, Term>(keyScope ?? bindings, StringComparer.Ordinal);
        int ReadBit() { int bit = (bytes[position / 8] >> (7 - position % 8)) & 1; position++; return bit; }
        foreach (var segment in Segments)
        {
            var spec = segment.Specification; bool integer = spec.Type == "integer";
            Term? size;
            try { size = spec.Size is null ? null : Execution.PatternKey(spec.Size, sizeScope, context); }
            catch (ErlangException) { return false; }
            bool whole = !integer && (size is null || size is Atom { Name: "all" });
            BigInteger length;
            if (whole) length = input.BitLength - position;
            else if (size is null && integer) length = 8 * spec.Unit;
            else if (size is Integer width && width.Value >= 0) length = width.Value * spec.Unit;
            else return false;
            if (length > input.BitLength - position) return false;
            int count = (int)length;
            if (whole && count % spec.Unit != 0) return false;
            Term extracted;
            if (integer)
            {
                BigInteger number = BigInteger.Zero;
                bool little = spec.Endian == "little" || spec.Endian == "native" && BitConverter.IsLittleEndian;
                if (!little) for (int i = 0; i < count; i++) number = (number << 1) | ReadBit();
                else for (int offset = 0; offset < count; offset += 8)
                {
                    int chunk = 0; for (int i = 0; i < Math.Min(8, count - offset); i++) chunk = (chunk << 1) | ReadBit();
                    number |= new BigInteger(chunk) << offset;
                }
                if (spec.Signed && count > 0 && (number & (BigInteger.One << (count - 1))) != 0) number -= BigInteger.One << count;
                extracted = new Integer(number);
            }
            else
            {
                var part = new byte[(int)(((long)count + 7) / 8)];
                for (int i = 0; i < count; i++) if (ReadBit() != 0) part[i / 8] |= (byte)(1 << (7 - i % 8));
                extracted = new BitString(part, count);
            }
            if (!segment.Value.Match(extracted, bindings, context, keyScope)) return false;
            foreach (string name in Semantics.Variables(segment.Value)) sizeScope[name] = bindings[name];
        }
        return position == input.BitLength;
    }
}
