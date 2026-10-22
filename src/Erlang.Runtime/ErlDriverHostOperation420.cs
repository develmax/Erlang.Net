namespace Erlang;

internal enum ErlDriverHostOperation420
{
    Initialize,
    Load,
    Unload,
    OpenPort,
    Output,
    Control,
    Call,
    ClosePort,
    Shutdown,
    PortOutput,
    PortTermOutput,
    PortFailure,
    PortStopped,
    RegisterLinkedEntry,
    RemoveLinkedEntry,
    PortBusy,
    PortConnected,
    MonitorProcess,
    DemonitorProcess,
    ProcessExited,
    LockDriver,
    PrimitiveHost,
    SchedulePortTask,
    InvokePortTask
}
