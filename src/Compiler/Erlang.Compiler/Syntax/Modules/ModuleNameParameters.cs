namespace Erlang.Compiler;

internal static class ModuleNameParameters
{
    public const int MaximumLatin1CodePoint = 255;
    public const int Space = 32;
    public const int NonBreakingSpace = 160;
    public const int SoftHyphen = 173;
    public const int FirstControlLimit = 32;
    public const int SecondControlStart = 127;
    public const int SecondControlLimit = 160;
}
