namespace Erlang;

internal static class ErlDriverPortNames420
{
    internal const int ControlBufferBytes = 64;
    internal const int CallBufferBytes = 256;
    internal const int BinaryControlFlag = 1;
    internal const uint KeepCallBufferFlag = 1;
    internal const int ExternalTermVersion = 131;
    internal const long StartGeneralFailure = -1;
    internal const long StartErrnoFailure = -2;
    internal const long StartBadArgument = -3;
    internal const int PortLockingFlag = 1;
    internal const string StartExport = "erldrv420_start_entry";
}
