namespace HashAllocationDiagnostic;

internal sealed record AllocationSample(
    string Path,
    int Ordinal,
    long AllocatedBytes,
    int StartingThread,
    int EndingThread,
    bool Synchronous,
    string Result,
    long ElapsedTicks
);
