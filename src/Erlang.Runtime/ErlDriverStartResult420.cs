namespace Erlang;

internal sealed record ErlDriverStartResult420(nint Data, int NativeErrno)
{
    internal bool Failed => (long)Data is ErlDriverPortNames420.StartGeneralFailure
        or ErlDriverPortNames420.StartErrnoFailure or ErlDriverPortNames420.StartBadArgument;
}
