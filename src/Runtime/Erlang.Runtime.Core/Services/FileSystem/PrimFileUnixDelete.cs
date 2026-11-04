using System.Runtime.InteropServices;

namespace Erlang;

// OTP-29.1.1 unix_prim_file.c efile_del_file, Apache-2.0, Ericsson AB.
internal static class PrimFileUnixDelete
{
    internal static string? Delete(Term term)
    {
        byte[]? path = PrimFileUnixPaths.Marshal(term);
        if (path is null)
        {
            return PrimFileDeleteNames.Invalid;
        }
        int result = PrimFileUnixDeleteNative.Unlink(path);
        int error = Marshal.GetLastPInvokeError();

        return result == 0 ? null : PrimFileUnixErrno.DeleteError(error, OperatingSystem.IsMacOS());
    }
}
