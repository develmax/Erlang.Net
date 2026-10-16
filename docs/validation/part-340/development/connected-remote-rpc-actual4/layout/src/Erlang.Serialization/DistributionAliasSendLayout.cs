namespace Erlang;

public static class DistributionAliasSendLayout
{
    public const int Arity = 3;
    public const int TraceArity = 4;
    public const int SenderIndex = 1;
    public const int TargetIndex = 2;
    public const int TokenIndex = 3;
    public const int AlternativeAlias = 1 << 2;
    public const int AlternativeSupportedFlags = DistributionAlternativePidSendLayout.SupportedFlags | AlternativeAlias;
}
