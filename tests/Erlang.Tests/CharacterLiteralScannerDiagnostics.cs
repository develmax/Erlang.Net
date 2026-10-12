namespace Erlang.Tests;

public static class CharacterLiteralScannerDiagnostics
{
    public const string SourceOffsets = "Character tokens must preserve UTF16 source offsets";
    public const string FollowingToken = "Character escapes consumed the following integer token";
    public const string MalformedLiteral = "Malformed character literal was accepted or reported at a wrong offset";
    public const string DollarOffset = "Invalid character diagnostic must identify the dollar marker";
}
