namespace Erlang;

public sealed record RuntimeHaltOptions(bool Flush = true, uint? FlushTimeoutMilliseconds = null);
