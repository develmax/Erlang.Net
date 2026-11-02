using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private static readonly BigInteger SmallBound559 = BigInteger.One << BoxedInteger559Names.SmallMagnitudeBits;

    internal static bool IsImmediateInteger559(Term value)
        => value is Integer number && IsSmallInteger559(number.Value);

    private static bool IsSmallInteger559(BigInteger value)
        => value >= -SmallBound559 && value < SmallBound559;

    // For an Integer, IsSharingLiteral425 only reads the exact CWT marker.
    internal bool IsLiteralInteger559(Term value)
        => value is Integer && runtime.IsSharingLiteral425(value);

    internal Integer CreateInteger559(BigInteger value, ProcessTermConstruction514 construction)
    {
        if (IsSmallInteger559(value))
            return new Integer(value);
        long bits = BigInteger.Abs(value).GetBitLength();
        long count = checked((bits + BoxedInteger559Names.DigitBits - 1) / BoxedInteger559Names.DigitBits);
        if (count > BoxedInteger559Names.MaximumDigits)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        byte[] bytes = BigInteger.Abs(value).ToByteArray(isUnsigned: true, isBigEndian: false);
        var digits = new nuint[(int)count];
        for (int index = 0; index < bytes.Length; index++)
            digits[index / BoxedInteger559Names.DigitBytes] |=
                (nuint)bytes[index] << ((index % BoxedInteger559Names.DigitBytes) * BoxedInteger559Names.ByteBits);
        ProcessTermSequence506 payload = AllocateEncoded526(
            [],
            [BoxedInteger559Names.Header(digits.Length, value.Sign < 0)],
            digits
        );
        try
        {
            var result = new Integer(payload);
            construction.Publish(result);

            return result;
        }
        catch
        {
            ReleaseRegisteredHeader526(payload);
            throw;
        }
    }

    internal BigInteger ReadInteger559(ProcessTermSequence506 payload)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using var reader = domain.EnterRead800(payload);

            return ReadLiteralIntegerCore800(payload);
        }
        lock (Gate) return ReadLiteralIntegerCore800(payload);
    }

    private BigInteger ReadLiteralIntegerCore800(ProcessTermSequence506 payload)
    {
        if (!ReferenceEquals(payload.Owner, this)
            || !blocks.TryGetValue(payload.Identity, out var active)
            || !ReferenceEquals(active, payload))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        int digits = payload.RawPayloadWords556;
        if (payload.Count != 0 || payload.HeaderWords526 != BoxedInteger559Names.HeaderWords
            || digits <= 0 || digits > BoxedInteger559Names.MaximumDigits)
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        nuint header = payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526);
        bool negative = header == BoxedInteger559Names.Header(digits, true);
        if (!negative && header != BoxedInteger559Names.Header(digits, false))
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        int start = checked(payload.AllocationOffset526 + BoxedInteger559Names.HeaderWords);
        var bytes = new byte[checked(digits * BoxedInteger559Names.DigitBytes)];
        nuint high = 0;
        for (int index = 0; index < digits; index++)
        {
            high = payload.Slab.Cells.ReadNumeric556(checked(start + index));
            BinaryPrimitives.WriteUInt64LittleEndian(
                bytes.AsSpan(index * BoxedInteger559Names.DigitBytes, BoxedInteger559Names.DigitBytes),
                (ulong)high
            );
        }
        if (high == 0)
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        BigInteger result = new(bytes, isUnsigned: true, isBigEndian: false);
        if (negative)
            result = -result;
        if (IsSmallInteger559(result))
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);

        return result;
    }
}
