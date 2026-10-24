namespace Erlang.Tests;

internal static class EtsRetirement407Names
{
    internal const string OwnerTest = "HOST/ets-retirement407/owner-down-after-owned-drain";
    internal const string LeaseTest = "HOST/ets-retirement407/sparse-rows-and-held-reader";
    internal const string ShutdownTest = "HOST/ets-retirement407/dispose-halt-initializer-drain";
    internal const string DeleteTest = "HOST/ets-retirement407/async-public-delete-name-generation";
    internal const string DeleteAsync = "DestroyTableAsync";
    internal const string Drain = "DrainAsync";
    internal const string Yields = "RetirementYields";
    internal const string Down = "DOWN";
    internal const string Remove = "RemoveTuple";
    internal const string ProcessMap = "processes";
    internal const string Gate = "gate";
    internal const string Kill = "kill";
    internal const int Rows = 8192;
    internal const int MinimumYields = 128;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
