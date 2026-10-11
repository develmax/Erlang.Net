namespace Erlang.Differential;

public static class IoGroupLeaderCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "io-group-leader-" + name,
            "-module(io_group_contract). -export([run/0]). run()->" + body + ",done. device(Owner,Mode)->receive {io_request,From,Tag,Request}->Owner!{observed,Request},case Mode of die->ok;_->From!{io_reply,Tag,reply(Mode)},receive stop->ok end end end. reply(value)->42;reply(arguments)->{error,arguments};reply(terminated)->{error,terminated};reply(calling_self)->{error,calling_self};reply(translation)->{error,{no_translation,unicode,latin1}};reply(other_error)->{error,custom};reply(ok)->ok. wait_dead(P)->case erlang:is_process_alive(P) of true->erlang:yield(),wait_dead(P);false->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "format-request",
            "G=group_leader(),Owner=self(),P=spawn(fun()->device(Owner,ok) end),true=group_leader(P,Owner),ok=io:format(\"~s\",[\"hello\"]),true=group_leader(G,Owner),receive {observed,{put_chars,unicode,io_lib,format,[\"~s\",[\"hello\"]]}}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
        );
        Add(
            "arbitrary-device-reply",
            "G=group_leader(),Owner=self(),P=spawn(fun()->device(Owner,value) end),true=group_leader(P,Owner),42=io:format(\"\",[]),true=group_leader(G,Owner),receive {observed,_}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
        );
        Add(
            "custom-device-validates-format",
            "G=group_leader(),Owner=self(),P=spawn(fun()->device(Owner,ok) end),true=group_leader(P,Owner),ok=io:format(42,bad),true=group_leader(G,Owner),receive {observed,{put_chars,unicode,io_lib,format,[42,bad]}}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
        );
        Add(
            "selective-reply-reference",
            "G=group_leader(),Owner=self(),Other=make_ref(),self()!{io_reply,Other,stray},P=spawn(fun()->device(Owner,ok) end),true=group_leader(P,Owner),ok=io:format(\"\",[]),true=group_leader(G,Owner),receive {io_reply,Other,stray}->ok after 0->error(lost) end,receive {observed,_}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
        );
        Add(
            "calling-self",
            "G=group_leader(),true=group_leader(self(),self()),ok=try io:format(\"\",[]) of _->unexpected catch error:calling_self->ok after true=group_leader(G,self()) end"
        );
        Add(
            "dead-device",
            "G=group_leader(),P=spawn(fun()->ok end),wait_dead(P),true=group_leader(P,self()),ok=try io:format(\"\",[]) of _->unexpected catch error:terminated->ok after true=group_leader(G,self()) end"
        );
        Add(
            "dead-device-flushes-exit",
            "G=group_leader(),P=spawn(fun()->ok end),wait_dead(P),self()!{'EXIT',P,old},true=group_leader(P,self()),ok=try io:format(\"\",[]) of _->unexpected catch error:terminated->ok after true=group_leader(G,self()) end,receive {'EXIT',P,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "device-dies-during-request",
            "G=group_leader(),Owner=self(),P=spawn(fun()->device(Owner,die) end),true=group_leader(P,Owner),ok=try io:format(\"\",[]) of _->unexpected catch error:terminated->ok after true=group_leader(G,Owner) end,receive {observed,_}->ok after 1000->error(timeout) end,wait_dead(P)"
        );
        foreach (string mode in new[] { "arguments", "terminated", "calling_self", "translation", "other_error" })
        {
            string reason = mode switch
            {
                "arguments" or "other_error" => "badarg",
                "translation" => "no_translation",
                _ => mode
            };
            Add(
                "device-error-" + mode,
                "G=group_leader(),Owner=self(),P=spawn(fun()->device(Owner," + mode + ") end),true=group_leader(P,Owner),ok=try io:format(\"\",[]) of _->unexpected catch error:" + reason + "->ok after true=group_leader(G,Owner) end,receive {observed,_}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
            );
        }
        Add(
            "default-device-survives-format-error",
            "ok=try io:format(\"~Q\",[]) of _->unexpected catch error:badarg->ok end,ok=io:format(\"\",[])"
        );
        Add(
            "default-device-rejects-extra-arguments",
            "ok=try io:format(\"\",[extra]) of _->unexpected catch error:badarg->ok end,ok=io:format(\"\",[])"
        );

        return cases;
    }
}
