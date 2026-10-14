namespace Erlang;

public static class ReferenceIdentityLayout
{
    public const int WordBits = 32;
    public const int MinimumWords = 1;
    public const int MaximumWords = 5;
    public const int LocalWords = 3;
    public const int LocalFirstWordBits = 18;
    public const int PidMarkerWord = 1;
    public const int PidMarkerBitOffset = 2;
    public const uint PidMarker = 1u << (LocalFirstWordBits - PidMarkerBitOffset);
    public const int PidWordCount = 5;
    public const int PidTagBits = 4;
    public const uint PidTagMask = (1u << PidTagBits) - 1;
    public const uint PidTag = 3;
    public const int PidOwnerLowWord = 3;
    public const int PidOwnerHighWord = 4;
    public const uint LocalFirstWordMask = (1u << LocalFirstWordBits) - 1;
}
