namespace Erlang.Tests;

internal static class StandardBootOriginal412Names
{
    internal const string ChildSwitch = "--standard79-412-child";
    internal const string Prefix = "Erlang.Tests.StandardBootOriginal412.";
    internal const string Manifest = "manifest.json", Modules = "modules.json", Dependencies = "dependencies.json";
    internal const string Observer = "beam_standard_observer411", Subject = "beam_standard_subject411";
    internal const string Erts = "erlang", Init = "init", Tty = "prim_tty", PrimFile = "prim_file";
    internal const string ApplicationController = "application_controller", KernelSupervisor = "kernel_sup";
    internal const string Run = "run", Stacks = "run_stacks", Whereis = "whereis", IsPid = "is_pid";
    internal const string Encode = "term_to_binary", GetStatus = "get_status", ScriptId = "script_id", OnLoad = "on_load";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Proof = "bootstrap-proof.etf";
    internal const string ActualPrefix = "actual-", ExpectedPrefix = "expected-", Json = ".json", Beam = ".beam";
    internal const string BootFile = "no_dot_erlang.boot", StartClean = "start_clean.boot", Ebin = "ebin";
    internal const string InstalledRoot = "InstalledOtpRoot", Bin = "bin", ErtsBin = "erts-17.1/bin";
    internal const string RootOption = "-root", BinOption = "-bindir", BootOption = "-boot", ModeOption = "-mode";
    internal const string Interactive = "interactive", NoShell = "-noshell", NoInput = "-noinput", PathOption = "-pa";
    internal const string BootBase = "no_dot_erlang", Ok = "ok", Receipt = "comparison.json", FailureFile = "child-failure.json";
    internal const string Failure = "Standard boot original 79 observations differ; inspect complete raw/typed receipt";
    internal const string InvalidArguments = "Expected standard child switch, absolute output and admission paths";
    internal const string DiagnosticFailures = "first-chance-failures.json";
    internal const string GuestTracePrefix = "\nGUEST_TRACE=";
    internal const int MaximumDiagnosticFailures = 4096;
    internal const int Rows = 13, WholeRows = 79, ArgumentsExit = 2, FailureExit = 1, SuccessExit = 0;
    internal static readonly TimeSpan BootDeadline = TimeSpan.FromSeconds(60);
    internal static readonly TimeSpan CommandDeadline = TimeSpan.FromSeconds(90);
}
