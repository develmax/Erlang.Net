namespace Erlang;

public sealed record DistributionDecodedProcessPacket(TupleTerm Control, Term? Payload);
