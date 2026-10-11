namespace Erlang;

internal static class ProcessAliasLayout
{
    public const uint PidMarker = 1u << 16;
    public const int PidTagBits = 4;
    public const uint PidTag = 3;
    public const int OwnerLowWord = 3;
    public const int OwnerHighWord = 4;
}
