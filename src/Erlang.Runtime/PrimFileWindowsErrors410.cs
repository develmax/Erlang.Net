namespace Erlang;

internal static class PrimFileWindowsErrors410
{
    internal const string Executable = "enoexec";
    internal const string TooManyFiles = "emfile";
    internal const string BadDescriptor = "ebadf";
    internal const string NoDeviceAddress = "enxio";
    internal const string CrossDevice = "exdev";
    internal const string ReadOnly = "erofs";
    internal const string TooBig = "e2big";
    internal const string IllegalSeek = "espipe";
    internal const string NoDevice = "enodev";
    internal const string Pipe = "epipe";
    internal const string NoSpace = "enospc";
    internal const string Unsupported = "enotsup";
    internal const string Exists = "eexist";
    internal const string Busy = "ebusy";
    internal const string Again = "eagain";
    internal const string Links = "emlink";
    internal const string Permission = "eperm";
    internal const string Io = "eio";
    internal const string Zero = "errno_0";

    // win_prim_file.c windows_to_posix_errno. The numeric values are WinError.h.
    internal static string Map(int error) => error switch
    {
        0 => Zero,
        1 or 13 or 87 or 114 or 117 or 131 => PrimFileWindowsNames410.Invalid,
        145 or 80 or 183 or 82 => Exists,
        11 => Executable,
        3 or 2 or 18 or 123 => PrimFileWindowsNames410.Missing,
        4 or 36 or 113 => TooManyFiles,
        5 or 12 or 16 or 32 or 33 or 86 or 108 => PrimFileWindowsNames410.Denied,
        6 => BadDescriptor,
        8 or 14 or 84 => PrimFileWindowsNames410.NoMemory,
        15 or 20 or 21 or 51 or 52 or 53 or 54 or 55 or 67 => NoDeviceAddress,
        17 => CrossDevice,
        19 => ReadOnly,
        24 or 111 => TooBig,
        25 or 27 => IllegalSeek,
        26 or 31 => NoDevice,
        38 or 109 => Pipe,
        39 or 112 => NoSpace,
        50 => Unsupported,
        85 => Busy,
        89 => Again,
        1921 => Links,
        1314 => Permission,
        _ => Io
    };

    internal static Term Tuple(string error) => Term.Tuple(Term.A(PrimFileWindowsNames410.Error), Term.A(error));

    internal static Term Last(int error) => Tuple(Map(error));

    internal static Term Ok(Term value) => Term.Tuple(Term.A(PrimFileWindowsNames410.Ok), value);
}
