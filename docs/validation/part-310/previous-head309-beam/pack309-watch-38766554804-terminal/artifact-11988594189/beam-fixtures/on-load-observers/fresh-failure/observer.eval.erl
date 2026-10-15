try
    M=beam_on_load_fresh_failure,Observer=self(),true=register(beam_on_load_observer,Observer),
    ObserveRun=fun()->try M:run() of V->{value,V} catch error:undef->{error,undef}; C:R->{C,R} end end,
    
    Before=code:is_loaded(M),BeforeRun=ObserveRun(),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/before.etf",term_to_binary({Before,BeforeRun})),
    false=Before,{error,undef}=BeforeRun,
    {ok,NewBinary}=file:read_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/new/beam_on_load_fresh_failure.beam"),
    {Worker,Ref}=spawn_monitor(fun()->Load=code:load_binary(M,"observer-new.beam",NewBinary),Observer!{observer_load_result,self(),Load} end),
    Notice=receive
        N={on_load_notice,M,_,_,_}->N;
        Early={observer_load_result,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/early-load.etf",term_to_binary(Early)),error({missing_notice,Early});
        NoticeWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/early-worker-down-before-notice.etf",term_to_binary(NoticeWorkerDown)),error({worker_before_notice,NoticeWorkerDown})
    after 10000->error(notice_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/notice.etf",term_to_binary(Notice)),
    {on_load_notice,M,Callback,NoticeAvailability,failure}=Notice,
    true=is_pid(Callback),false=(Callback=:=Observer),false=(Callback=:=Worker),
    During=code:is_loaded(M),DuringRun=not_invoked,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/during.etf",term_to_binary({During,DuringRun})),
    false=NoticeAvailability,false=During,not_invoked=DuringRun,
    CallbackRef=monitor(process,Callback),
    Callback!{on_load_continue,Observer},
    CallbackDown=receive CD={'DOWN',CallbackRef,process,Callback,_}->CD after 10000->error(callback_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/callback-down.etf",term_to_binary(CallbackDown)),
    {'DOWN',CallbackRef,process,Callback,CallbackReason}=CallbackDown,
    not_ok=CallbackReason,
    LoadResult=receive
        {observer_load_result,Worker,L}->L;
        LoadWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/early-worker-down-before-load-result.etf",term_to_binary(LoadWorkerDown)),error({worker_before_load_result,LoadWorkerDown})
    after 10000->error(load_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/load-result.etf",term_to_binary(LoadResult)),
    {error,on_load_failure}=LoadResult,
    Down=receive D={'DOWN',Ref,process,Worker,_}->D after 10000->error(worker_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/worker-down.etf",term_to_binary(Down)),
    {'DOWN',Ref,process,Worker,normal}=Down,
    After=code:is_loaded(M),AfterRun=ObserveRun(),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/after.etf",term_to_binary({After,AfterRun})),
    false=After,{error,undef}=AfterRun,
    Actual={observed,LoadResult,CallbackReason,BeforeRun,DuringRun,AfterRun},Expected={observed,{error,on_load_failure},not_ok,{error,undef},not_invoked,{error,undef}},
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/actual.etf",term_to_binary({ok,Actual})),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/expected.etf",term_to_binary({ok,Expected})),
    true=(Actual=:=Expected),true=unregister(beam_on_load_observer),halt(0)
catch Class:Reason:Stack ->
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/fresh-failure/failure.etf",term_to_binary({Class,Reason,Stack})),
    io:format(standard_error,"~tp~n",[{Class,Reason,Stack}]),halt(3)
end.
