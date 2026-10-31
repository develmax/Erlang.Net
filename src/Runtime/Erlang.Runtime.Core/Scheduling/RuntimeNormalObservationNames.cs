namespace Erlang;

internal static class RuntimeNormalObservationNames
{
    internal const string Requester = "Normal observation requires the actual own normal execution turn";
    internal const string Barrier = "Normal observation cannot await excluded auxiliary jobs while holding a normal barrier";
}
