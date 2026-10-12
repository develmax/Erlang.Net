using System.Buffers;
using System.Text;

namespace Erlang;

internal static class StandardStreamText
{
    public static ReadOnlyMemory<byte> Encode(Term value)
    {
        var bytes = new ArrayBufferWriter<byte>();
        foreach (var item in Cons.Items(value))
        {
            if (item is not Integer character || character.Value.Sign < 0 || character.Value > int.MaxValue || !Rune.TryCreate((int)character.Value, out var rune))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            int count = rune.EncodeToUtf8(bytes.GetSpan(rune.Utf8SequenceLength));
            bytes.Advance(count);
        }

        return bytes.WrittenMemory;
    }
}
