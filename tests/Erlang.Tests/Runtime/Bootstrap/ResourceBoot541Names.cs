namespace Erlang.Tests;

internal static class ResourceBoot541Names
{
    internal const string StockPlanSha256 = "D80D075FDBCE8DE0081E6EFAF4A6CA6FB7158EAEA38AD5A33D639787172A3690";
    internal const string Eager = "preparing-original-178-images";
    internal const string EagerReady = "ordinary-original-images-published";
    internal const string Preparation = "eager-images541.json";
    internal const string PrepareFailure = "Original eager image preparation failed: ";
    internal const int StockImageCount = 178;
    internal const string FirstChance = "first-chance541.json";
    internal const string Progress = "progress541.json";
    internal const string Diagnostic = "diagnostic541.json";
    internal const string RuntimeCreated = "runtime-created";
    internal const string PrimFile = "starting-prim-file-on-load";
    internal const string PrimFileReady = "prim-file-on-load-returned";
    internal const string Boot = "starting-original-boot";
    internal const string BootReady = "original-boot-returned";
    internal const string Observer = "starting-original-resource-observer";
    internal const string Completed = "resource-driver-returned";
    internal const int MaximumExceptions = 4096;
    internal const int MaximumSamples = 64;
    internal static readonly TimeSpan SamplePeriod = TimeSpan.FromSeconds(10);
}
