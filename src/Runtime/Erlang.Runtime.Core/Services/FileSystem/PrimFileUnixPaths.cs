namespace Erlang;

internal static class PrimFileUnixPaths
{
    // Match efile_marshal_path: raw bytes, at least one NUL, only NULs afterward.
    // Do not decode/re-encode Unix filenames or resolve their symbolic links.
    internal static byte[]? Marshal(Term term)
    {
        if (term is not BitString { IsBinary: true } binary)
        {
            return null;
        }
        byte[] bytes = binary.ToArray();
        int terminator = Array.IndexOf(bytes, PrimFileDeleteNames.PathTerminator);
        if (terminator < 0)
        {
            return null;
        }
        for (int index = terminator; index < bytes.Length; index++)
        {
            if (bytes[index] != PrimFileDeleteNames.PathTerminator)
            {
                return null;
            }
        }

        return bytes;
    }
}
