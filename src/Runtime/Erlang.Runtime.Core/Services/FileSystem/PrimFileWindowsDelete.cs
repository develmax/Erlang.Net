using System.Runtime.InteropServices;

namespace Erlang;

// OTP-29.1.1 win_prim_file.c efile_del_file, Apache-2.0, Ericsson AB.
internal static class PrimFileWindowsDelete
{
    internal static string? Delete(Term term)
    {
        if (!PrimFileWindowsPaths410.Marshal(term, out string path, out string error))
        {
            return error;
        }
        if (PrimFileWindowsDeleteNative.Delete(path))
        {
            return null;
        }
        int nativeError = Marshal.GetLastPInvokeError();
        if (nativeError == PrimFileDeleteNames.WindowsInvalidName)
        {
            return PrimFileDeleteNames.Denied;
        }
        if (nativeError == PrimFileDeleteNames.WindowsAccessDenied)
        {
            uint attributes = PrimFileWindowsNative410.Attributes(path);
            if (attributes != PrimFileWindowsNames410.InvalidAttributes
                && (attributes & PrimFileWindowsNames410.DirectoryAttribute) != 0)
            {
                return PrimFileDeleteNames.Permission;
            }
        }

        return PrimFileWindowsErrors410.Map(nativeError);
    }
}
