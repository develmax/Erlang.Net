namespace Erlang;

public static class ReferenceIdentityLayout
{
    public const int WordBits = 32;
    public const int MinimumWords = 1;
    public const int MaximumWords = 5;
    public const int LocalWords = 3;
    public const int LocalFirstWordBits = 18;
    public const uint LocalFirstWordMask = (1u << LocalFirstWordBits) - 1;
}
