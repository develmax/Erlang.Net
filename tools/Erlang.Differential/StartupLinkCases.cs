namespace Erlang.Differential;

public static class StartupLinkCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "otp-startup-link-" + name,
            "-module(startup_link_contract). -export([run/0,init/1,child/0,handle_call/3,handle_cast/2]). run()->" + body + ",done. " +
            "init({server,stop})->{stop,failed};init({server,error})->{error,failed};init({server,ignore})->ignore;init({server,ready})->{ok,ready};" +
            "init({server,{check,Owner,Leader}})->{links,[Owner]}=process_info(self(),links),Leader=group_leader(),{ok,ready};" +
            "init({server,{wait,Owner}})->Owner!{initializing,self()},receive proceed->{ok,ready} end;" +
            "init({supervisor,wrong})->wrong;init({supervisor,fail})->{ok,{#{},[#{id=>first,start=>{startup_link_contract,child,[]}}]}};" +
            "init({supervisor,invalid})->{ok,{#{intensity=>-1},[]}};init({supervisor,empty})->{ok,{#{},[]}};" +
            "init({supervisor,{check,Owner,Leader}})->{links,[Owner]}=process_info(self(),links),Leader=group_leader(),{ok,{#{},[]}};" +
            "init(ignore)->ignore. child()->{error,failed}. handle_call(get,_,S)->{reply,S,S}. handle_cast(_,S)->{noreply,S}.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "trapped-server-stop-cleans-exit",
            "process_flag(trap_exit,true),self()!preserved,{error,failed}=gen_server:start_link(startup_link_contract,{server,stop},[]),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end,receive preserved->ok after 1000->error(timeout) end"
        );
        Add("unlinked-server-stop", "{error,failed}=gen_server:start(startup_link_contract,{server,stop},[])");
        Add("untrapped-server-error-normal-exit", "{error,failed}=gen_server:start_link(startup_link_contract,{server,error},[])");
        Add(
            "trapped-server-error-cleans-exit",
            "process_flag(trap_exit,true),{error,failed}=gen_server:start_link(startup_link_contract,{server,error},[]),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end"
        );
        Add("untrapped-server-ignore", "ignore=gen_server:start_link(startup_link_contract,{server,ignore},[])");
        Add(
            "trapped-server-ignore-cleans-exit",
            "process_flag(trap_exit,true),ignore=gen_server:start_link(startup_link_contract,{server,ignore},[]),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end"
        );
        Add(
            "trapped-supervisor-bad-init",
            "process_flag(trap_exit,true),{error,{bad_return,{startup_link_contract,init,wrong}}}=supervisor:start_link(startup_link_contract,{supervisor,wrong}),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end"
        );
        Add(
            "trapped-supervisor-start-failure",
            "process_flag(trap_exit,true),{error,{shutdown,{failed_to_start_child,first,failed}}}=supervisor:start_link(startup_link_contract,{supervisor,fail}),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end"
        );
        Add(
            "trapped-supervisor-invalid-intensity",
            "process_flag(trap_exit,true),{error,{supervisor_data,{invalid_intensity,-1}}}=supervisor:start_link(startup_link_contract,{supervisor,invalid}),receive {'EXIT',_,_}->error(stale_exit) after 0->ok end"
        );
        Add(
            "server-link-before-init",
            "{ok,S}=gen_server:start_link(startup_link_contract,{server,{check,self(),group_leader()}},[]),ok=gen_server:stop(S)"
        );
        Add(
            "supervisor-link-before-init",
            "{ok,S}=supervisor:start_link(startup_link_contract,{supervisor,{check,self(),group_leader()}}),ok=gen_server:stop(S)"
        );
        Add(
            "server-inherited-leader-before-init",
            "G=spawn(fun()->receive stop->ok end end),true=group_leader(G,self()),{ok,S}=gen_server:start_link(startup_link_contract,{server,{check,self(),G}},[]),ok=gen_server:stop(S),G!stop"
        );
        Add(
            "supervisor-inherited-leader-before-init",
            "G=spawn(fun()->receive stop->ok end end),true=group_leader(G,self()),{ok,S}=supervisor:start_link(startup_link_contract,{supervisor,{check,self(),G}}),ok=gen_server:stop(S),G!stop"
        );
        Add(
            "caller-killed-during-server-init",
            "Owner=self(),P=spawn(fun()->gen_server:start_link(startup_link_contract,{server,{wait,Owner}},[]) end),receive {initializing,S}->R=monitor(process,S),true=exit(P,shutdown),receive {'DOWN',R,process,S,shutdown}->ok after 1000->error(orphan) end after 1000->error(timeout) end"
        );
        foreach (string kind in new[] { "server", "supervisor" })
        {
            string start = kind == "server" ? "gen_server:start_link({local,racing_startup},startup_link_contract,{server,ready},[])" :
                "supervisor:start_link({local,racing_startup},startup_link_contract,{supervisor,empty})";
            Add(
                kind + "-concurrent-named-start",
                "Owner=self(),Ps=[spawn(fun()->Result=" + start + ",Owner!{started,self(),Result},receive stop->ok end end)||_<-lists:seq(1,16)]," +
                "Results=[receive {started,P,Result}->{P,Result} after 1000->error(timeout) end||P<-Ps]," +
                "[{Winner,S}]=[{P,S}||{P,{ok,S}}<-Results],15=length([P||{P,{error,{already_started,S}}}<-Results])," +
                "{links,[Winner]}=process_info(S,links),ok=gen_server:stop(S),[P!stop||P<-Ps]"
            );
        }

        return cases;
    }
}
