namespace Erlang;

internal sealed class EtsIndexChange420(EtsPrimaryIndex420 index, RuntimeEtsStoredObject[] retired)
{
    internal EtsPrimaryIndex420 Index { get; } = index;
    internal RuntimeEtsStoredObject[] Retired { get; } = retired;
}
