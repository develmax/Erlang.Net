using System.Buffers;

namespace Erlang;

internal static class ZlibMemberBoundary
{
    public static int Measure(ReadOnlySpan<byte> input, int expected)
    {
        if (input.Length < DeflateFormat.HeaderBytes + DeflateFormat.AdlerBytes || (input[0] & DeflateFormat.CompressionMethodMask) != DeflateFormat.CompressionMethod || (input[0] >> DeflateFormat.WindowCodeShift) > DeflateFormat.MaximumWindowCode || ((input[0] << DeflateFormat.ByteBits) + input[1]) % DeflateFormat.HeaderModulus != 0 || (input[1] & DeflateFormat.DictionaryFlag) != 0)
            throw new InvalidDataException();
        var reader = new Bits(input[DeflateFormat.HeaderBytes..]);
        long produced = 0;
        bool final;
        do
        {
            final = reader.Read(DeflateFormat.FinalFlagBits) != 0;
            int kind = reader.Read(DeflateFormat.BlockKindBits);
            if (kind == DeflateFormat.StoredBlock)
            {
                reader.Align();
                int count = reader.Read(DeflateFormat.SymbolBits);
                if ((count ^ reader.Read(DeflateFormat.SymbolBits)) != DeflateFormat.StoredLengthMask)
                    throw new InvalidDataException();
                reader.Skip(checked(count * DeflateFormat.ByteBits));
                produced += count;
            }
            else
            {
                if (kind == DeflateFormat.ReservedBlock)
                    throw new InvalidDataException();
                int[] literals;
                int[] distances;
                if (kind == DeflateFormat.FixedBlock)
                {
                    literals = new int[DeflateFormat.FixedLiteralSymbols];
                    Array.Fill(
                        literals,
                        DeflateFormat.FixedLowBits,
                        0,
                        DeflateFormat.FixedLowSymbols
                    );
                    Array.Fill(
                        literals,
                        DeflateFormat.FixedHighBits,
                        DeflateFormat.FixedLowSymbols,
                        DeflateFormat.FixedHighSymbols
                    );
                    Array.Fill(
                        literals,
                        DeflateFormat.FixedLengthBits,
                        DeflateFormat.EndBlock,
                        DeflateFormat.FixedLengthSymbols
                    );
                    Array.Fill(
                        literals,
                        DeflateFormat.FixedLastBits,
                        DeflateFormat.FixedLastSymbol,
                        DeflateFormat.FixedLastSymbols
                    );
                    distances = new int[DeflateFormat.FixedDistanceSymbols];
                    Array.Fill(distances, DeflateFormat.FixedDistanceBits);
                }
                else
                {
                    int literalCount = reader.Read(DeflateFormat.DynamicLiteralCountBits) + DeflateFormat.MinimumLiteralCount;
                    int distanceCount = reader.Read(DeflateFormat.DynamicDistanceCountBits) + DeflateFormat.MinimumDistanceCount;
                    int codeCount = reader.Read(DeflateFormat.DynamicCodeCountBits) + DeflateFormat.MinimumCodeLengthCount;
                    if (literalCount > DeflateFormat.MaximumLiteralCount)
                        throw new InvalidDataException();
                    var codeLengths = new int[DeflateFormat.CodeLengthSymbols];
                    for (int index = 0; index < codeCount; index++)
                        codeLengths[DeflateFormat.CodeLengthOrder[index]] = reader.Read(DeflateFormat.CodeLengthBits);
                    using var lengthsTree = new Huffman(codeLengths);
                    var lengths = new int[literalCount + distanceCount];
                    int at = 0;
                    while (at < lengths.Length)
                    {
                        int symbol = lengthsTree.Read(ref reader);
                        if (symbol < DeflateFormat.RepeatPreviousSymbol)
                            lengths[at++] = symbol;
                        else
                        {
                            int value = 0;
                            int repeat;
                            switch (symbol)
                            {
                                case DeflateFormat.RepeatPreviousSymbol:
                                    if (at == 0)
                                        throw new InvalidDataException();
                                    value = lengths[at - 1];
                                    repeat = reader.Read(DeflateFormat.RepeatPreviousBits) + DeflateFormat.RepeatPreviousMinimum;
                                    break;
                                case DeflateFormat.RepeatShortZeroSymbol:
                                    repeat = reader.Read(DeflateFormat.RepeatShortZeroBits) + DeflateFormat.RepeatShortZeroMinimum;
                                    break;
                                case DeflateFormat.RepeatLongZeroSymbol:
                                    repeat = reader.Read(DeflateFormat.RepeatLongZeroBits) + DeflateFormat.RepeatLongZeroMinimum;
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
                if (literals[DeflateFormat.EndBlock] == 0)
                    throw new InvalidDataException();
                using var literalTree = new Huffman(literals);
                using var distanceTree = new Huffman(distances);
                while (true)
                {
                    int symbol = literalTree.Read(ref reader);
                    if (symbol == DeflateFormat.EndBlock)
                        break;
                    if (symbol < DeflateFormat.EndBlock)
                        produced++;
                    else
                    {
                        if (symbol > DeflateFormat.MaximumLengthSymbol)
                            throw new InvalidDataException();
                        int index = symbol - DeflateFormat.FirstLength;
                        int count = DeflateFormat.LengthBase[index] + reader.Read(DeflateFormat.LengthExtra[index]);
                        int distanceSymbol = distanceTree.Read(ref reader);
                        if (distanceSymbol > DeflateFormat.MaximumDistanceSymbol)
                            throw new InvalidDataException();
                        int extra = distanceSymbol < DeflateFormat.DirectDistanceSymbols ? 0 : (distanceSymbol >> 1) - 1;
                        int distanceBase = distanceSymbol < DeflateFormat.DirectDistanceSymbols ? distanceSymbol + 1 : ((DeflateFormat.PairedDistanceBase + (distanceSymbol & 1)) << extra) + 1;
                        int distance = distanceBase + reader.Read(extra);
                        if (distance > produced || distance > DeflateFormat.WindowBytes)
                            throw new InvalidDataException();
                        produced += count;
                    }
                    if (produced > expected)
                        throw new InvalidDataException();
                }
            }
            if (produced > expected)
                throw new InvalidDataException();
        } while (!final);
        if (produced != expected)
            throw new InvalidDataException();
        reader.Align();
        reader.Skip(DeflateFormat.AdlerBytes * DeflateFormat.ByteBits);

        return DeflateFormat.HeaderBytes + reader.PositionBytes;
    }

    private ref struct Bits(ReadOnlySpan<byte> input)
    {
        private readonly ReadOnlySpan<byte> input = input;
        private int position;
        public int PositionBytes => position / DeflateFormat.ByteBits;

        public int Peek(int count)
        {
            if (count == 0)
                return 0;
            int available = Math.Min(count, checked(input.Length * DeflateFormat.ByteBits) - position);
            if (available <= 0)
                throw new InvalidDataException();
            int first = position / DeflateFormat.ByteBits;
            int shift = position % DeflateFormat.ByteBits;
            uint value = 0;
            int bytes = (shift + available + DeflateFormat.ByteBits - 1) / DeflateFormat.ByteBits;
            for (int index = 0; index < bytes; index++)
                value |= (uint)input[first + index] << (index * DeflateFormat.ByteBits);

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
            if (count < 0 || count > checked(input.Length * DeflateFormat.ByteBits) - position)
                throw new InvalidDataException();
            position += count;
        }

        public void Align() => Skip((DeflateFormat.ByteBits - position % DeflateFormat.ByteBits) % DeflateFormat.ByteBits);
    }

    private sealed class Huffman : IDisposable
    {
        private readonly int[] table;
        private readonly int bits;

        public Huffman(ReadOnlySpan<int> lengths)
        {
            Span<int> counts = stackalloc int[DeflateFormat.MaximumCodeBits + 1];
            counts.Clear();
            foreach (int length in lengths)
            {
                if (length < 0 || length > DeflateFormat.MaximumCodeBits)
                    throw new InvalidDataException();
                if (length != 0)
                {
                    counts[length]++;
                    bits = Math.Max(bits, length);
                }
            }
            int remaining = 1;
            Span<int> next = stackalloc int[DeflateFormat.MaximumCodeBits + 1];
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
                int value = (length << DeflateFormat.SymbolBits) | symbol;
                for (int index = reversed; index < (1 << bits); index += 1 << length)
                    table[index] = value;
            }
        }

        public int Read(ref Bits reader)
        {
            int value = table[reader.Peek(bits)];
            int length = value >> DeflateFormat.SymbolBits;
            if (length == 0)
                throw new InvalidDataException();
            reader.Skip(length);

            return value & DeflateFormat.SymbolMask;
        }

        public void Dispose() => ArrayPool<int>.Shared.Return(table);
    }
}
