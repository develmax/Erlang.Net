namespace Erlang;

internal sealed class PersistentTermEntry(RuntimeEtsStoredObject payload, ulong hash)
{
    internal RuntimeEtsStoredObject Payload { get; } = payload;
    internal ulong Hash { get; } = hash;
}
