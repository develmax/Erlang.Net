namespace Erlang.Otp;

internal enum RpcWorkerKind
{
    Execute,
    Cast,
    Middleman,
    NodesObserver,
    ProxyUser,
    CnodeGroupLeader
}
