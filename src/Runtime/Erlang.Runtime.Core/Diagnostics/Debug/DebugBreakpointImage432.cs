namespace Erlang;

internal sealed record DebugBreakpointImage432(WeakReference<ModuleCodeImage> Image, IReadOnlySet<int> Entries);
