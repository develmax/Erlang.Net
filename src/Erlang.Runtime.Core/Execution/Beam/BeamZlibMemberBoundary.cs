using System.Buffers;

namespace Erlang;

internal static class BeamZlibMemberBoundary
{
    public static int Measure(ReadOnlySpan<byte> input, int capacity)
    {
        if (input.Length < BeamDeflateFormat.HeaderBytes + BeamDeflateFormat.AdlerBytes || (input[0] & BeamDeflateFormat.CompressionMethodMask) != BeamDeflateFormat.CompressionMethod || (input[0] >> BeamDeflateFormat.WindowCodeShift) > BeamDeflateFormat.MaximumWindowCode || ((input[0] << BeamDeflateFormat.ByteBits) + input[1]) % BeamDeflateFormat.HeaderModulus != 0 || (input[1] & BeamDeflateFormat.DictionaryFlag) != 0)
            throw new InvalidDataException();
        int deflateBytes = MeasureDeflate(input[BeamDeflateFormat.HeaderBytes..], capacity);
        int length = checked(BeamDeflateFormat.HeaderBytes + deflateBytes + BeamDeflateFormat.AdlerBytes);
        if (length > input.Length)
            throw new InvalidDataException();

        return length;
    }

    internal static int MeasureDeflate(ReadOnlySpan<byte> input, int capacity)
    {
        var reader = new Bits(input);
        long produced = 0;
        bool final;
        do
        {
            final = reader.Read(BeamDeflateFormat.FinalFlagBits) != 0;
            int kind = reader.Read(BeamDeflateFormat.BlockKindBits);
            if (kind == BeamDeflateFormat.StoredBlock)
            {
                reader.Align();
                int count = reader.Read(BeamDeflateFormat.SymbolBits);
                if ((count ^ reader.Read(BeamDeflateFormat.SymbolBits)) != BeamDeflateFormat.StoredLengthMask)
                    throw new InvalidDataException();
                reader.Skip(checked(count * BeamDeflateFormat.ByteBits));
                produced += count;
            }
            else
            {
                if (kind == BeamDeflateFormat.ReservedBlock)
                    throw new InvalidDataException();
                int[] literals;
                int[] distances;
                if (kind == BeamDeflateFormat.FixedBlock)
                {
                    literals = new int[BeamDeflateFormat.FixedLiteralSymbols];
                    Array.Fill(
                        literals,
                        BeamDeflateFormat.FixedLowBits,
                        0,
                        BeamDeflateFormat.FixedLowSymbols
                    );
                    Array.Fill(
                        literals,
                        BeamDeflateFormat.FixedHighBits,
                        BeamDeflateFormat.FixedLowSymbols,
                        BeamDeflateFormat.FixedHighSymbols
                    );
                    Array.Fill(
                        literals,
                        BeamDeflateFormat.FixedLengthBits,
                        BeamDeflateFormat.EndBlock,
                        BeamDeflateFormat.FixedLengthSymbols
                    );
                    Array.Fill(
                        literals,
                        BeamDeflateFormat.FixedLastBits,
                        BeamDeflateFormat.FixedLastSymbol,
                        BeamDeflateFormat.FixedLastSymbols
                    );
                    distances = new int[BeamDeflateFormat.FixedDistanceSymbols];
                    Array.Fill(distances, BeamDeflateFormat.FixedDistanceBits);
                }
                else
                {
                    int literalCount = reader.Read(BeamDeflateFormat.DynamicLiteralCountBits) + BeamDeflateFormat.MinimumLiteralCount;
                    int distanceCount = reader.Read(BeamDeflateFormat.DynamicDistanceCountBits) + BeamDeflateFormat.MinimumDistanceCount;
                    int codeCount = reader.Read(BeamDeflateFormat.DynamicCodeCountBits) + BeamDeflateFormat.MinimumCodeLengthCount;
                    if (literalCount > BeamDeflateFormat.MaximumLiteralCount)
                        throw new InvalidDataException();
                    var codeLengths = new int[BeamDeflateFormat.CodeLengthSymbols];
                    for (int index = 0; index < codeCount; index++)
                        codeLengths[BeamDeflateFormat.CodeLengthOrder[index]] = reader.Read(BeamDeflateFormat.CodeLengthBits);
                    using var lengthsTree = new Huffman(codeLengths);
                    var lengths = new int[literalCount + distanceCount];
                    int at = 0;
                    while (at < lengths.Length)
                    {
                        int symbol = lengthsTree.Read(ref reader);
                        if (symbol < BeamDeflateFormat.RepeatPreviousSymbol)
                            lengths[at++] = symbol;
                        else
                        {
                            int value = 0;
                            int repeat;
                            switch (symbol)
                            {
                                case BeamDeflateFormat.RepeatPreviousSymbol:
                                    if (at == 0)
                                        throw new InvalidDataException();
                                    value = lengths[at - 1];
                                    repeat = reader.Read(BeamDeflateFormat.RepeatPreviousBits) + BeamDeflateFormat.RepeatPreviousMinimum;
                                    break;
                                case BeamDeflateFormat.RepeatShortZeroSymbol:
                                    repeat = reader.Read(BeamDeflateFormat.RepeatShortZeroBits) + BeamDeflateFormat.RepeatShortZeroMinimum;
                                    break;
                                case BeamDeflateFormat.RepeatLongZeroSymbol:
                                    repeat = reader.Read(BeamDeflateFormat.RepeatLongZeroBits) + BeamDeflateFormat.RepeatLongZeroMinimum;
                                    break;
                                default:
                                    throw new InvalidDataException();
                            }
                            if (repeat > lengths.Length - at)
                                throw new InvalidDataException();
                            Array.Fill(
                                lengths,
                                value,
                                at,
                                repeat
                            );
                            at += repeat;
                        }
                    }
                    literals = lengths[..literalCount];
                    distances = lengths[literalCount..];
                }
                if (literals[BeamDeflateFormat.EndBlock] == 0)
                    throw new InvalidDataException();
                using var literalTree = new Huffman(literals);
                using var distanceTree = new Huffman(distances);
                while (true)
                {
                    int symbol = literalTree.Read(ref reader);
                    if (symbol == BeamDeflateFormat.EndBlock)
                        break;
                    if (symbol < BeamDeflateFormat.EndBlock)
                        produced++;
                    else
                    {
                        if (symbol > BeamDeflateFormat.MaximumLengthSymbol)
                            throw new InvalidDataException();
                        int index = symbol - BeamDeflateFormat.FirstLength;
                        int count = BeamDeflateFormat.LengthBase[index] + reader.Read(BeamDeflateFormat.LengthExtra[index]);
                        int distanceSymbol = distanceTree.Read(ref reader);
                        if (distanceSymbol > BeamDeflateFormat.MaximumDistanceSymbol)
                            throw new InvalidDataException();
                        int extra = distanceSymbol < BeamDeflateFormat.DirectDistanceSymbols ? 0 : (distanceSymbol >> 1) - 1;
                        int distanceBase = distanceSymbol < BeamDeflateFormat.DirectDistanceSymbols ? distanceSymbol + 1 : ((BeamDeflateFormat.PairedDistanceBase + (distanceSymbol & 1)) << extra) + 1;
                        int distance = distanceBase + reader.Read(extra);
                        if (distance > produced || distance > BeamDeflateFormat.WindowBytes)
                            throw new InvalidDataException();
                        produced += count;
                    }
                    if (produced > capacity)
                        throw new InvalidDataException();
                }
            }
            if (produced > capacity)
                throw new InvalidDataException();
        } while (!final);
        reader.Align();

        return reader.PositionBytes;
    }

