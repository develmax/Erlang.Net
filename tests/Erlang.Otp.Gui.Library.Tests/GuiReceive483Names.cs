namespace Erlang.Otp.Gui.Library.Tests;

internal static class GuiReceive483Names
{
    internal const string Case = "oracle/compiled-module/receive483-binary-and-function-guards-whole-mailbox";
    internal const string AdmissionPrefix = "compiler/receive483/original-image/";
    internal const string Prefix = "Erlang.Otp.Gui.Library.Tests.Receive483.";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Failure = "Original selective receive binary/function guard contract failed";
    internal const string Observer = "otp_receive483_observer";
    internal const string ObserverImage = Observer + ".beam";
    internal const string Golden = "original-values.etf";
    internal const string OutputDirectory = "receive483";
    internal const string Completion = "Original receive483: 54 complete selections and unmatched mailbox queues match OTP";
    internal const string Worker = "worker";
    internal const string ImportsResource = "imports.json";
    internal const string Output = "artifacts/otp-gui-managed/receive483";
    internal const string Managed = "managed-values.etf";
    internal const int Outcomes = 54;
    internal static readonly string[] Modules = ["ct_ssh", "test_server_node", "mod_cgi", "mod_esi", "mnesia_loader", "snmpm_server", "tftp_engine"];
}
