namespace Erlang.Differential;

public static class ProcessInfoAsyncDistributionCaseNames
{
    public const string Prefix = "process-info-async-dist-";
    public const string ModulePrefix = "-module(oracle_process_info_async_dist). -export([run/0]). run()->";
    public const string ModuleSuffix = ".";
    public const string Flag = "async_dist";
    public const string Default = "default-false";
    public const string Transitions = "self-transitions-and-invalid-no-mutation";
    public const string List = "list-order-duplicates-and-default-query-exclusion";
    public const string Child = "other-process-transitions-and-child-noninheritance";
    public const string Dead = "dead-target-and-full-item-validation";
    public const string Unknown = "unknown-local-valid-max-pid-and-remote-domain";
    public const string DefaultBody = "{async_dist,false}=process_info(self(),async_dist),done";
    public const string TransitionsBody = """
        false=process_flag(async_dist,true), {async_dist,true}=process_info(self(),async_dist),
        true=process_flag(async_dist,false), {async_dist,false}=process_info(self(),async_dist),
        ok=try process_flag(async_dist,invalid_async_value) of _->unexpected catch error:badarg->ok end,
        {async_dist,false}=process_info(self(),async_dist),done
        """;
    public const string ListBody = """
        false=process_flag(async_dist,true), {trap_exit,Trap}=process_info(self(),trap_exit),
        [{priority,normal},{async_dist,true},{async_dist,true},{trap_exit,Trap}]=
            process_info(self(),[priority,async_dist,async_dist,trap_exit]),
        []=process_info(self(),[]), false=lists:keymember(async_dist,1,process_info(self())),done
        """;
    public const string ChildBody = """
        Owner=self(),false=process_flag(async_dist,true),
        {P,R}=spawn_monitor(fun()->Owner!{ready,self(),process_info(self(),async_dist)},
            receive turn_on->false=process_flag(async_dist,true),Owner!{enabled,self()},receive stop->ok end end end),
        try
            receive {ready,P,{async_dist,false}}->ok after 5000->erlang:error(child_ready_timeout) end,
            {async_dist,false}=process_info(P,async_dist), P!turn_on,
            receive {enabled,P}->ok after 5000->erlang:error(child_enabled_timeout) end,
            {async_dist,true}=process_info(P,async_dist),
            [{links,[]},{async_dist,true},{links,[]},{async_dist,true}]=process_info(P,[links,async_dist,links,async_dist]),
            {async_dist,true}=process_info(self(),async_dist),done
        after
            exit(P,kill),receive {'DOWN',R,process,P,_}->ok after 5000->erlang:error(child_cleanup_timeout) end
        end
        """;
    public const string DeadBody = """
        {P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok after 5000->erlang:error(dead_ready_timeout) end,
        undefined=process_info(P,async_dist),undefined=process_info(P,[async_dist,async_dist]),undefined=process_info(P,[]),
        ok=try process_info(P,[async_dist,unknown_async_info]) of _->unexpected catch error:badarg->ok end,
        ok=try process_info(P,[async_dist|invalid_tail]) of _->unexpected catch error:badarg->ok end,
        ok=try process_info(self(),{async_dist,invalid_tuple}) of _->unexpected catch error:badarg->ok end,
        ok=try process_info(make_ref(),async_dist) of _->unexpected catch error:badarg->ok end,done
        """;
    public const string UnknownBody = """
        <<131,88,B/binary>>=term_to_binary(self()),S=byte_size(B)-12,
        <<N:S/binary,_:32,_:32,C:32>>=B,
        MaxLocalPidNumber=(1 bsl 28)-1,
        P=binary_to_term(<<131,88,N/binary,MaxLocalPidNumber:32,0:32,C:32>>),
        false=is_process_alive(P),undefined=process_info(P,async_dist),undefined=process_info(P,[async_dist]),
        ok=try process_info(P,[async_dist,unknown_async_info]) of _->unexpected catch error:badarg->ok end,
        <<131,RemoteNode/binary>>=term_to_binary(async_info_remote@invalid),
        Remote=binary_to_term(<<131,88,RemoteNode/binary,1:32,0:32,1:32>>),
        ok=try process_info(Remote,async_dist) of _->unexpected catch error:badarg->ok end,done
        """;
}
