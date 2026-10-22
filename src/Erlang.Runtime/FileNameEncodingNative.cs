namespace Erlang;

internal static class FileNameEncodingNative
{
    private static readonly Term Encoding = Term.A(FileNameEncodingNames.Utf8);

    // This VM currently has one filename policy: Unicode host paths and UTF-8
    // Unix bytes. It exposes no +fnl/+fna or mutable locale-derived startup mode.
    internal static bool TryCapture(
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (module == FileNameEncodingNames.Module && function == FileNameEncodingNames.Function && arity == 0)
        {
            body = (_, _) => ValueTask.FromResult(Encoding);

            return true;
        }
        body = null!;

        return false;
    }
}
