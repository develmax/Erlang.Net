namespace Erlang;

internal static class EtfFunctionLayout
{
    public const int SizeBytes = 4;
    public const int UniqueBytes = 16;
    public const int MaximumEnvironment = 255;
    public const int MaximumArity = 255;
    public const int SmallPayloadBits = 59;
    public const long MinimumSmall = -(1L << SmallPayloadBits);
    public const long MaximumSmall = (1L << SmallPayloadBits) - 1;
}
