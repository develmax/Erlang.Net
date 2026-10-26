namespace Erlang;

internal sealed record ProcessTraceSettings(ProcessTraceFlags Flags, Pid Tracer);
