namespace Erlang;

internal static class BeamRegisterProtocolNames
{
    internal const string TypedValue = "A private BEAM register payload cannot be read as an Erlang term";
    internal const string Handler = "The BEAM handler tag does not own this live Y frame or slot";
    internal const string Trace = "A BEAM saved trace requires an already captured Erlang exception";
}
