using Microsoft.Win32.SafeHandles;

namespace Erlang;

/// <summary>Shared host file leaves used by libraries and prim_file bindings.</summary>
public static class RuntimeFileOperations
{
    /// <summary>Truncates at the stream cursor, retaining the host's native error.</summary>
    public static Term TruncateFile(FileStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!OperatingSystem.IsWindows()) return TruncatePosix(stream.SafeFileHandle);
        if (!stream.CanWrite) return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.Denied);
        stream.SetLength(stream.Position);

        return Term.A(CommonAtomNames.Ok);
    }

    internal static Term TruncatePosix(SafeFileHandle handle)
    {
        int error = PrimFilePosixNative.Truncate(checked((int)handle.DangerousGetHandle()));

        return error == 0 ? Term.A(CommonAtomNames.Ok)
            : Term.Tuple(Term.A(CommonAtomNames.Error), Term.A(PrimFileUnixErrno.Error(error, OperatingSystem.IsMacOS())));
    }
}
