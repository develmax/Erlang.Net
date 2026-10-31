namespace Erlang.NifNativeBackend;

public static class NativeWritableCaseNames
{
    public const string Bits = "runtime/statistics-native-writable-partial-byte-actual-flags-and-payload";
    public const string Close = "runtime/statistics-native-writable-genuine-source-wait-vm-close-real-retirement";
    internal const string Module = "native_writable_physical";
    internal const string File = "native_writable_physical.erl";
    internal const string Run = "run";
    internal const string BitRun = "bits";
    internal const string Wait = "wait";
    internal const string Probe = "native_writable_physical_probe";
    internal const string Checkpoint = "checkpoint";
    internal const string Heap = "termHeap506";
    internal const string Descriptors = "resourceBits577";
    internal const string Nodes = "magicReferences576";
    internal const string Addresses = "magicAddresses576";
    internal const string RegularHead = "magicOffHeap576";
    internal const string WritableHead = "writableOffHeap";
    internal const string ProtocolModule = "native_writable_protocol";
    internal const string Ok = "ok";
    internal const string Failure = "Actual native writable backing/append/volatile view/pin/GC/lifetime contract failed.";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    internal const string Source = """
-module(native_writable_physical).
-export([run/0,bits/0,wait/0,append/2,grow/1]).
run() ->
    A = append(<<>>,7),
    native_writable_physical_probe:checkpoint(1),
    B = append(A,8),
    native_writable_physical_probe:checkpoint(2),
    C = append(A,99),
    native_writable_physical_probe:checkpoint(3),
    D = grow(B),
    native_writable_physical_probe:checkpoint(4),
    native_writable_physical_probe:checkpoint(5),
    native_writable_physical_probe:checkpoint(6),
    {A,B,C,D}.
bits() ->
    A = append(<<>>,7),
    B = <<A/binary,1:1>>,
    native_writable_physical_probe:checkpoint(8),
    {A,B}.
wait() ->
    A = append(<<>>,7),
    native_writable_physical_probe:checkpoint(7),
    receive stop -> byte_size(A) end.
append(B,N) -> <<B/binary,N>>.
grow(B) -> <<B/binary,0:2048>>.
""";
}
