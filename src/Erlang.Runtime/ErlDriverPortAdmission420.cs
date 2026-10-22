namespace Erlang;

// Status1 owns a live lease;0 is closing; -1 is absent/invalid.
internal sealed record ErlDriverPortAdmission420(ErlDriverPortReadLease420? Lease, int Status);
