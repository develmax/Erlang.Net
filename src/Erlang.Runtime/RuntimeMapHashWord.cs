namespace Erlang;

// An identity word in the managed VM model; never a captured original address.
internal sealed record RuntimeMapHashWord(ulong Value);
