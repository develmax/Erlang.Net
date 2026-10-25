using System.Text;

namespace Erlang;

internal static class DriverOpenCommand422
{
    internal static byte[] Read(Term value, CancellationToken cancellation)
    {
        byte[] result;
        if (value is Atom atom) result = Encoding.UTF8.GetBytes(atom.Name);
        else if (value is BitString { IsBinary: true } binary) result = binary.ToArray();
        else
        {
            if (!SourceLoadNifFilename.IsValidUtf8(value)) throw BadArgument();
            var bytes = new List<byte>();
            var tails = new Stack<Term>();
            Span<byte> encoded = stackalloc byte[4];
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                while (value is Cons cell)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (cell.Head is Integer number)
                    {
                        int length = new Rune((int)number.Value).EncodeToUtf8(encoded);
                        for (int index = 0; index < length; index++) bytes.Add(encoded[index]);
                    }
                    else if (cell.Head is Cons)
                    {
                        tails.Push(cell.Tail);
                        value = cell.Head;
                        continue;
                    }
                    value = cell.Tail;
                }
                if (tails.Count == 0) break;
                value = tails.Pop();
            }
            result = bytes.ToArray();
        }
        if (result.Length == 0 || result.AsSpan().Contains((byte)0)) throw BadArgument();

        return result;
    }

    internal static ErlDriverName420 Name(byte[] command)
    {
        // io.c digs the name out at the first ASCII space only, without shell
        // tokenization, whitespace trimming, UTF8 recoding or quote removal.
        int length = Array.IndexOf(command, (byte)' ');

        return ErlDriverName420.Parse(new BitString(length < 0 ? command : command.AsSpan(0, length).ToArray()), CancellationToken.None);
    }

    private static ErlangException BadArgument() => new(ErlangErrorReasons.BadArgument);
}
