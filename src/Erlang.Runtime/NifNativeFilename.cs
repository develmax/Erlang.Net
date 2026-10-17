using System.Text;

namespace Erlang;

internal static class NifNativeFilename
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static string Resolve(Term value)
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
            catch (DecoderFallbackException failure)
            {
                throw new NotSupportedException(NifNativeDefaultLoaderNames.BinaryFilenameEncoding, failure);
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
