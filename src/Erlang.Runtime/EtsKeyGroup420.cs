namespace Erlang;
// A live key has actual owned slots. A deleted fixed key retains one actual payload.
internal sealed class EtsKeyGroup420(ulong hash, RuntimeEtsObjectSlot[] rows, RuntimeEtsObjectSlot? tombstone)
{
    internal RuntimeEtsStoredObject KeyPayload => Rows.Length != 0 ? Rows[0].Payload : Tombstone!.Payload;
    internal ulong Hash { get; } = hash;
    internal RuntimeEtsObjectSlot[] Rows { get; } = rows;
    internal RuntimeEtsObjectSlot? Tombstone { get; } = tombstone;
    internal bool Deleted => Rows.Length == 0;
}
