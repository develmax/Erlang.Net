namespace Erlang;

public static class DistributionAlternativePidSendLayout
{
    public const int Arity = 4;
    public const int TraceArity = 5;
    public const int FlagsIndex = 1;
    public const int SenderIndex = 2;
    public const int TargetIndex = 3;
    public const int TokenIndex = 4;
    public const int Priority = 1;
    public const int Token = 2;
    public const int SupportedFlags = Priority | Token;
    public const int TraceTokenArity = 5;
}
