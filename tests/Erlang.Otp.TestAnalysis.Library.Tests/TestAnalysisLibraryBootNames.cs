namespace Erlang.Otp.TestAnalysis.Library.Tests;

internal static class TestAnalysisLibraryBootNames
{
    internal const string Features = "erl_features";
    internal const string PrimitiveFile = "prim_file";
    internal const string OnLoad = "on_load";
    internal const string Init = "init";
    internal const string FileServer = "file_server_2";
    internal const string Root = "-root";
    internal const string Bindir = "-bindir";
    internal const string Boot = "-boot";
    internal const string Mode = "-mode";
    internal const string Embedded = "embedded";
    internal const string DirectoryPrefix = "otp-testanalysis475-";
    internal const string GuidFormat = "N";
    internal const string BootBase = "testanalysis";
    internal const string BootExtension = ".boot";
    internal const string Script = "script";
    internal const string ScriptVersion = "1";
    internal const string Preloaded = "preLoaded";
    internal const string ScriptPath = "path";
    internal const string PrimitiveLoad = "primLoad";
    internal const string LoadCompleted = "kernel_load_completed";
    internal const string KernelProcess = "kernelProcess";
    internal const string FileServerModule = "file_server";
    internal const string StartLink = "start_link";
    internal const string Progress = "progress";
    internal const string Started = "started";
    internal const string Ok = "ok";
    internal static readonly string[] StdlibModules = [Features, "gb_sets", "gb_trees", "gen", "gen_server", "proc_lib", "sys"];
    internal static readonly string[] KernelModules = ["file", FileServerModule, "code_server", "code", "os", "logger"];
    internal static readonly string[] ErtsModules = [Init, "erl_prim_loader", PrimitiveFile];
}
