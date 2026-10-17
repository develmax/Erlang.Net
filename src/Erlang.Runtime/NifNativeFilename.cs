using System.Text;

namespace Erlang;

internal static class NifNativeFilename
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static string Resolve(Term value) =>
        ResolveNative(value).AsAbsolute(NifNativeUnixFile.WorkingDirectory).DisplayPath;

    internal static NifNativeLibraryPath ResolveNative(Term value, bool reviewed = false)
    {
        if (OperatingSystem.IsWindows())
            return NifNativeLibraryPath.FromHostPath(ResolveWindows(value));
        if (!SourceLoadNifFilename.IsValidUtf8(value))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        NifNativePlatformLoader.VerifyPlatform();
        byte[] stem = value is BitString binary ? binary.ToArray() : Encoding.UTF8.GetBytes(Flatten(value));

        NifNativeLibraryPath path = NifNativeLibraryPath.ResolveUnix(stem);

        return reviewed && !path.IsUnixBasename ? path.AsAbsolute(NifNativeUnixFile.WorkingDirectory) : path;
    }

    private static string Flatten(Term value)
    {
        var text = new StringBuilder();
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out Term? next))
        {
            if (next is Cons cell)
            {
                pending.Push(cell.Tail);
                pending.Push(cell.Head);
            }
            else if (next is Integer number)
                text.Append(char.ConvertFromUtf32((int)number.Value));
        }

        return text.ToString();
    }

    private static string ResolveWindows(Term value)
    {
        if (!SourceLoadNifFilename.IsValidUtf8(value))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        string name;
        if (value is BitString binary)
        {
            byte[] bytes = binary.ToArray();
            // The native filename conversion copies binary bytes; ddll strlen uses the first NUL.
            int terminator = Array.IndexOf(bytes, (byte)0);
            int length = terminator < 0 ? bytes.Length : terminator;
            try
            {
                name = StrictUtf8.GetString(bytes, 0, length);
            }
            catch (DecoderFallbackException) when (OperatingSystem.IsWindows())
            {
                // Pinned erts_convert_filename_to_wchar falls back byte-for-byte to Latin1.
                name = Encoding.Latin1.GetString(bytes, 0, length);
            }
        }
        else
        {
            var text = new StringBuilder();
            var pending = new Stack<Term>();
            pending.Push(value);
            while (pending.TryPop(out Term? next))
            {
                if (next is Cons cell)
                {
                    pending.Push(cell.Tail);
                    pending.Push(cell.Head);
                }
                else if (next is Integer number)
                {
                    text.Append(char.ConvertFromUtf32((int)number.Value));
                }
            }
            name = text.ToString();
        }
        NifNativePlatformLoader.VerifyPlatform();
        string suffix = OperatingSystem.IsWindows()
            ? NifNativeDefaultLoaderNames.WindowsSuffix : NifNativeDefaultLoaderNames.UnixSuffix;

        // OTP's platform ddll open appends its suffix even when the input already has one.
        string suffixed = name + suffix;
        if (Path.IsPathRooted(suffixed) && !Path.IsPathFullyQualified(suffixed))
            throw new NotSupportedException(NifNativeDefaultLoaderNames.DriveRelativeFilename);

        // Do not collapse '..': on Unix a preceding symlink changes the actual opened target.
        return Path.IsPathFullyQualified(suffixed) ? suffixed : Path.Combine(Environment.CurrentDirectory, suffixed);
    }
}
