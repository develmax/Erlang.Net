namespace Erlang;

internal static class ReferenceNumberLayout
{
    public const uint HostDomain = 0;
    public const uint DomainMask = (1u << 16) - 1;
    public const uint MagicMarker = 1u << 17;
    public const uint CountMiddleMask = ~(DomainMask | ProcessAliasLayout.PidMarker | MagicMarker);
    public const int CountHighShift = 32;
    public const uint PidTagMask = ReferenceIdentityLayout.PidTagMask;
    public const int PidWords = ReferenceIdentityLayout.PidWordCount;
}
