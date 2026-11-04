namespace Erlang;

internal static class PrimFileUnixErrno
{
    // Linux x64/arm64 and Darwin share errno 1..34 except 11. Numeric errno
    // values are ABI facts, not a Win32 HResult mask or localized messages.
    internal static string DeleteError(int error, bool darwin)
    {
        if (error == PrimFileUnixErrnoValues.IsDirectory)
        {
            return PrimFileDeleteNames.Permission;
        }
        var names = darwin ? PrimFileDarwinErrno.Names : PrimFileLinuxErrno.Names;

        return names.TryGetValue(error, out string? name) ? name : PrimFileDeleteNames.UnknownError(error);
    }
}
