namespace Erlang.Otp;

internal static class RpcWorkerIdentity
{
    internal const int OldUnique = 0;
    internal const int IdentityBytes = 16;
    internal const int BroadcastIndex = 6;
    internal const string BroadcastFunction = "-eval_everywhere/4-fun-0-";
    internal static readonly string[] Functions =
        ["-handle_call/3-fun-0-", "-handle_cast/2-fun-0-", "-do_srv_call/3-fun-0-",
         "-start_nodes_observer/0-fun-1-", "-proxy_user/0-fun-0-", "-cnode_call_group_leader_start/1-fun-0-"];
}
