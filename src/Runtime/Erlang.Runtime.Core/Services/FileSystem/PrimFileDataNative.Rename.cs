using System.Runtime.InteropServices;

namespace Erlang;

internal static partial class PrimFileDataNative
{
    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.Rename, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Move(string source, string target, uint flags);

    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.RemoveDirectory, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveEmptyDirectory(string path);

    [DllImport(PrimFileDataNativeNames.Library, EntryPoint = PrimFileDataNativeNames.MakeDirectory, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateDirectory(string path, nint securityAttributes);

    internal static bool MakeDirectory(string path) => CreateDirectory(path, 0);

    internal static bool DeleteDirectory(string path) => RemoveEmptyDirectory(path);

    internal static Term Rename(string source, string target)
    {
        const uint flags = PrimFileDataNativeNames.MoveCopyAllowed | PrimFileDataNativeNames.MoveWriteThrough;
        if (Move(source, target, flags))
        {
            return Term.A(CommonAtomNames.Ok);
        }
        int error = Marshal.GetLastPInvokeError();
        bool sourceDirectory = Directory.Exists(source);
        bool targetDirectory = Directory.Exists(target);
        if (error is PrimFileDataNativeNames.FileExists or PrimFileDataNativeNames.AlreadyExists)
        {
            if (sourceDirectory && !targetDirectory)
            {
                return PrimFileWindowsErrors410.Tuple(PrimFileWindowsErrors410.NotDirectory);
            }
            if (!sourceDirectory && targetDirectory)
            {
                return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.IsDirectory);
            }
            if (sourceDirectory)
            {
                if (!RemoveEmptyDirectory(target))
                {
                    return PrimFileWindowsErrors410.Tuple(PrimFileWindowsErrors410.Exists);
                }
                if (Move(source, target, flags))
                {
                    return Term.A(CommonAtomNames.Ok);
                }

                return PrimFileWindowsErrors410.Last(Marshal.GetLastPInvokeError());
            }
            File.Move(source, target, overwrite: true);

            return Term.A(CommonAtomNames.Ok);
        }
        if (sourceDirectory && error == PrimFileDataNativeNames.AccessDenied
            && target.StartsWith(Path.TrimEndingDirectorySeparator(source) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return PrimFileWindowsErrors410.Tuple(PrimFileWindowsNames410.Invalid);
        }

        return PrimFileWindowsErrors410.Last(error);
    }
}
