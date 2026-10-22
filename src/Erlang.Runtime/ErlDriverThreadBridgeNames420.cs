namespace Erlang;

internal static class ErlDriverThreadBridgeNames420
{
    internal const string WrongThread = "A native driver owner scope must finish on its original physical thread";
    internal const string Configure = "erldrv420_configure_threads";
    internal const string ExchangeOwner = "erldrv420_exchange_owner";
    internal const string Callbacks = "erldrv420_thread_callbacks";
    internal const string Close = "erldrv420_close_threads";
    internal const int NoMemory = 12;
    internal const int InvalidArgument = 22;
    internal const int CallbackCount = 29;
    internal const string InvalidBinary = "Native driver thread bridge differs from its actual build receipt";
    internal const string NotDrained = "Native driver thread services have not been explicitly drained and closed";
    internal static readonly string[] CallbackPrefixes =
        ["erl_drv_mutex_", "erl_drv_cond_", "erl_drv_rwlock_", "erl_drv_tsd_", "erl_drv_thread_"];
    internal const string EqualThreads = "erl_drv_equal_tids";
}
