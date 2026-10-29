namespace Erlang.Tests;

internal static class MonitorShutdown652Names
{
    internal const string Case = "runtime/monitor652/earlier-target-resource-down-vm-shutdown";
    internal const string Module = "monitor_shutdown652";
    internal const string SourceFile = "monitor_shutdown652.erl";
    internal const string Arm = "arm";
    internal const string Stored = "stored";
    internal const string ProcessTable = "processes";
    internal const string MonitorTable = "monitors";
    internal const string RuntimeGate = "gate";
    internal const string Observer = "Observer";
    internal const string Target = "Target";
    internal const string Custody = "Custody641";
    internal const string Graph = "Graph641";
    internal const string MailboxOwner = "MailboxRoots593";
    internal const string GroupType = "Erlang.RuntimeProcessPayloadGroup641";
    internal const string CaptureGroup = "Capture641";
    internal const string PrepareDerived = "PrepareProcessPayloadInterestGraph641";
    internal const string PrepareFresh = "PrepareGraph";
    internal const string PrepareGeneric = "PrepareInterestGraph593";
    internal const string RetainGraph = "Retain";
    internal const string DrainRoots = "DrainAsync";
    internal const string Failure = "Earlier-target resource monitor did not settle VM shutdown.";
    internal const string Source = """
        -module(monitor_shutdown652).
        -export([arm/1]).
        arm(Target) ->
          Tag={payload,[#{resource=>get(stored)}]},
          Ref=erlang:monitor(process,Target,[{tag,Tag}]),
          erase(stored),erlang:garbage_collect(),Ref.
        """;
}
