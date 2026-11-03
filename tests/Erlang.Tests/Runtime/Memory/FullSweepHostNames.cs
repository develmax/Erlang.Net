namespace Erlang.Tests;

internal static class FullSweepHostNames
{
    internal const string Flag = "fullsweep_after";
    internal const string Error = "fullsweep_after did not change actual forced collection behavior";
    internal const string EnterFrame = "Enter";
    internal const string Retain = "Retain";
    internal const string GenerationSnapshot = "GenerationSnapshot508";
    internal const string OldWords = "OldPayloadWords";
    internal const int PromotionCollections = 2;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
