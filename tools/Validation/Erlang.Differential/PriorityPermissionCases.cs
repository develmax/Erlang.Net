namespace Erlang.Differential;

public static class PriorityPermissionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string inactive = "{priority_messages,false}=process_info(self(),priority_messages)";
        const string active = "{priority_messages,true}=process_info(self(),priority_messages)";
        const string target = "P=spawn(fun()->receive stop->ok end end)";

        void Add(string name, string body) => cases.Add(new(
            "erlang-priority-permission-" + name,
            "-module(priority_permission_contract). -export([run/0,await_down/2]). run()->" + body + ",done. await_down(_,0)->error(no_down); await_down(R,N)->case process_info(self(),messages) of {messages,[{'DOWN',R,process,_,normal}]}->ok;_->receive after 1->await_down(R,N-1) end end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("dormant", inactive);
        Add("ordinary-alias", "A=alias()," + inactive + ",true=unalias(A)," + inactive);
        Add("priority-alias", "A=alias([priority])," + active + ",true=unalias(A)," + inactive + ",false=unalias(A)," + inactive);
        Add(
            "multiple-aliases",
            "A=alias([priority]),B=alias([priority])," + active + ",true=unalias(A)," + active + ",true=unalias(B)," + inactive
        );
        Add(
            "unread-alias-message",
            "A=alias([priority]),self()!ordinary,ok=erlang:send(A,payload,[priority]),{messages,[payload,ordinary]}=process_info(self(),messages),true=unalias(A)," + inactive + ",{messages,[payload,ordinary]}=process_info(self(),messages),receive payload->ok end,receive ordinary->ok end"
        );
        Add(
            "reply-alias-admission",
            "A=alias([reply,priority])," + active + ",self()!ordinary,ok=erlang:send(A,payload,[priority]),{messages,[payload,ordinary]}=process_info(self(),messages)," + inactive + ",false=unalias(A),receive payload->ok end,receive ordinary->ok end"
        );
        Add("self-link", "true=erlang:link(self(),[priority])," + inactive + ",true=unlink(self())," + inactive);
        Add(
            "link-upgrade-downgrade",
            target + ",true=link(P)," + inactive + ",true=erlang:link(P,[priority])," + active + ",true=erlang:link(P,[priority])," + active + ",true=erlang:link(P,[])," + inactive + ",true=unlink(P),P!stop"
        );
        Add(
            "link-and-alias",
            target + ",A=alias([priority]),true=erlang:link(P,[priority]),true=unalias(A)," + active + ",true=unlink(P)," + inactive + ",P!stop"
        );
        Add("ordinary-monitor", target + ",R=monitor(process,P)," + inactive + ",true=demonitor(R),P!stop");
        Add("priority-monitor", target + ",R=monitor(process,P,[priority])," + active + ",true=demonitor(R)," + inactive + ",P!stop");
        Add(
            "unread-down",
            "P=spawn(fun()->receive stop->ok end end),R=monitor(process,P,[priority]),P!stop,await_down(R,5000)," + inactive + ",{messages,[{'DOWN',R,process,_,normal}]}=process_info(self(),messages),receive {'DOWN',R,process,_,normal}->ok end"
        );
        Add(
            "retained-monitor-alias",
            target + ",R=monitor(process,P,[priority,{alias,explicit_unalias}])," + active + ",true=demonitor(R)," + inactive + ",self()!ordinary,ok=erlang:send(R,payload,[priority]),{messages,[ordinary,payload]}=process_info(self(),messages),true=unalias(R),receive ordinary->ok end,receive payload->ok end,P!stop"
        );
        Add("time-offset-monitor", "R=monitor(time_offset,clock_service,[priority])," + active + ",true=demonitor(R)," + inactive);
        Add("self-monitor", "R=monitor(process,self(),[priority])," + inactive + ",true=demonitor(R)," + inactive);
        Add(
            "spawn-request-monitor",
            "R=spawn_request(fun()->receive stop->ok end end,[{monitor,[priority]}])," + active + ",receive {spawn_reply,R,ok,P}->true=demonitor(R)," + inactive + ",P!stop after 5000->error(no_reply) end"
        );
        Add(
            "spawn-request-suppressed-success",
            "S=self(),R=spawn_request(fun()->S!{child,self()},receive stop->ok end end,[{monitor,[priority]},{reply,error_only}])," + active + ",receive {child,P}->true=demonitor(R)," + inactive + ",P!stop after 5000->error(no_child) end,receive {spawn_reply,R,_,_}->error(unexpected_reply) after 0->ok end"
        );
        Add(
            "list-duplicates",
            "[{priority_messages,false},{priority_messages,false}]=process_info(self(),[priority_messages,priority_messages]),A=alias([priority]),[{priority_messages,true},{priority_messages,true}]=process_info(self(),[priority_messages,priority_messages]),true=unalias(A)"
        );
        Add(
            "dead-target",
            "P=spawn(fun()->receive stop->ok end end),R=monitor(process,P),P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end,undefined=process_info(P,priority_messages)"
        );

        return cases;
    }
}
