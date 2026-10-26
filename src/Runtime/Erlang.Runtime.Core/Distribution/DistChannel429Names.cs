namespace Erlang;

internal static class DistChannel429Names
{
    internal const string Module = "erts_internal", Function = "create_dist_channel", BadArgument = "badarg", Ok = "ok", Message = "message", System = "system";
    internal const string Foreign = "Distribution channel belongs to another runtime";
    internal const string Conflict = "Distribution channel native primitive conflicts with an existing owner";
    internal const string PortFrontier = "Distribution driver-port attachment/output owner is not implemented";
    internal const string DownFrontier = "Native pending-node-down channel retry requires a genuine retirement permit";
    internal const string FlagsFrontier = "Negotiated distribution capability is not implemented by the actual codec";
    internal const int Arity = 3;
}
