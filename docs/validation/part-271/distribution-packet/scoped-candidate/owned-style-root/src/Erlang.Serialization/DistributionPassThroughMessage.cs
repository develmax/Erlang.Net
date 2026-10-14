namespace Erlang;

public sealed record DistributionPassThroughMessage(TupleTerm Control, Term Payload);
