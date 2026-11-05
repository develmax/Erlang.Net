using System.Globalization;

namespace Erlang;

internal static class PrimFileDeleteNames
{
    internal const string WindowsEntry = "DeleteFileW";
    internal const string UnixLibrary = "libc";
    internal const string UnixEntry = "unlink";
    internal const string Invalid = "einval";
    internal const string Permission = "eperm";
    internal const string Denied = "eacces";
    internal const string Unsupported = "enotsup";
    internal const int WindowsAccessDenied = 5;
    internal const int WindowsInvalidName = 123;
    internal const byte PathTerminator = 0;

    internal static string UnknownError(int error) => "errno_" + error.ToString(CultureInfo.InvariantCulture);
}
