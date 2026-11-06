namespace Erlang;

internal static class ApplicationApi
{
    internal const string EnsureAllStarted = "ensure_all_started";
    internal const string Start = "start";
    internal const string StartBoot = "start_boot";
    internal const string Stop = "stop";
    internal const string Load = "load";
    internal const string Unload = "unload";
    internal const string Takeover = "takeover";
    internal const string WhichApplications = "which_applications";
    internal const string LoadedApplications = "loaded_applications";
    internal const string Permit = "permit";
    internal const string EnsureStarted = "ensure_started";
    internal const string SetEnv = "set_env";
    internal const string UnsetEnv = "unset_env";
    internal const string GetEnv = "get_env";
    internal const string GetAllEnv = "get_all_env";
    internal const string GetKey = "get_key";
    internal const string GetAllKey = "get_all_key";
    internal const string GetApplication = "get_application";
    internal const string GetSupervisor = "get_supervisor";
    internal const string Info = "info";
    internal const string StartType = "start_type";
    internal const string BehaviourInfo = "behaviour_info";
}
