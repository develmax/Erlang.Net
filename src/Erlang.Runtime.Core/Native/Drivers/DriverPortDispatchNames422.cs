namespace Erlang;

internal static class DriverPortDispatchNames422
{
    internal const string OutsidePortTurn = "Driver timeslice consumption outside an actual managed port execution turn is not supported";
    internal const string MissingCallback = "Native port task callback is missing";
    internal const string NoOnlineLane = "Native port task requires an actual online runtime scheduler lane";
    internal const string ActorLeak = "Auxiliary native port callback inherited a normal actor execution turn";
}
