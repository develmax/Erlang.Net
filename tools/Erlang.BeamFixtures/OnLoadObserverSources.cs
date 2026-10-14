namespace BeamFixtureExport;

internal static class OnLoadObserverSources
{
    internal static string Old(OnLoadObserverScenario scenario) => $"-module({scenario.Module}).\n-export([run/0]).\nrun() -> old_image.\n";

    internal static string New(OnLoadObserverScenario scenario) => $$"""
        -module({{scenario.Module}}).
        -on_load(init/0).
        -export([run/0]).
        init() ->
            Observer = whereis({{OnLoadObserverNames.Observer}}),
            true = is_pid(Observer),
            Observer ! {on_load_notice,?MODULE,self(),code:is_loaded(?MODULE),{{(scenario.Succeeds ? "success" : "failure")}}},
            receive
                {on_load_continue,Observer} -> {{(scenario.Succeeds ? OnLoadObserverNames.CallbackSuccessResult : OnLoadObserverNames.CallbackFailureResult)}}
            after {{OnLoadObserverNames.WaitMilliseconds}} ->
                error(observer_timeout)
            end.
        run() -> new_image.
        """ + "\n";

    internal static string Expected(OnLoadObserverScenario scenario)
    {
        string load = scenario.Succeeds ? $"{{module,{scenario.Module}}}" : "{error,on_load_failure}";
        string before = scenario.HasOldImage ? "{value,old_image}" : "{error,undef}";
        string during = scenario.HasOldImage ? "{value,old_image}" : "not_invoked";
        string after = scenario.Succeeds ? "{value,new_image}" : before;

        string callbackReason = scenario.Succeeds ? OnLoadObserverNames.CallbackSuccessResult : OnLoadObserverNames.CallbackFailureResult;

        return $"{{observed,{load},{callbackReason},{before},{during},{after}}}";
    }

    internal static string Evaluation(
        OnLoadObserverScenario scenario,
        string oldBeam,
        string newBeam,
        string directory
    )
    {
        string file(string name) => String(Path.Combine(directory, name));
        string oldLoad = scenario.HasOldImage
            ? $"{{ok,OldBinary}}=file:read_file({String(oldBeam)}),{{module,M}}=code:load_binary(M,\"{OnLoadObserverNames.OldVersion}\",OldBinary),"
            : "";
        string before = scenario.HasOldImage ? $"{{file,\"{OnLoadObserverNames.OldVersion}\"}}" : "false";
        string beforeRun = scenario.HasOldImage ? "{value,old_image}" : "{error,undef}";
        string duringRun = scenario.HasOldImage ? "ObserveRun()" : "not_invoked";
        string expectedDuring = scenario.HasOldImage ? "{value,old_image}" : "not_invoked";
        string after = scenario.Succeeds ? $"{{file,\"{OnLoadObserverNames.NewVersion}\"}}" : before;
        string afterRun = scenario.Succeeds ? "{value,new_image}" : beforeRun;
        string loadResult = scenario.Succeeds ? "{module,M}" : "{error,on_load_failure}";

        return $$"""
            try
                M={{scenario.Module}},Observer=self(),true=register({{OnLoadObserverNames.Observer}},Observer),
                ObserveRun=fun()->try M:run() of V->{value,V} catch error:undef->{error,undef}; C:R->{C,R} end end,
                {{oldLoad}}
                Before=code:is_loaded(M),BeforeRun=ObserveRun(),
                ok=file:write_file({{file("before.etf")}},term_to_binary({Before,BeforeRun})),
                {{before}}=Before,{{beforeRun}}=BeforeRun,
                {ok,NewBinary}=file:read_file({{String(newBeam)}}),
                {Worker,Ref}=spawn_monitor(fun()->Load=code:load_binary(M,"{{OnLoadObserverNames.NewVersion}}",NewBinary),Observer!{observer_load_result,self(),Load} end),
                Notice=receive
                    N={on_load_notice,M,_,_,_}->N;
                    Early={observer_load_result,Worker,_}->ok=file:write_file({{file("early-load.etf")}},term_to_binary(Early)),error({missing_notice,Early});
                    NoticeWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file({{file("early-worker-down-before-notice.etf")}},term_to_binary(NoticeWorkerDown)),error({worker_before_notice,NoticeWorkerDown})
                after {{OnLoadObserverNames.WaitMilliseconds}}->error(notice_timeout) end,
                ok=file:write_file({{file("notice.etf")}},term_to_binary(Notice)),
                {on_load_notice,M,Callback,NoticeAvailability,{{(scenario.Succeeds ? "success" : "failure")}}}=Notice,
                true=is_pid(Callback),false=(Callback=:=Observer),false=(Callback=:=Worker),
                During=code:is_loaded(M),DuringRun={{duringRun}},
                ok=file:write_file({{file("during.etf")}},term_to_binary({During,DuringRun})),
                {{before}}=NoticeAvailability,{{before}}=During,{{expectedDuring}}=DuringRun,
                CallbackRef=monitor(process,Callback),
                Callback!{on_load_continue,Observer},
                CallbackDown=receive CD={'DOWN',CallbackRef,process,Callback,_}->CD after {{OnLoadObserverNames.WaitMilliseconds}}->error(callback_timeout) end,
                ok=file:write_file({{file("callback-down.etf")}},term_to_binary(CallbackDown)),
                {'DOWN',CallbackRef,process,Callback,CallbackReason}=CallbackDown,
                {{(scenario.Succeeds ? OnLoadObserverNames.CallbackSuccessResult : OnLoadObserverNames.CallbackFailureResult)}}=CallbackReason,
                LoadResult=receive
                    {observer_load_result,Worker,L}->L;
                    LoadWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file({{file("early-worker-down-before-load-result.etf")}},term_to_binary(LoadWorkerDown)),error({worker_before_load_result,LoadWorkerDown})
                after {{OnLoadObserverNames.WaitMilliseconds}}->error(load_timeout) end,
                ok=file:write_file({{file("load-result.etf")}},term_to_binary(LoadResult)),
                {{loadResult}}=LoadResult,
                Down=receive D={'DOWN',Ref,process,Worker,_}->D after {{OnLoadObserverNames.WaitMilliseconds}}->error(worker_timeout) end,
                ok=file:write_file({{file("worker-down.etf")}},term_to_binary(Down)),
                {'DOWN',Ref,process,Worker,normal}=Down,
                After=code:is_loaded(M),AfterRun=ObserveRun(),
                ok=file:write_file({{file("after.etf")}},term_to_binary({After,AfterRun})),
                {{after}}=After,{{afterRun}}=AfterRun,
                Actual={observed,LoadResult,CallbackReason,BeforeRun,DuringRun,AfterRun},Expected={{Expected(scenario)}},
                ok=file:write_file({{file("actual.etf")}},term_to_binary({ok,Actual})),
                ok=file:write_file({{file("expected.etf")}},term_to_binary({ok,Expected})),
                true=(Actual=:=Expected),true=unregister({{OnLoadObserverNames.Observer}}),halt(0)
            catch Class:Reason:Stack ->
                ok=file:write_file({{file("failure.etf")}},term_to_binary({Class,Reason,Stack})),
                io:format(standard_error,"~tp~n",[{Class,Reason,Stack}]),halt(3)
            end.
            """ + "\n";
    }

    internal static string String(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
