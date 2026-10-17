using System.Text;

namespace Erlang;

internal static class NifNativeTextNames
{
    internal const int Latin1 = 1;
    internal const int Utf8 = 2;
    internal const int MaximumAtomCharacters = 255;
    internal const int MaximumAtomUtf8Bytes = 4 * MaximumAtomCharacters;
    internal const int MaximumUtf8ScalarBytes = 4;
    internal const int MaximumUnicodeScalar = 0x10ffff;
    internal const string NullOutput = "A successful NIF text or sequence query requires output pointers.";
    internal const string NullInput = "A nonempty NIF text input requires a byte pointer.";
    internal static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
}
