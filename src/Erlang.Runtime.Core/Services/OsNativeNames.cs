namespace Erlang;

internal static class OsNativeNames
{
    internal const string Module = "os";
    internal const string Env = "env";
    internal const string GetEnv = "getenv";
    internal const string PutEnv = "putenv";
    internal const string UnsetEnv = "unsetenv";
    internal const string GetPid = "getpid";
    internal const string PerfCounter = "perf_counter";
    internal const string SystemTime = "system_time";
    internal const string Timestamp = "timestamp";
    internal const string SetSignal = "set_signal";
    internal const string Ignore = "ignore";
    internal const string Default = "default";
    internal const string Handle = "handle";
    internal const string Cause = "cause";
    internal const string BadOption = "badopt";
    internal const string Formatter = "erl_kernel_errors";
    internal const string ForeignRuntime = "OS primitive belongs to a different runtime";
    internal const string WindowsLibrary = "kernel32.dll";
    internal const string CrtLibrary = "ucrtbase.dll";
    internal const string InvariantLocale = "C";
    internal const string UnadmittedSignature = "Unadmitted OS native signature";
    internal const string UnsupportedPlatform = "The complete OS native capability is not installed on this platform";
    internal const string LocaleCreationFailed = "Cannot create the immutable Windows C locale";
    internal const string CreateLocaleEntry = "_create_locale";
    internal const string FreeLocaleEntry = "_free_locale";
    internal const string CompareLocaleEntry = "_wcsnicmp_l";
    internal const int AllLocaleCategories = 0;
}
