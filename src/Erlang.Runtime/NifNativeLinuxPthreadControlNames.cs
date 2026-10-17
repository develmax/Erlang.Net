namespace Erlang;

internal static class NifNativeLinuxPthreadControlNames
{
    internal const string Libc = "libc.so.6";
    internal const string Register = "nif_bridge_pthread_register";
    internal const string Provider = "nif_bridge_pthread_provider";
    internal const string Calls = "nif_bridge_pthread_calls";
    internal static readonly string[] Symbols = ["pthread_create", "pthread_join", "pthread_key_create", "pthread_key_delete", "pthread_getspecific", "pthread_setspecific"];
}
