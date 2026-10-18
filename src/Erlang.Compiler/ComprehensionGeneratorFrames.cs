namespace Erlang.Compiler;

internal sealed record ComprehensionGeneratorFrames(
    IReadOnlyList<ComprehensionLogicalFrame> Frames,
    long EmittedCount
);
