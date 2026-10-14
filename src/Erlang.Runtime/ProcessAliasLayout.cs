namespace Erlang;

internal static class ProcessAliasLayout
{
    public const uint PidMarker = ReferenceIdentityLayout.PidMarker;
    public const int PidTagBits = ReferenceIdentityLayout.PidTagBits;
    public const uint PidTag = ReferenceIdentityLayout.PidTag;
    public const int OwnerLowWord = ReferenceIdentityLayout.PidOwnerLowWord;
    public const int OwnerHighWord = ReferenceIdentityLayout.PidOwnerHighWord;
}