    private ref struct Bits(ReadOnlySpan<byte> input)
    {
        private readonly ReadOnlySpan<byte> input = input;
        private int position;
        public int PositionBytes => position / BeamDeflateFormat.ByteBits;

        public int Peek(int count)
        {
            if (count == 0)
                return 0;
            int available = Math.Min(count, checked(input.Length * BeamDeflateFormat.ByteBits) - position);
            if (available <= 0)
                throw new InvalidDataException();
            int first = position / BeamDeflateFormat.ByteBits;
            int shift = position % BeamDeflateFormat.ByteBits;
            uint value = 0;
            int bytes = (shift + available + BeamDeflateFormat.ByteBits - 1) / BeamDeflateFormat.ByteBits;
            for (int index = 0; index < bytes; index++)
                value |= (uint)input[first + index] << (index * BeamDeflateFormat.ByteBits);

            return (int)((value >> shift) & ((1u << available) - 1));
        }

        public int Read(int count)
        {
            int value = Peek(count);
            Skip(count);

            return value;
        }

        public void Skip(int count)
        {
            if (count < 0 || count > checked(input.Length * BeamDeflateFormat.ByteBits) - position)
                throw new InvalidDataException();
            position += count;
        }

        public void Align() => Skip((BeamDeflateFormat.ByteBits - position % BeamDeflateFormat.ByteBits) % BeamDeflateFormat.ByteBits);
    }

    private sealed class Huffman : IDisposable
    {
        private readonly int[] table;
        private readonly int bits;

        public Huffman(ReadOnlySpan<int> lengths)
        {
            Span<int> counts = stackalloc int[BeamDeflateFormat.MaximumCodeBits + 1];
            counts.Clear();
            foreach (int length in lengths)
            {
                if (length < 0 || length > BeamDeflateFormat.MaximumCodeBits)
                    throw new InvalidDataException();
                if (length != 0)
                {
                    counts[length]++;
                    bits = Math.Max(bits, length);
                }
            }
            int remaining = 1;
            Span<int> next = stackalloc int[BeamDeflateFormat.MaximumCodeBits + 1];
            next.Clear();
            int code = 0;
            for (int length = 1; length <= bits; length++)
            {
                remaining = (remaining << 1) - counts[length];
                if (remaining < 0)
                    throw new InvalidDataException();
                code = (code + counts[length - 1]) << 1;
                next[length] = code;
            }
            table = ArrayPool<int>.Shared.Rent(1 << bits);
            table.AsSpan(0, 1 << bits).Clear();
            for (int symbol = 0; symbol < lengths.Length; symbol++)
            {
                int length = lengths[symbol];
                if (length == 0)
                    continue;
                int canonical = next[length]++;
                int reversed = 0;
                for (int index = 0; index < length; index++)
                {
                    reversed = (reversed << 1) | (canonical & 1);
                    canonical >>= 1;
                }
                int value = (length << BeamDeflateFormat.SymbolBits) | symbol;
                for (int index = reversed; index < (1 << bits); index += 1 << length)
                    table[index] = value;
            }
        }

        public int Read(ref Bits reader)
        {
            int value = table[reader.Peek(bits)];
            int length = value >> BeamDeflateFormat.SymbolBits;
            if (length == 0)
                throw new InvalidDataException();
            reader.Skip(length);

            return value & BeamDeflateFormat.SymbolMask;
        }

        public void Dispose() => ArrayPool<int>.Shared.Return(table);
    }
}
