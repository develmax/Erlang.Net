namespace Erlang;

public sealed class RuntimeStandardStreams : IDisposable
{
    private readonly object gate = new();
    private readonly Stream? output;
    private readonly Stream? error;
    private bool disposed;

    public RuntimeStandardStreams(Stream standardOutput, Stream standardError)
    {
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        output = standardOutput;
        error = standardError;
    }

    private RuntimeStandardStreams()
    {
    }

    internal static RuntimeStandardStreams Open() => new();

    internal void Write(string device, Term value)
    {
        if (device is not (ErlangStandardStreamNames.StandardOutput or ErlangStandardStreamNames.StandardError or ErlangStandardStreamNames.StandardInput))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var terminal = device == ErlangStandardStreamNames.StandardInput ? UnixStandardInput.Open() : null;
        if (value is BitString binary)
        {
            if (!binary.IsBinary)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (terminal is not null)
                    terminal.Write(binary.ToArray());
                else
                {
                    var supplied = device == ErlangStandardStreamNames.StandardOutput ? output : error;
                    using var native = supplied is null ? NativeStandardStreams.Open(device == ErlangStandardStreamNames.StandardError) : null;
                    var stream = supplied ?? native!;
                    binary.WriteTo(stream);
                    stream.Flush();
                }
            }

            return;
        }
        var bytes = StandardStreamText.Encode(value);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (terminal is not null)
                terminal.Write(bytes.Span);
            else
            {
                var supplied = device == ErlangStandardStreamNames.StandardOutput ? output : error;
                using var native = supplied is null ? NativeStandardStreams.Open(device == ErlangStandardStreamNames.StandardError) : null;
                var stream = supplied ?? native!;
                stream.Write(bytes.Span);
                stream.Flush();
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
        }
    }
}
