namespace Erlang;

// Private version1126 ABI: deliberately NOT RuntimeMicrostateAction numeric order.
// The IPC/source adapter must map actions explicitly by semantic name.
internal enum ErlDriverAsyncAccountingAction1126 : uint
{
    Enable,
    Disable,
    Reset,
    Gather
}
