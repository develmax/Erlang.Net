namespace Erlang.Tests;

internal static class ReceiveOriginal483Names
{
    internal const string Case = "oracle/compiled-module/receive483-binary-and-function-guards-whole-mailbox";
    internal const string AdmissionPrefix = "compiler/receive483/original-image/";
    internal const string Prefix = "Erlang.Tests.ReceiveOriginal483.";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Failure = "Original selective receive binary/function guard contract failed";
    internal const string Observer = "otp_receive483_observer";
    internal const string ObserverImage = Observer + ".beam";
    internal const string Golden = "original-values.etf";
    internal const string Worker = "worker";
    internal const string ImportsResource = "imports.json";
    internal const string Output = "artifacts/library483/permanent";
    internal const string Managed = "managed-values.etf";
    internal const int Outcomes = 54;
    internal static readonly string[] Modules = ["ct_ssh", "test_server_node", "mod_cgi", "mod_esi", "mnesia_loader", "snmpm_server", "tftp_engine"];
}
