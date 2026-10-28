namespace Erlang;

public sealed partial class BitString
{
    // Root inspection must not pin/revoke a live append writer or copy bytes.
    internal BinaryPayloadHandle512 CaptureBinaryPayload512() => new(backing);
}
