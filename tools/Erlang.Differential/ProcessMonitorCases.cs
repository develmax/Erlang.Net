namespace Erlang.Differential;

public static class ProcessMonitorCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("monitor", 3) => "erlang-monitor-tag",
        ("demonitor", 2) => "erlang-monitor-info-active",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-monitor-" + name,
            "-module(monitor_contract). -export([run/0]). run()->" + body + ",done. wait_dead(P)->case erlang:is_process_alive(P) of true->erlang:yield(),wait_dead(P);false->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "tag",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{tag,custom}]),P!go,receive {custom,R,process,P,normal}->ok after 1000->error(timeout) end,false=erlang:demonitor(R,[info])"
        );
        Add(
            "tag-last",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{tag,one},{tag,{two,42}}]),P!go,receive {{two,42},R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "tag-map",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{tag,#{key=>42}}]),P!go,receive {#{key:=42},R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "priority-down",
            "self()!ordinary,R=erlang:monitor(process,priority_missing,[priority]),N=node(),{'DOWN',R,process,{priority_missing,N},noproc}=receive M1->M1 after 1000->error(timeout) end,ordinary=receive M2->M2 after 1000->error(timeout) end"
        );
        Add(
            "plain-down-order",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[]),self()!ordinary,P!go,wait_dead(P),ordinary=receive M1->M1 after 1000->error(timeout) end,receive {'DOWN',R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "info-active",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[]),true=erlang:demonitor(R,[info]),false=erlang:demonitor(R,[info]),true=erlang:demonitor(R,[]),P!go,wait_dead(P),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add("info-ordinary", "R=make_ref(),false=erlang:demonitor(R,[info]),true=erlang:demonitor(R,[])");
        Add("self", "R=erlang:monitor(process,self(),[]),false=erlang:demonitor(R,[info])");
        Add(
            "self-alias",
            "R=erlang:monitor(process,self(),[{alias,explicit_unalias}]),false=erlang:unalias(R),false=erlang:demonitor(R,[info]),R!lost,receive lost->error(unexpected) after 0->ok end"
        );
        Add(
            "missing-name",
            "R=erlang:monitor(process,monitor_missing,[{tag,tagged}]),N=node(),receive {tagged,R,process,{monitor_missing,N},noproc}->ok after 1000->error(timeout) end,false=erlang:demonitor(R,[info])"
        );
        Add(
            "missing-local-tuple",
            "N=node(),R=erlang:monitor(process,{monitor_missing,N},[]),receive {'DOWN',R,process,{monitor_missing,N},noproc}->ok after 1000->error(timeout) end"
        );
        Add(
            "named",
            "P=spawn(fun()->receive go->ok end end),true=register(monitored_name,P),R=erlang:monitor(process,monitored_name,[{tag,named}]),P!go,N=node(),receive {named,R,process,{monitored_name,N},normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "flush-any-tag-one",
            "R=make_ref(),self()!{tag,R,1,2,first},self()!{other,R,1,2,second},false=erlang:demonitor(R,[flush,info]),receive {other,R,1,2,second}->ok after 0->error(lost) end,receive {tag,R,1,2,first}->error(unexpected) after 0->ok end"
        );
        Add(
            "flush-exact-reference",
            "R=make_ref(),Other=make_ref(),self()!{tag,Other,1,2,kept},self()!{tag,R,1,2,removed},true=erlang:demonitor(R,[flush]),receive {tag,Other,1,2,kept}->ok after 0->error(lost) end,receive {tag,R,1,2,removed}->error(unexpected) after 0->ok end"
        );
        Add(
            "flush-skips-wrong-arity",
            "R=make_ref(),self()!{tag,R,1,2},self()!{tag,R,1,2,removed},true=erlang:demonitor(R,[flush]),receive {tag,R,1,2}->ok after 0->error(lost) end,receive {tag,R,1,2,removed}->error(unexpected) after 0->ok end"
        );
        Add(
            "flush-active-does-not-drain-user-tuple",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[]),self()!{tag,R,1,2,kept},true=erlang:demonitor(R,[flush,info]),receive {tag,R,1,2,kept}->ok after 0->error(lost) end,P!go,wait_dead(P)"
        );
        Add(
            "explicit-survives-demonitor",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,explicit_unalias}]),true=erlang:demonitor(R,[info]),R!reply,receive reply->ok after 1000->error(timeout) end,true=erlang:unalias(R),P!go,wait_dead(P)"
        );
        Add(
            "explicit-survives-down",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,explicit_unalias}]),P!go,receive {'DOWN',R,_,_,_}->ok after 1000->error(timeout) end,R!reply,receive reply->ok after 1000->error(timeout) end,true=erlang:unalias(R)"
        );
        Add(
            "demonitor-mode-removes-alias",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,demonitor}]),true=erlang:demonitor(R,[info]),false=erlang:unalias(R),R!lost,receive lost->error(unexpected) after 0->ok end,P!go,wait_dead(P)"
        );
        Add(
            "demonitor-mode-down",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,demonitor}]),P!go,receive {'DOWN',R,_,_,_}->ok after 1000->error(timeout) end,false=erlang:unalias(R)"
        );
        Add(
            "reply-demonitor",
            "Owner=self(),P=spawn(fun()->receive {go,R}->R!reply,Owner!barrier end end),R=erlang:monitor(process,P,[{alias,reply_demonitor}]),P!{go,R},receive reply->ok after 1000->error(timeout) end,receive barrier->ok after 1000->error(timeout) end,wait_dead(P),false=erlang:demonitor(R,[info]),false=erlang:unalias(R),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "unalias-keeps-monitor",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,reply_demonitor}]),true=erlang:unalias(R),P!go,receive {'DOWN',R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "monitor-priority-not-alias-permission",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[priority,{alias,explicit_unalias}]),self()!ordinary,ok=erlang:send(R,reply,[priority]),ordinary=receive M1->M1 after 1000->error(timeout) end,reply=receive M2->M2 after 1000->error(timeout) end,true=erlang:demonitor(R,[info]),true=erlang:unalias(R),P!go,wait_dead(P)"
        );
        Add(
            "alias-last-mode",
            "P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[{alias,reply_demonitor},{alias,explicit_unalias}]),R!one,R!two,receive one->ok after 1000->error(timeout) end,receive two->ok after 1000->error(timeout) end,true=erlang:demonitor(R,[info]),true=erlang:unalias(R),P!go,wait_dead(P)"
        );
        Add(
            "standalone-alias-is-not-monitor",
            "R=erlang:alias(),false=erlang:demonitor(R,[info]),R!kept,receive kept->ok after 1000->error(timeout) end,true=erlang:unalias(R)"
        );
        Add(
            "foreign-owner-info-false",
            "Owner=self(),P=spawn(fun()->receive go->ok end end),R=erlang:monitor(process,P,[]),Q=spawn(fun()->Owner!{result,erlang:demonitor(R,[info])} end),receive {result,false}->ok after 1000->error(timeout) end,wait_dead(Q),true=erlang:demonitor(R,[info]),P!go,wait_dead(P)"
        );
        Add(
            "foreign-node-reference",
            "R=erlang:binary_to_term(<<131,90,0,1,119,17,\"elsewhere@invalid\",0:32,1:32>>),ok=try erlang:demonitor(R,[info]) of _->unexpected catch error:badarg->ok end"
        );
        Add(
            "old-local-incarnation",
            "N=erlang:atom_to_binary(node()),S=byte_size(N),R=erlang:binary_to_term(<<131,90,0,1,119,S,N/binary,7:32,1:32>>),false=erlang:demonitor(R,[info]),true=erlang:demonitor(R,[flush])"
        );
        Add(
            "duplicate-flush-info",
            "R=make_ref(),self()!{tag,R,1,2,first},self()!{tag,R,1,2,second},false=erlang:demonitor(R,[flush,info,flush,info]),receive {tag,R,1,2,second}->ok after 0->error(lost) end"
        );
        Add(
            "explicit-missing-name-remains-alias",
            "R=erlang:monitor(process,monitor_missing,[{alias,explicit_unalias}]),receive {'DOWN',R,_,_,noproc}->ok after 1000->error(timeout) end,R!reply,receive reply->ok after 1000->error(timeout) end,true=erlang:unalias(R)"
        );
        Add(
            "demonitor-missing-name-deactivates",
            "R=erlang:monitor(process,monitor_missing,[{alias,demonitor}]),receive {'DOWN',R,_,_,noproc}->ok after 1000->error(timeout) end,false=erlang:unalias(R)"
        );
        Add(
            "foreign-pid-disconnected",
            "P=erlang:binary_to_term(<<131,88,119,17,\"elsewhere@invalid\",1:32,0:32,0:32>>),R=erlang:monitor(process,P,[{tag,remote}]),receive {remote,R,process,P,noconnection}->ok after 1000->error(timeout) end,false=erlang:demonitor(R,[info])"
        );
        Add(
            "pending-missing-demonitor-info",
            "R=erlang:monitor(process,monitor_missing,[]),true=erlang:demonitor(R,[info]),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-missing-demonitor-flush",
            "R=erlang:monitor(process,monitor_missing,[]),true=erlang:demonitor(R,[flush,info]),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-missing-explicit-keeps-alias",
            "R=erlang:monitor(process,monitor_missing,[{alias,explicit_unalias}]),true=erlang:demonitor(R,[info]),R!reply,receive reply->ok after 1000->error(timeout) end,receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end,true=erlang:unalias(R)"
        );
        Add(
            "pending-missing-demonitor-removes-alias",
            "R=erlang:monitor(process,monitor_missing,[{alias,demonitor}]),true=erlang:demonitor(R,[info]),false=erlang:unalias(R),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "admitted-down-kept-after-demonitor",
            "R=erlang:monitor(process,monitor_missing,[]),receive absent->error(unexpected) after 0->ok end,false=erlang:demonitor(R,[info]),receive {'DOWN',R,_,_,noproc}->ok after 1000->error(timeout) end"
        );
        Add(
            "admitted-down-flushed",
            "R=erlang:monitor(process,monitor_missing,[]),receive absent->error(unexpected) after 0->ok end,false=erlang:demonitor(R,[flush,info]),receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "pending-priority-down-cancelled",
            "self()!ordinary,R=erlang:monitor(process,monitor_missing,[priority]),true=erlang:demonitor(R,[info]),ordinary=receive M->M after 1000->error(timeout) end,receive {'DOWN',R,_,_,_}->error(unexpected) after 0->ok end"
        );
        int index = 0;
        foreach (string options in new[] { "bad", "[bad]", "[priority|bad]", "[{alias,reply}]", "[{tag}]", "[{alias,demonitor},42]", "[{tag,a,extra}]" })
            Add(
                "invalid-monitor-" + index++,
                "ok=try erlang:monitor(process,self()," + options + ") of _->unexpected catch error:badarg->ok end"
            );
        index = 0;
        foreach (string options in new[] { "bad", "[bad]", "[flush|bad]", "[{flush,true}]", "[info,42]" })
            Add(
                "invalid-demonitor-" + index++,
                "ok=try erlang:demonitor(make_ref()," + options + ") of _->unexpected catch error:badarg->ok end"
            );

        return cases;
    }
}
