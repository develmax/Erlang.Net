namespace Erlang.NifNativeBackend;

internal static class NifLinuxManagedControlNames
{
    internal const string Failure = "Managed-only Linux map control failed.";
    internal const string Module = "map_size_callback_contract";
    internal const string Function = "make_map";
    internal const string FirstNode = "managed_first@fixture";
    internal const string SecondNode = "managed_second@fixture";
    internal const string First = "first";
    internal const string Second = "second";
    internal const string Journal = "journal.jsonl";
    internal const string Completion = "completion.json";
    internal const string Before = "make-map-before";
    internal const string After = "make-map-after";
    internal const string Failed = "managed-control-failed";
    internal const string Disposed = "runtime-disposed";
    internal const string Acquired = "ACQUIRED_MANAGED_ONLY_TWO_RUNTIME_MAP_CONTROL";
    internal const string LocalIdentity = "LocalIdentity";
    internal const string Publish = "Publish";
    internal const int Domains = 2;
    internal static readonly int[] Counts = [0, 1, 31, 32, 33, 512, 8193, 8193];
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(120);
}
