namespace Erlang.Tests;

internal static class DirectInterpreterEntryNames
{
    internal const string EvaluateCase = "compiler/direct-evaluate-ordinary-roots-custom-pattern-host-return";
    internal const string InvokeCase = "compiler/direct-invoke-unregistered-definition-roots-host-function";
    internal const string BeamCase = "compiler/direct-raw-beam-ordinary-roots-before-gc-host-return";
    internal const string ErrorCase = "compiler/direct-public-error-export-and-beam-validation-precedence";
    internal const string CancelCase = "compiler/direct-evaluate-cancel-real-frame-retirement";
    internal const string OffActorCase = "compiler/direct-evaluate-off-actor-host-behavior-preserved";
    internal const string Guest = "direct_interpreter_guest";
    internal const string Run = "run";
    internal const string Fail = "fail";
    internal const string Bound = "bound_value";
    internal const string Probe = "direct_interpreter_probe";
    internal const string Backing = "backing";
    internal const string MakeReference = "make_ref";
    internal const string Marker = "direct_entry_value";
    internal const string Error = "direct_entry_error";
    internal const string HostMetadata = "direct_entry_host_metadata";
    internal const string Missing = "direct_entry_missing";
    internal const string MissingDiagnostic = "BEAM export was not found";
    internal const string ProfileFailure = "Direct entry private profile=";
    internal const string Failure = "Direct public interpreter lost real guest roots or opaque Host boundary";
    internal const string BeamFile = "beam_fixture_constant.beam";
    internal const string BeamExpected = "beam_fixture_constant.expected.etf";
    internal const string ResourceSeparator = ".";
    internal const string Heap = "termHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Blocks = "Blocks";
    internal const string Frames = "RootFrames";
    internal const string Storage = "OwnedStorage506";
    internal const string CheckpointType = "Erlang.BeamOwnedCheckpoint507";
    internal const string Enter = "Enter";
    internal const string Unwound = "Unwound";
    internal const string Task = "Task";
    internal const string Retirement = "OwnedTermRetirement506";
    internal const string Kill = "kill";
    internal const int Operand = 41;
    internal const int Index = 7;
    internal const int UniqueBytes = 16;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(direct_interpreter_guest).
        -export([run/1, fail/0]).
        run(F) -> V={make_ref(),[1,2,3]}, true=direct_interpreter_probe:backing(), F(V).
        fail() -> erlang:error({direct_entry_error,make_ref()}).
        """;
}
