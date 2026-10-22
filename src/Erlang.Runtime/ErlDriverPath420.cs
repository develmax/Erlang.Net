using System.Text;

namespace Erlang;

// Byte identity is used by the DDLL registry before Windows filename decoding.
// Reuses the native-filename validator only, never the NIF loader/path policy.
internal sealed class ErlDriverPath420
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly byte[] bytes;

    private ErlDriverPath420(byte[] bytes)
    {
        int nul = Array.IndexOf(bytes, (byte)0);
        this.bytes = nul < 0 ? bytes : bytes.AsSpan(0, nul).ToArray();
        Key = Convert.ToHexString(this.bytes);
        try
        {
            WindowsPath = StrictUtf8.GetString(this.bytes);
        }
        catch (DecoderFallbackException)
        {
            WindowsPath = Encoding.Latin1.GetString(this.bytes);
        }
    }

    internal string Key { get; }
    internal string WindowsPath { get; }
    internal int ByteCount => bytes.Length;

    internal byte[] CopyBytes() => bytes.ToArray();

    internal static ErlDriverPath420 Parse(Term path, ErlDriverName420 name, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (!SourceLoadNifFilename.IsValidUtf8(path)) throw new ErlangException(ErlangErrorReasons.BadArgument);
        byte[] directory;
        if (path is BitString binary) directory = binary.ToArray();
        else
        {
            var text = new StringBuilder();
            var pending = new Stack<Term>();
            pending.Push(path);
            while (pending.TryPop(out var item))
            {
                cancellation.ThrowIfCancellationRequested();
                if (item is Cons cell)
                {
                    pending.Push(cell.Tail);
                    pending.Push(cell.Head);
                }
                else if (item is Integer point) text.Append(char.ConvertFromUtf32((int)point.Value));
            }
            directory = StrictUtf8.GetBytes(text.ToString());
        }
        int end = directory.Length;
        while (end > 0 && (directory[end - 1] == ErlDriverPathNames420.Slash || directory[end - 1] == ErlDriverPathNames420.Backslash)) end--;
        byte[] full = new byte[checked(end + 1 + name.Bytes.Length)];
        directory.AsSpan(0, end).CopyTo(full);
        full[end] = ErlDriverPathNames420.Slash;
        name.Bytes.CopyTo(full.AsSpan(end + 1));
        cancellation.ThrowIfCancellationRequested();

        // In the native source the name is appended using used byte length,
        // then platform strlen truncates the completed path at its first NUL.
        return new(full);
    }

    internal static ErlDriverPath420 FromWire(byte[] bytes) => new(bytes.ToArray());
}
