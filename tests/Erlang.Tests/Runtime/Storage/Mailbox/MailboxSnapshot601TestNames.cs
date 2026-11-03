using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxSnapshot601TestNames
{
    internal const string SnapshotCase = "runtime/mailbox601/public-snapshot-retains-origin-across-dequeue";
    internal const string OverrideCase = "runtime/mailbox601/public-reregister-process-info-stays-opaque";
    internal const string Functions = "functions";
    internal const string SourceScalarObservation = "snapshot601 actual source scalar returned: {0}={1}";
    internal const string SourceCollectedObservation = "snapshot601 actual post-return major collection: {0}";
    internal const string DebugProviderObservation = "snapshot601 actual flat_size binding before={0}, after={1}";
    internal const string SelfCase = "runtime/mailbox601/source-self-process-info-and-trusted-receive";
    internal const string SelfEntry = "self_run";
    internal const string SourceCase = "runtime/mailbox601/source-remote-process-info-roots-native-messages";
    internal const string CancellationCase = "runtime/mailbox601/cancel-before-target-capture-releases-request";
    internal const string StopSource = "Stop";
    internal const string MutualCase = "runtime/mailbox601/mutual-process-info-control-signal-liveness";
    internal const string OrderingCase = "runtime/mailbox601/remote-process-info-original-reply-order";
    internal const string RuntimeGate = "gate";
    internal const string MailboxGate = "gate";
    internal const string PendingSignal = "PendingSignalAdded428";
    internal const string AdmitSignals = "AdmitSignals";
    internal const string Query = "QueryProcessInfo";
    internal const string ItemType = "Erlang.ProcessInfoItem";
    internal const string Parse = "Parse";
    internal const string Values = "Values";
    internal const string AfterReply = "snapshot601_after_reply";
    internal const string DefaultCase = "runtime/mailbox601/default-process-info-private-reply-no-owned-heap";
    internal const string HostScopeRequired = "Native resource host escape requires an explicit native host scope.";
    internal const string Failure = "A mailbox snapshot lost its actual native ownership boundary.";
    internal const string CleanupFailure = "Mailbox snapshot assertion and independent cleanup failed.";
    internal const string Heap = "TermHeap506";
    internal const string HeapField = "termHeap506";
    internal const string MailboxProperty = "Mailbox";
    internal const string Make = "MakeNativeResource524";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Major = "CollectGeneration508";
    internal const string PointerProperty = "Pointer524";
    internal const string Retired = "Retired524";
    internal const string CopyType = "Erlang.OwnedTermDetachedCopy506";
    internal const string Copy = "CopyAtomics586";
    internal const string PublicationType = "Erlang.MailboxPublicationScope593";
    internal const string Capture = "Capture";
    internal const string Send = "SendTracedSnapshot593";
    internal const string CaptureSnapshot = "CaptureSnapshot601";
    internal const string CompleteSnapshot = "CompletePublicSnapshot601";
    internal const string Roots = "Erlang.NativeResourceRoots524";
    internal const string Direct = "Direct524";
    internal const string Stop = "snapshot601_stop";
    internal const string Messages = "messages";
    internal const string GuestModule = "snapshot_source601";
    internal const string Entry = "run";
    internal const string Probe = "snapshot_probe601";
    internal const string After = "after_snapshot";
    internal const int Sentinel = 601;
    internal const int DefaultMessage = 61;
    internal const int NativeHeaderWords = 3;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal const string Source = """
        -module(snapshot_source601).
        -export([run/1,self_run/0]).
        run(Target) ->
            {messages,[R]} = erlang:process_info(Target,messages),
            snapshot_probe601:after_snapshot(Target),
            erlang:garbage_collect(),
            erts_debug:flat_size(R).
        self_run() ->
            {messages,[R]} = erlang:process_info(self(),messages),
            receive R -> ok end,
            erlang:garbage_collect(),
            erts_debug:flat_size(R).
        """;
}
