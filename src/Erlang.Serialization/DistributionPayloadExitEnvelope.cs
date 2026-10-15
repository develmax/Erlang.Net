namespace Erlang;

internal sealed record DistributionPayloadExitEnvelope(TupleTerm Control, ReadOnlyMemory<byte> Reason);
