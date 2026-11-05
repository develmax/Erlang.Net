namespace Erlang.Tests;

internal static class BulkMapPhysicalNames
{
    internal const string Case = "runtime/maps-source-flat-physical-values-through-copy";
    internal const string InputCase = "small_forward";
    internal const string Factory = "FromSourceFlatPairs";
    internal const string Failure = "Source flat map lost its physical value sequence";
    internal const int PhysicalCount = 3;
}
