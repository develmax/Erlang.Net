namespace Erlang.NifNativeBackend;

internal static class NifNativeSequenceTextHostNames
{
    internal const string Module = "nif_sequence_text_callback_host";
    internal const string SingleAtom = "a";
    internal const string EmbeddedZeroAtom = "a\0b";
    internal const string Atom = "callback_atom";
    internal const string UnicodeAtom = "é😀";
    internal const string AbsentAtom = "nif_lookup_absent_5dd21f916e8b4b29";
    internal const string BindingFailure = "test binding restoration";
    internal const string Failure = "NIF sequence/text Windows table assertion failed.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    internal const int Tuple = 14;
    internal const int AtomMake = 22;
    internal const int AtomExisting = 23;
    internal const int StringGet = 63;
    internal const int AtomGet = 64;
    internal const int AtomLength = 84;
    internal const int ListLength = 85;
    internal const int AtomMakeLength = 86;
    internal const int AtomExistingLength = 87;
    internal const int StringMakeLength = 88;
    internal const int Latin1 = 1;
    internal const int Utf8 = 2;
    internal const byte Canary = 165;
}
