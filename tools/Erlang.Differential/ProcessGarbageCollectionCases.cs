namespace Erlang.Differential;

public static class ProcessGarbageCollectionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function switch
    {
        ProcessGarbageCollectionNames.Function when arity == 0 => "erlang-gc-self-zero",
        ProcessGarbageCollectionNames.Function when arity == 1 => "erlang-gc-self-one",
        ProcessGarbageCollectionNames.Function when arity == 2 => "erlang-gc-self-minor",
        ProcessGarbageCollectionNames.MessageAreaFunction when arity == 0 => "erlang-gc-message-area-stub",
        _ => null
    };

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-gc-" + name,
            "-module(gc_contract). -export([run/0]). run()->" + body + ",done. worker(Parent)->Parent!{ready,self()},receive finish->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("self-zero", "true=erlang:garbage_collect()");
        Add("self-one", "true=erlang:garbage_collect(self())");
        Add("self-minor", "true=erlang:garbage_collect(self(),[{type,minor}])");
        Add("self-major", "true=erlang:garbage_collect(self(),[{type,major}])");
        Add("type-overwrite", "true=erlang:garbage_collect(self(),[{type,wrong},{type,minor},{type,major}])");
        Add("self-async", "async=erlang:garbage_collect(self(),[{async,request}]),receive {garbage_collect,request,true}->ok end");
        Add(
            "self-async-minor",
            "async=erlang:garbage_collect(self(),[{async,request},{type,minor}]),receive {garbage_collect,request,true}->ok end"
        );
        Add(
            "arbitrary-id",
            "R={request,[head|tail]},async=erlang:garbage_collect(self(),[{async,R}]),receive {garbage_collect,R,true}->ok end"
        );
        Add(
            "async-overwrite",
            "async=erlang:garbage_collect(self(),[{async,first},{async,last}]),receive {garbage_collect,last,true}->ok end,receive {garbage_collect,first,_}->error(extra) after 0->ok end"
        );
        Add(
            "duplicate-request",
            "async=erlang:garbage_collect(self(),[{async,same}]),async=erlang:garbage_collect(self(),[{async,same}]),receive {garbage_collect,same,true}->ok end,receive {garbage_collect,same,true}->ok end"
        );
        Add("mailbox-preserved", "self()!kept,true=erlang:garbage_collect(),receive kept->ok end");
        Add("dictionary-preserved", "put(kept,{value,[1,2,3]}),true=erlang:garbage_collect(),{value,[1,2,3]}=get(kept)");
        Add("message-area-stub", "try erlang:garbage_collect_message_area() of _->error(accepted) catch error:undefined->ok end");
        const string start = "Parent=self(),P=spawn(fun()->worker(Parent) end),receive {ready,P}->ok end,";
        const string end = ",M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok end";
        Add("other-sync", start + "true=erlang:garbage_collect(P)" + end);
        Add(
            "other-async",
            start + "async=erlang:garbage_collect(P,[{async,request}]),receive {garbage_collect,request,true}->ok end" + end
        );
        Add("suspended", start + "true=erlang:suspend_process(P),true=erlang:garbage_collect(P),true=erlang:resume_process(P)" + end);
        Add(
            "suspended-async",
            start + "true=erlang:suspend_process(P),async=erlang:garbage_collect(P,[{async,request}]),receive {garbage_collect,request,true}->ok end,true=erlang:resume_process(P)" + end
        );
        const string dead = "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,normal}->ok end,";
        Add("dead-sync", dead + "false=erlang:garbage_collect(P)");
        Add("dead-async", dead + "async=erlang:garbage_collect(P,[{async,request}]),receive {garbage_collect,request,false}->ok end");
        Add(
            "exit-before-gc",
            start + "M=monitor(process,P),exit(P,kill),async=erlang:garbage_collect(P,[{async,request}]),receive {'DOWN',M,process,P,killed}->ok end,receive {garbage_collect,request,false}->ok end"
        );
        foreach (string target in new[] { "wrong", "42", "[]", "make_ref()", "{pid,self()}" })
            Add("bad-pid-" + cases.Count, "try erlang:garbage_collect(" + target + ") of _->error(accepted) catch error:badarg->ok end");
        foreach (string options in new[] { "wrong", "[wrong]", "[{async}]", "[{type,major,extra}]", "[{async,request}|tail]", "[{unknown,minor}]" })
            Add(
                "bad-options-" + cases.Count,
                "try erlang:garbage_collect(wrong," + options + ") of _->error(accepted) catch error:badarg:S->[{erlang,garbage_collect,_,L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=bad_option}}=lists:keyfind(error_info,1,L) end"
            );
        foreach (string type in new[] { "wrong", "42", "[]", "{major}" })
            Add(
                "bad-type-" + cases.Count,
                "try erlang:garbage_collect(self(),[{type," + type + "}]) of _->error(accepted) catch error:badarg:S->[{erlang,garbage_collect,_,L}|_]=S,{error_info,I}=lists:keyfind(error_info,1,L),false=maps:is_key(cause,I) end"
            );
        Add(
            "foreign-pid",
            "P=binary_to_term(<<131,88,119,12,102,111,114,101,105,103,110,64,104,111,115,116,0,0,0,1,0,0,0,0,0,0,0,0>>),try erlang:garbage_collect(P) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "foreign-before-type",
            "P=binary_to_term(<<131,88,119,12,102,111,114,101,105,103,110,64,104,111,115,116,0,0,0,1,0,0,0,0,0,0,0,0>>),try erlang:garbage_collect(P,[{async,r},{type,wrong}]) of _->error(accepted) catch error:badarg->ok end,receive {garbage_collect,_,_}->error(extra) after 0->ok end"
        );

        return cases;
    }
}
