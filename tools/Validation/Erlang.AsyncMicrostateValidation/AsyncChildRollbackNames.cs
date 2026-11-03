namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncChildRollbackNames
{
    internal const string CaseName = "component-child-reservation-rollback";
    internal const string PackageManifest = "PackageManifest";
    internal const string DdllBeam = "DdllBeam";
    internal const string DriverDirectory = "DriverDirectory";
    internal const string DriverName = "async_msacc1127_drv";
    internal const string Erlang = "erlang";
    internal const string ErtsInternal = "erts_internal";
    internal const string Ddll = "erl_ddll";
    internal const string Lists = "lists";
    internal const string Maps = "maps";
    internal const string Binary = "binary";
    internal const string File = "file";
    internal const string IoLibrary = "io_lib";
    internal const string String = "string";
    internal const string Init = "init";
    internal const string Io = "io";
    internal const string Load = "load_driver";
    internal const string Unload = "unload_driver";
    internal const string Open = "open_port";
    internal const string ClosePort = "port_close";
    internal const string SpawnDriver = "spawn_driver";
    internal const string Ok = "ok";
    internal const string True = "true";
    internal const string ReasonAtom = "reserved_child_exit1389";
    internal const int ReasonValue = 1389;
    internal const string ChildName = "rollback_child1385";
    internal const string GateField = "gate";
    internal const string RegistryField = "ports";
    internal const string DriversField = "drivers";
    internal const string ActorsField = "actors";
    internal const string TransportField = "transport";
    internal const string NativeRowsField = "native";
    internal const string DriverRowsField = "identities";
    internal const string CloseMethod = "Close";
    internal const string Failure = "Actual unprepared child rollback did not settle logical/physical custody";
    internal const ulong Attempt = 1;
    internal const ulong WrongIncrement = 1;
    internal const int WorkerCount = 1;
    internal const int ReservationCount = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan FenceDeadline = TimeSpan.FromSeconds(5);
}
