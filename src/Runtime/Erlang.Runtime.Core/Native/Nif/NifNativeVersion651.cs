namespace Erlang;

internal static class NifNativeVersion651
{
    internal const int MinimumMajor = 2;
    internal const int RejectedMapsMinor = 5;
    internal const int VariantMinor = 1;
    internal const int FlagsMinor = 7;
    internal const int ResourceInitMinor = 12;
    internal const int MinimumErtsMinor = 14;
    internal const int PrefixBytes = 64;
    internal const int LegacyFunctionBytes = 24;
    internal const int VariantOffset = 64;
    internal const int OptionsOffset = 72;
    internal const int ResourceInitOffset = 80;
    internal const int MinimumErtsOffset = 88;
    internal const string FuturePrefix = "That '";
    internal const string FutureMiddle = "' NIF library needs ";
    internal const string FutureSuffix = " or newer. Either try to recompile the NIF lib or use a newer erts runtime.";
    internal const string OldPrefix = "That old NIF library (";
    internal const string OldMiddle = ") is not compatible with this erts runtime (";
    internal const string OldSuffix = "). Try recompile the NIF lib.";
    internal const string VersionSeparator = ".";
    internal const string VariantPrefix = "Library (";
    internal const string VariantMiddle = ") not compiled for this vm variant (";
    internal const string VariantSuffix = ").";
}
