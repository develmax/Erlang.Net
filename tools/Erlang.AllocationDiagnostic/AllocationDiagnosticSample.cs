namespace AllocationDiagnostic;

internal sealed record AllocationDiagnosticSample(
    string Kind,
    int Sample,
    long Bytes,
    int StartingThread,
    int EndingThread,
    bool Synchronous,
    string? Result,
    uint? Hash
);
