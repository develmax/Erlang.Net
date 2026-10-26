namespace Erlang;

internal static class NativeDriverPortNames422
{
    internal const int StartGeneral = -1;
    internal const int StartErrno = -2;
    internal const int StartBadArgument = -3;
    internal const int BinaryFlag = 1;
    internal const int LineFlag = 2;
    internal const uint SoftBusyFlag = 1U << 1;
    internal const string InvalidValue = "einval";
    internal const string DriverUnloaded = "driver_unloaded";
    internal const string NativeCleanupFailed = "Native driver port cleanup reported failure";
    internal const string MonitorInfoUnsupported = "Native driver port monitor metadata requires actual origin enumeration";
    internal const string QueueInfoUnsupported = "Native driver queue size requires the actual child queue state";
    internal const string RawMessageUnsupported = "Native driver raw port-message admission is not yet implemented; use the actual port BIF routes";
    internal const string ConnectUnsupported = "Native driver connected-owner transition is not yet implemented";
    internal const string CallerRequired = "Native driver command requires an actual runtime process context";
}
