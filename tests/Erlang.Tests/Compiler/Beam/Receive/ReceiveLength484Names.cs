namespace Erlang.Tests;

internal static class ReceiveLength484Names
{
    internal const string Case = "runtime/beam/receive484/length-plus-whole-mailbox";
    internal const string AdmissionCase = "compiler/receive484/original-megaco-image";
    internal const string HostPrefix = "runtime/beam/receive484/cooperative-";
    internal const string Prefix = "Erlang.Tests.ReceiveLength484.";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Failure = "Original cooperative receive length contract failed";
    internal const string Observer = "otp_receive_length484_observer";
    internal const string ObserverImage = Observer + ".beam";
    internal const string Golden = "original-values.etf";
    internal const string Worker = "worker";
    internal const string ImportsResource = "imports.json";
    internal const string Output = "artifacts/library484/permanent";
    internal const string Managed = "managed-values.etf";
    internal const string Megaco = "megaco_trans_sender";
    internal const string HostImage = "length_host_guard.beam";
    internal const string PriorityGolden = "priority-values.etf";
    internal const string PriorityManaged = "priority-managed-values.etf";
    internal const string StatefulImport = "get";
    internal const int Outcomes = 576;
}
