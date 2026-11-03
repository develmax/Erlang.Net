namespace Erlang.NifNativeBackend;

public static class NativeRecordHeapCaseNames
{
    public const string Physical = "runtime/statistics-native-record-real-instance-definition-order-pointers-and-reclaim";
    public const string Transfer = "runtime/statistics-native-record-copied-actual-children-detached-host-escape";
    public const string Source = "runtime/statistics-native-record-source-birth-update-getters-live-gc-and-return";
    public const string Close = "runtime/statistics-native-record-genuine-source-wait-vm-close-and-stale-read";
    internal const string Ok = "ok";
    internal const string Module = "native_record_physical";
    internal const string Record = "r";
    internal const string FieldA = "native_record_physical_a";
    internal const string FieldZ = "native_record_physical_z";
    internal const string Probe = "native_record_physical_probe";
    internal const string Check = "check";
    internal const string Run = "run";
    internal const string Wait = "wait";
    internal const string Storage = "OwnedRecordStorage";
    internal const string HeapStorage = "Storage";
    internal const string Definitions = "recordDefinitions";
    internal const string Blocks = "blocks";
    internal const string File = "native_record_physical.erl";
    internal const string Failure = "Actual native record instance/definition/pointer/copied-child/GC/retirement contract failed";
    // Actual local dynamic layout: order3 + definition8 + instance4 + big5 + HeapBits3.
    internal const int LocalTwoFieldWords = 23;
    internal const nuint TwoFieldHeader = 0xDC;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    internal const string Program = """
-module(native_record_physical).
-export([run/1,wait/1]).
-record #r{z,a}.
-export_record([r]).
run(N) ->
    R = #r{z=N,a = <<1,2,3>>},
    true = erlang:garbage_collect(),
    native_record_physical_probe:check(1),
    Z = R#r.z,
    R2 = R#r{z=N+1},
    true = erlang:garbage_collect(),
    native_record_physical_probe:check(2),
    {R,R2,Z}.
wait(N) ->
    R = #r{z=N,a = <<1,2,3>>},
    native_record_physical_probe:check(3),
    receive stop -> R end.
""";
}
