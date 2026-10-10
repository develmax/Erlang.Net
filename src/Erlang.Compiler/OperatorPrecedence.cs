namespace Erlang.Compiler;

internal static class OperatorPrecedence
{
    public const int None = 0;
    public const int MatchAndSend = 1;
    public const int OrElse = 3;
    public const int AndAlso = 4;
    public const int Comparison = 5;
    public const int List = 6;
    public const int Additive = 7;
    public const int Multiplicative = 8;
    public const int Prefix = 9;
}
