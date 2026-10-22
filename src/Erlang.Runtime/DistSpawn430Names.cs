namespace Erlang;

internal static class DistSpawn430Names
{
    internal const string Module = "erts_internal", Function = "dist_spawn_request", Request = "spawn_request", BadArgument = "badarg", BadOptions = "badopt", NoConnection = "noconnection", NotSupported = "notsup", Reply = "spawn_reply", Error = "error";
    internal const string Foreign = "Distribution spawn primitive belongs to another runtime";
    internal const string Conflict = "Distribution spawn native primitive conflicts with another source owner";
    internal const string Pending = "Distribution spawn pending/automatic connection output owner is not implemented";
    internal const string Async = "Distribution spawn asynchronous ignore-busy output admission is not implemented";
    internal const string Monitor = "Distribution spawn negotiated monitor downgrade is not implemented";
    internal const int Arity = 4;
}
