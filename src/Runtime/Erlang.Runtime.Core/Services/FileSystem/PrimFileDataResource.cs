namespace Erlang;

internal sealed class PrimFileDataResource(FileStream stream, string path, bool append) : IDisposable
{
    internal object Gate { get; } = new();
    internal FileStream Stream { get; } = stream;
    internal string Path { get; } = path;
    internal bool Append { get; } = append;
    internal bool Closed { get; private set; }
    private int closing;
    internal bool IsClosing => Volatile.Read(ref closing) != 0;

    internal void RequestClose() => Volatile.Write(ref closing, 1);

    public void Dispose()
    {
        RequestClose();
        lock (Gate)
        {
            if (Closed)
            {
                return;
            }
            Closed = true;
            Stream.Dispose();
        }
    }
}
