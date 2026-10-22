namespace Erlang;

internal static class BeamReceiveMarkerNames
{
    internal const string Reserve = "recv_marker_reserve";
    internal const string Bind = "recv_marker_bind";
    internal const string Clear = "recv_marker_clear";
    internal const string Use = "recv_marker_use";
    internal const string InvalidSlot = "A private BEAM receive marker is required in this register";
}
