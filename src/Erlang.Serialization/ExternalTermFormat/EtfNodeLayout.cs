namespace Erlang;

internal static class EtfNodeLayout
{
    public const uint MaximumTinyCreation = 3;
    public const uint MaximumLocalPidNumber = (1u << 28) - 1;
    public const ulong MaximumLocalPortNumber = (1UL << 60) - 1;
}
