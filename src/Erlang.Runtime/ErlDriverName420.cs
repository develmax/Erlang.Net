using System.Text;

namespace Erlang;

internal sealed class ErlDriverName420
{
    private readonly byte[] bytes;

    private ErlDriverName420(byte[] value)
    {
        // Native pick_list_or_atom copies bytes, then all registry/path comparison
        // uses C strings. Preserve byte identity; do not collapse invalid UTF8.
        int nul = Array.IndexOf(value, (byte)0);
        bytes = nul < 0 ? value : value.AsSpan(0, nul).ToArray();
        Key = Convert.ToHexString(bytes);
    }

    internal string Key { get; }
    internal ReadOnlySpan<byte> Bytes => bytes;

    internal static ErlDriverName420 Parse(Term value, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        byte[] bytes;
        if (value is Atom atom)
        {
            if (atom.Name.Length == 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
            bytes = Encoding.UTF8.GetBytes(atom.Name);
        }
        else bytes = ErlangIoData.Flatten(value, false, false).ToArray();
        cancellation.ThrowIfCancellationRequested();

        return new(bytes);
    }
}
