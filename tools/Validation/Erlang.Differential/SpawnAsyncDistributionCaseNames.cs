namespace Erlang.Differential;

public static class SpawnAsyncDistributionCaseNames
{
    public const string Prefix = "spawn-async-dist-";
    public const string ModulePrefix = "-module(oracle_spawn_async_dist). -export([run/0,worker/1]). run()->";
    public const string ModuleSuffix = """
        ,done.
        worker(S)->S!{first,self(),process_info(self(),async_dist)},receive stop->ok after 5000->error(worker_timeout) end.
        observe(P,R,V)->
            try receive {first,P,{async_dist,V}}->ok after 5000->error(first_timeout) end,
                P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(down_timeout) end
            after exit(P,kill) end.
        reply(R)->receive {spawn_reply,R,ok,P}->P after 5000->error(reply_timeout) end.
        rejected(F)->try F() of _->error(accepted) catch error:badarg->ok end.
        bad_reply(R)->receive {spawn_reply,R,error,badopt}->ok after 5000->error(badopt_timeout) end.
        disconnected(R)->receive {spawn_reply,R,error,noconnection}->ok after 5000->error(disconnected_timeout) end.
        """;
    public const string Fun = "fun-first-instruction-true";
    public const string FunBody = "S=self(),{P,R}=spawn_opt(fun()->worker(S) end,[{async_dist,true},monitor]),observe(P,R,true)";
    public const string LocalFun = "local-node-fun-first-instruction-true";
    public const string LocalFunBody = "S=self(),{P,R}=spawn_opt(node(),fun()->worker(S) end,[{async_dist,true},monitor]),observe(P,R,true)";
    public const string Mfa = "mfa-first-instruction-true";
    public const string MfaBody = "S=self(),{P,R}=spawn_opt(oracle_spawn_async_dist,worker,[S],[{async_dist,true},monitor]),observe(P,R,true)";
    public const string LocalMfa = "local-node-mfa-first-instruction-true";
    public const string LocalMfaBody = "S=self(),{P,R}=spawn_opt(node(),oracle_spawn_async_dist,worker,[S],[{async_dist,true},monitor]),observe(P,R,true)";
    public const string False = "false-and-last-option-wins";
    public const string FalseBody = "S=self(),{P,R}=spawn_opt(fun()->worker(S) end,[{async_dist,true},{async_dist,false},monitor]),observe(P,R,false),{Q,T}=spawn_opt(fun()->worker(S) end,[{async_dist,false},{async_dist,true},monitor]),observe(Q,T,true)";
    public const string Noninheritance = "parent-true-default-child-and-grandchild-false";
    public const string NoninheritanceBody = """
        false=process_flag(async_dist,true),S=self(),
        {P,R}=spawn_opt(fun()->{async_dist,false}=process_info(self(),async_dist),
            false=process_flag(async_dist,true),T=self(),
            {G,M}=spawn_opt(fun()->worker(T) end,[monitor]),observe(G,M,false),worker(S) end,[monitor]),
        observe(P,R,true),{async_dist,true}=process_info(self(),async_dist)
        """;
    public const string LinkMonitor = "true-with-link-monitor-and-normal-exit";
    public const string LinkMonitorBody = """
        process_flag(trap_exit,true),S=self(),{P,R}=spawn_opt(fun()->worker(S) end,[link,monitor,{async_dist,true}]),
        {links,L}=process_info(P,links),true=lists:member(S,L),observe(P,R,true),
        receive {'EXIT',P,normal}->ok after 5000->error(exit_timeout) end
        """;
    public const string RequestFun = "request-fun-first-instruction-true";
    public const string RequestFunBody = "S=self(),R=spawn_request(fun()->worker(S) end,[monitor,{async_dist,true}]),P=reply(R),observe(P,R,true)";
    public const string RequestMfa = "request-local-node-mfa-first-instruction-true";
    public const string RequestMfaBody = "S=self(),R=spawn_request(node(),oracle_spawn_async_dist,worker,[S],[monitor,{async_dist,true}]),P=reply(R),observe(P,R,true)";
    public const string RequestFalse = "request-last-option-false-and-custom-reply-tag";
    public const string RequestFalseBody = """
        S=self(),R=spawn_request(fun()->worker(S) end,[monitor,{async_dist,true},{async_dist,false},{reply_tag,async_reply}]),
        receive {async_reply,R,ok,P}->observe(P,R,false) after 5000->error(custom_reply_timeout) end
        """;
    public const string Invalid = "invalid-values-and-shapes-never-create-child";
    public const string InvalidBody = """
        S=self(),F=fun()->S!unexpected_child end,
        rejected(fun()->spawn_opt(F,[{async_dist,1}]) end),
        rejected(fun()->spawn_opt(F,[{async_dist,wrong},{async_dist,true}]) end),
        rejected(fun()->spawn_opt(F,[{async_dist,true},{async_dist,wrong}]) end),
        rejected(fun()->spawn_opt(F,[async_dist]) end),
        rejected(fun()->spawn_opt(F,[{async_dist}]) end),
        rejected(fun()->spawn_opt(F,[{async_dist,true,extra}]) end),
        rejected(fun()->spawn_opt(F,[{async_dist,true}|wrong]) end),
        receive unexpected_child->error(child_created) after 0->ok end
        """;
    public const string RequestInvalid = "request-invalid-stays-badopt-before-and-after-valid";
    public const string RequestInvalidBody = """
        S=self(),F=fun()->S!unexpected_child end,
        bad_reply(spawn_request(F,[{async_dist,wrong},{async_dist,true}])),
        bad_reply(spawn_request(F,[{async_dist,true},{async_dist,wrong}])),
        receive unexpected_child->error(child_created) after 0->ok end
        """;
    public const string Disconnected = "disconnected-request-defers-creator-options";
    public const string DisconnectedBody = """
        F=fun()->error(unexpected_remote_child) end,
        disconnected(spawn_request('missing@localhost',F,[{async_dist,true}])),
        disconnected(spawn_request('missing@localhost',F,[{async_dist,wrong}])),
        bad_reply(spawn_request('missing@localhost',F,[{async_dist,true},{reply,wrong}]))
        """;
    public const string Evaluation = "argument-failure-before-option-validation";
    public const string EvaluationBody = """
        put(order,initial),
        caught=try spawn_opt(begin put(order,callback),error(first_failure) end,
            begin put(order,options),[{async_dist,wrong}] end) of _->unexpected catch error:first_failure->caught end,
        callback=get(order)
        """;
}
