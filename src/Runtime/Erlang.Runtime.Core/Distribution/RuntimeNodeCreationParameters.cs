namespace Erlang;

internal static class RuntimeNodeCreationParameters
{
    internal const long MaximumSmallCreation = (1L << 59) - 1;
    internal const uint WireCreationMask = uint.MaxValue;
}
