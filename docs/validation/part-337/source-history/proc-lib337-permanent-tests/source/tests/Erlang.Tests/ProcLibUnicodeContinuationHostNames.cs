namespace Erlang.Tests;

internal static class ProcLibUnicodeContinuationHostNames
{
    public const int EmptySegments = 100000;
    public const byte InvalidUtf8Byte = 128;
    public const int PrefixCharacter = 97;
    public const string CharactersType = "Erlang.Otp.ProcLibReportCharacters";
    public const string Measure = "Measure";
    public const string Unicode = "unicode";
    public const string MissingRefusal = "Malformed UTF8 unexpectedly measured successfully.";
    public const string WrongRemainder = "Malformed UTF8 remainder or error class differs from the source contract.";
}
