using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

internal static class ErlangMd5Context
{
    public static Term Initialize()
    {
        byte[] context = new byte[ErlangMd5Layout.ContextBytes];
        for (int index = 0; index < ErlangMd5Layout.StateWords; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(context.AsSpan(index * ErlangMd5Layout.WordBytes), ErlangMd5Layout.InitialState[index]);

        return new BitString(context);
    }

    private static byte[] Read(Term value)
    {
        if (value is not BitString { IsBinary: true } binary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] context = binary.ToArray();
        if (context.Length != ErlangMd5Layout.ContextBytes || BinaryPrimitives.ReadUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.PendingLengthOffset)) > ErlangMd5Layout.BlockBytes)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return context;
    }

    public static Term Update(Term value, Term data)
    {
        byte[] context = Read(value);
        foreach (var segment in ErlangIoData.Segments(data, false, false))
            Append(context, segment.ToArray());

        return new BitString(context);
    }

    private static void Append(byte[] context, ReadOnlySpan<byte> data)
    {
        int pending = (int)BinaryPrimitives.ReadUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.PendingLengthOffset));
        ulong total = BinaryPrimitives.ReadUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.TotalLengthOffset));
        if (pending > 0)
        {
            int fill = ErlangMd5Layout.BlockBytes - pending;
            if (fill >= data.Length)
            {
                data.CopyTo(context.AsSpan(ErlangMd5Layout.BufferOffset + pending));
                BinaryPrimitives.WriteUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.PendingLengthOffset), (ulong)(pending + data.Length));

                return;
            }
            data[..fill].CopyTo(context.AsSpan(ErlangMd5Layout.BufferOffset + pending));
            Compress(context, context.AsSpan(ErlangMd5Layout.BufferOffset, ErlangMd5Layout.BlockBytes));
            data = data[fill..];
            total = unchecked(total + ErlangMd5Layout.BlockBytes);
            pending = 0;
        }
        while (data.Length >= ErlangMd5Layout.BlockBytes)
        {
            Compress(context, data[..ErlangMd5Layout.BlockBytes]);
            data = data[ErlangMd5Layout.BlockBytes..];
            total = unchecked(total + ErlangMd5Layout.BlockBytes);
        }
        if (!data.IsEmpty)
        {
            data.CopyTo(context.AsSpan(ErlangMd5Layout.BufferOffset));
            pending = data.Length;
        }
        BinaryPrimitives.WriteUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.PendingLengthOffset), (ulong)pending);
        BinaryPrimitives.WriteUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.TotalLengthOffset), total);
    }

    public static Term Finalize(Term value)
    {
        byte[] context = Read(value);
        int pending = (int)BinaryPrimitives.ReadUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.PendingLengthOffset));
        ulong total = unchecked(BinaryPrimitives.ReadUInt64LittleEndian(context.AsSpan(ErlangMd5Layout.TotalLengthOffset)) + (ulong)pending);
        int paddedBytes = ((pending + ErlangMd5Layout.PaddingOverheadBytes + ErlangMd5Layout.BlockBytes - 1) / ErlangMd5Layout.BlockBytes) * ErlangMd5Layout.BlockBytes;
        Span<byte> padded = stackalloc byte[paddedBytes];
        padded.Clear();
        context.AsSpan(ErlangMd5Layout.BufferOffset, pending).CopyTo(padded);
        padded[pending] = ErlangMd5Layout.PaddingMarker;
        BinaryPrimitives.WriteUInt64LittleEndian(padded[^ErlangMd5Layout.LengthFieldBytes..], unchecked(total * ErlangMd5Layout.BitsPerByte));
        for (int offset = 0; offset < padded.Length; offset += ErlangMd5Layout.BlockBytes)
            Compress(context, padded.Slice(offset, ErlangMd5Layout.BlockBytes));

        return new BitString(context.AsSpan(0, ErlangMd5Layout.DigestBytes));
    }

    private static void Compress(byte[] context, ReadOnlySpan<byte> block)
    {
        uint a = BinaryPrimitives.ReadUInt32LittleEndian(context.AsSpan(0));
        uint b = BinaryPrimitives.ReadUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes));
        uint c = BinaryPrimitives.ReadUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes * 2));
        uint d = BinaryPrimitives.ReadUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes * 3));
        uint savedA = a, savedB = b, savedC = c, savedD = d;
        for (int step = 0; step < ErlangMd5Layout.TransformSteps; step++)
        {
            uint function;
            int word;
            if (step < ErlangMd5Layout.RoundSteps)
            {
                function = d ^ (b & (c ^ d));
                word = step;
            }
            else if (step < ErlangMd5Layout.RoundSteps * 2)
            {
                function = c ^ (d & (b ^ c));
                word = (ErlangMd5Layout.SecondRoundStride * step + ErlangMd5Layout.SecondRoundOffset) & ErlangMd5Layout.WordIndexMask;
            }
            else if (step < ErlangMd5Layout.RoundSteps * 3)
            {
                function = b ^ c ^ d;
                word = (ErlangMd5Layout.ThirdRoundStride * step + ErlangMd5Layout.ThirdRoundOffset) & ErlangMd5Layout.WordIndexMask;
            }
            else
            {
                function = c ^ (b | ~d);
                word = (ErlangMd5Layout.FourthRoundStride * step) & ErlangMd5Layout.WordIndexMask;
            }
            uint mixed = unchecked(function + a + ErlangMd5Layout.Constants[step] + BinaryPrimitives.ReadUInt32LittleEndian(block[(word * ErlangMd5Layout.WordBytes)..]));
            uint rotated = BitOperations.RotateLeft(mixed, ErlangMd5Layout.Shifts[step]);
            a = d;
            d = c;
            c = b;
            b = unchecked(b + rotated);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(context.AsSpan(0), unchecked(savedA + a));
        BinaryPrimitives.WriteUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes), unchecked(savedB + b));
        BinaryPrimitives.WriteUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes * 2), unchecked(savedC + c));
        BinaryPrimitives.WriteUInt32LittleEndian(context.AsSpan(ErlangMd5Layout.WordBytes * 3), unchecked(savedD + d));
    }
}
