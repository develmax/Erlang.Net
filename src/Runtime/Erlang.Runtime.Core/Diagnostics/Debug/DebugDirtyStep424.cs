namespace Erlang;

// A private continuation of the real native test operation, never a guest
// scheduler label. Each transition is admitted to the actual target queue.
internal sealed class DebugDirtyStep424(DirtyNativeKind419 kind, Term operation, Term argument)
{
    internal DirtyNativeKind419 Kind { get; } = kind;
    internal Term Operation { get; } = operation;
    internal Term Argument { get; } = argument;
    internal DebugDirtyStep424? Next { get; set; }
}
