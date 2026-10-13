using System.ComponentModel;

namespace Erlang;

internal sealed class NativePortIOException(int error) : IOException(new Win32Exception(error).Message)
{
    public int Error { get; } = error;
}
