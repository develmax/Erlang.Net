namespace Erlang.Tests;

internal sealed record ManagedBatchJob513(
    string Job,
    string Module,
    bool Paired335,
    ManagedBatchImage513[] FixtureImages,
    string[] LibraryModules,
    ManagedBatchRequirement513[] RuntimeRequirements,
    bool RequiresOriginalKernelBoot,
    ManagedBatchImage513? WorkerImage,
    string OriginalWorkerBaseline,
    string[]? SourceSubjects = null,
    string[]? LibraryOnLoadOrder = null
);
