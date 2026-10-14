using System.Text;

namespace Erlang;

internal static class IoUnicodeCharacters
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static BitString Encode(Term data)
    {
        using var bytes = new MemoryStream();
        var pending = new Stack<(Term Value, bool Scalar)>();
        pending.Push((data, false));
        while (pending.TryPop(out var item))
        {
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons cell:
                    pending.Push((cell.Tail, false));
                    pending.Push((cell.Head, true));
                    break;
                case BitString binary when binary.IsBinary:
                    binary.WriteTo(bytes);
                    break;
                case Integer integer when item.Scalar && integer.Value >= 0 && integer.Value <= IoCharacterNames.MaximumUnicodeScalar && Rune.IsValid((int)integer.Value):
                    bytes.Write(Encoding.UTF8.GetBytes(char.ConvertFromUtf32((int)integer.Value)));
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        byte[] result = bytes.ToArray();
        try
        {
            StrictUtf8.GetCharCount(result);
        }
        catch (DecoderFallbackException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return new(result);
    }

    public static string Text(Term data) => StrictUtf8.GetString(Encode(data).ToArray());
}
