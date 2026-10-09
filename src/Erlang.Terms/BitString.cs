using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class BitString : Term
{
    private readonly byte[] bytes;
    public int BitLength
    {
        get;
    }
    public bool IsBinary => BitLength % 8 == 0;

    public BitString(ReadOnlySpan<byte> data, int? bitLength = null)
    {
        BitLength = bitLength ?? checked(data.Length * 8);
        if (BitLength < 0 || BitLength > data.Length * 8 || (BitLength + 7) / 8 != data.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bytes = data.ToArray();
        if (bytes.Length > 0 && BitLength % 8 != 0)
            bytes[^1] &= (byte)(0xff << (8 - BitLength % 8));
    }

    public byte[] ToArray() => (byte[])bytes.Clone();

    internal int Bit(int i) => (bytes[i / 8] >> (7 - i % 8)) & 1;

    public override string ToString() => IsBinary ? "<<" + string.Join(',', bytes) + ">>" : "<<" + string.Join(
        ',',
        bytes[..^1].Select(x => x.ToString(CultureInfo.InvariantCulture)).Append($"{bytes[^1] >> (8 - BitLength % 8)}:{BitLength % 8}")
    ) + ">>";
}
