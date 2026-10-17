namespace Erlang.NifNativeBackend;

internal static class NifNativeLegacyStringHostNames
{
    internal const string Module = "nif_legacy_string_callback_host";
    internal const string LegacyGreeting = "native hello";
    internal const string Unicode = "é😀";
    internal const string Failure = "NIF legacy string Windows table assertion failed.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    internal static readonly int[] Lengths = [4095, 4096, 4097, 8193, 65537];
    internal const int MakeString = 27;
    internal const int Latin1 = 1;
    internal const int Utf8 = 2;
    internal const int PendingException = 123;
    internal const int IsException = 102;
}
