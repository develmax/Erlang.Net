namespace Erlang.Differential;

public static class ClockMonitorCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-clock-monitor-" + name,
            "-module(clock_monitor_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "two-active",
            "R=erlang:monitor(time_offset,clock_service),true=is_reference(R),true=erlang:demonitor(R,[info]),false=erlang:demonitor(R,[info])"
        );
        Add(
            "three-active",
            "R=erlang:monitor(time_offset,clock_service,[]),true=erlang:demonitor(R,[info]),false=erlang:demonitor(R,[info])"
        );
        Add(
            "priority-tag",
            "R=erlang:monitor(time_offset,clock_service,[priority,priority,{tag,first},{tag,{custom,42}}]),true=erlang:demonitor(R,[info])"
        );
        Add(
            "flush-active-all",
            "R=erlang:monitor(time_offset,clock_service),self()!{'CHANGE',R,time_offset,clock_service,one},self()!{custom,R,any,any,two},true=erlang:demonitor(R,[flush,info]),receive {_,R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "flush-active-preserves-other",
            "R=erlang:monitor(time_offset,clock_service),S=make_ref(),self()!{'CHANGE',R,time_offset,clock_service,one},self()!{'CHANGE',S,time_offset,clock_service,two},true=erlang:demonitor(R,[flush]),receive {'CHANGE',S,time_offset,clock_service,two}->ok after 1000->error(timeout) end,receive {_,R,_,_,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "inactive-flush-one",
            "R=erlang:monitor(time_offset,clock_service),true=erlang:demonitor(R,[info]),self()!{one,R,any,any,one},self()!{two,R,any,any,two},false=erlang:demonitor(R,[flush,info]),receive {two,R,any,any,two}->ok after 1000->error(timeout) end"
        );
        Add(
            "remove-without-flush-retains",
            "R=erlang:monitor(time_offset,clock_service),self()!{custom,R,any,any,payload},true=erlang:demonitor(R,[info]),receive {custom,R,any,any,payload}->ok after 1000->error(timeout) end"
        );
        foreach (string mode in new[] { "explicit_unalias", "demonitor", "reply_demonitor" })
        {
            Add(
                "alias-" + mode,
                "R=erlang:monitor(time_offset,clock_service,[{alias," + mode + "}]),true=erlang:demonitor(R,[info])," + (mode == "explicit_unalias" ? "true" : "false") + "=erlang:unalias(R)"
            );
            Add(
                "alias-delivery-" + mode,
                "R=erlang:monitor(time_offset,clock_service,[{alias," + mode + "}]),R!payload,receive payload->ok after 1000->error(timeout) end," + (mode == "reply_demonitor" ? "false" : "true") + "=erlang:demonitor(R,[info])," + (mode == "explicit_unalias" ? "true" : "false") + "=erlang:unalias(R)"
            );
        }
        Add(
            "alias-priority-not-enabled-by-monitor",
            "R=erlang:monitor(time_offset,clock_service,[priority,{alias,explicit_unalias}]),self()!ordinary,ok=erlang:send(R,payload,[priority]),ordinary=receive X->X after 1000->error(timeout) end,payload=receive Y->Y after 1000->error(timeout) end,true=erlang:demonitor(R,[info]),true=erlang:unalias(R)"
        );
        Add(
            "wrong-owner",
            "Owner=self(),P=spawn(fun()->R=erlang:monitor(time_offset,clock_service),Owner!{ref,R},receive remove->true=erlang:demonitor(R,[info]),Owner!removed end end),receive {ref,R}->false=erlang:demonitor(R,[info]),P!remove end,receive removed->ok after 1000->error(timeout) end"
        );
        int index = 0;
        foreach (string target in new[] { "wrong", "self()", "make_ref()", "[]", "42", "{clock_service,node()}" })
            Add("bad-target-" + index++, "ok=try erlang:monitor(time_offset," + target + ",[]) of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string options in new[] { "bad", "[bad]", "[priority|bad]", "[{alias,bad}]", "[{tag}]", "[{tag,one,two}]", "[{priority,true}]" })
            Add(
                "bad-options-" + index++,
                "ok=try erlang:monitor(time_offset,clock_service," + options + ") of _->unexpected catch error:badarg->ok end"
            );

        return cases;
    }
}
