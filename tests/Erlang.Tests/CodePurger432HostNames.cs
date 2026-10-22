using System.Reflection;
namespace Erlang.Tests;

internal static class CodePurger432HostNames
{
    internal const string Module = "erts_code_purger", Start = "start", Internal = "erts_internal", Native = "purge_module", Subject = "beam_purge_subject432";
    internal const string NativeType = "Erlang.InternalPurgeNative432", Capture = "TryCapture", Spawn = "TrySpawnCodePurger432", Actor = "sourceCodePurger432", Source = "sourceCodePurgerImage432", Session = "sourceCodePurgerSession432", Operation = "sourceCodePurgerOperation432", Retirement = "codePurgerRetirement432";
    internal const string Scheduler = "ActorTaskScheduler", Execution = "ExecutionIdentity", Image = "Image", Stage = "StageLoaderOnLoad", Finish = "FinishLoaderOnLoad";
    internal const string Prepare = "prepare", PrepareOnLoad = "prepare_on_load", Abort = "abort", Complete = "complete", Ok = "ok", False = "false", True = "true", NotSupported = "notsup", BadArgument = "badarg", Rejected = "rejected432", Kill = "kill", Down = "DOWN";
    internal const string AlteredAuthority = "altered_authority432";
    internal const string ErLang = "erlang", Apply = "apply", Decode = "binary_to_term", Encode = "term_to_binary", FunValue = "fun_value", SoftPurge = "soft_purge", SystemInfo = "system_info", SystemFlag = "system_flag", Outstanding = "outstanding_system_requests_limit";
    internal const string TestRole = "HOST/code-purger432-authentic-role-order-and-window";
    internal const string TestFence = "HOST/code-purger432-real-prepare-abort-fun-fence";
    internal const string TestOnLoad = "HOST/code-purger432-rejected-on-load-saved-old";
    internal const string TestDeath = "HOST/code-purger432-owner-death-before-down";
    internal const string TestShutdown = "HOST/code-purger432-dispose-halt-initializer-pending-session";
    internal const string Failure = "Code purger432 actual source/owner/session control failed.";
    internal const int Schedulers = 2;
    internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
