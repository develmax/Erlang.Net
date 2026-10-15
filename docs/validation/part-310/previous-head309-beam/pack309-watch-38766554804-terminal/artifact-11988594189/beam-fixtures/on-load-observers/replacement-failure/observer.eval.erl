try
    M=beam_on_load_replace_failure,Observer=self(),true=register(beam_on_load_observer,Observer),
    ObserveRun=fun()->try M:run() of V->{value,V} catch error:undef->{error,undef}; C:R->{C,R} end end,
    {ok,OldBinary}=file:read_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/old/beam_on_load_replace_failure.beam"),{module,M}=code:load_binary(M,"observer-old.beam",OldBinary),
    Before=code:is_loaded(M),BeforeRun=ObserveRun(),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/before.etf",term_to_binary({Before,BeforeRun})),
    {file,"observer-old.beam"}=Before,{value,old_image}=BeforeRun,
    {ok,NewBinary}=file:read_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/new/beam_on_load_replace_failure.beam"),
    {Worker,Ref}=spawn_monitor(fun()->Load=code:load_binary(M,"observer-new.beam",NewBinary),Observer!{observer_load_result,self(),Load} end),
    Notice=receive
        N={on_load_notice,M,_,_,_}->N;
        Early={observer_load_result,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/early-load.etf",term_to_binary(Early)),error({missing_notice,Early});
        NoticeWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/early-worker-down-before-notice.etf",term_to_binary(NoticeWorkerDown)),error({worker_before_notice,NoticeWorkerDown})
    after 10000->error(notice_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/notice.etf",term_to_binary(Notice)),
    {on_load_notice,M,Callback,NoticeAvailability,failure}=Notice,
    true=is_pid(Callback),false=(Callback=:=Observer),false=(Callback=:=Worker),
    During=code:is_loaded(M),DuringRun=ObserveRun(),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/during.etf",term_to_binary({During,DuringRun})),
    {file,"observer-old.beam"}=NoticeAvailability,{file,"observer-old.beam"}=During,{value,old_image}=DuringRun,
    CallbackRef=monitor(process,Callback),
    Callback!{on_load_continue,Observer},
    CallbackDown=receive CD={'DOWN',CallbackRef,process,Callback,_}->CD after 10000->error(callback_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/callback-down.etf",term_to_binary(CallbackDown)),
    {'DOWN',CallbackRef,process,Callback,CallbackReason}=CallbackDown,
    not_ok=CallbackReason,
    LoadResult=receive
        {observer_load_result,Worker,L}->L;
        LoadWorkerDown={'DOWN',Ref,process,Worker,_}->ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/early-worker-down-before-load-result.etf",term_to_binary(LoadWorkerDown)),error({worker_before_load_result,LoadWorkerDown})
    after 10000->error(load_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/load-result.etf",term_to_binary(LoadResult)),
    {error,on_load_failure}=LoadResult,
    Down=receive D={'DOWN',Ref,process,Worker,_}->D after 10000->error(worker_timeout) end,
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/worker-down.etf",term_to_binary(Down)),
    {'DOWN',Ref,process,Worker,normal}=Down,
    After=code:is_loaded(M),AfterRun=ObserveRun(),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/after.etf",term_to_binary({After,AfterRun})),
    {file,"observer-old.beam"}=After,{value,old_image}=AfterRun,
    Actual={observed,LoadResult,CallbackReason,BeforeRun,DuringRun,AfterRun},Expected={observed,{error,on_load_failure},not_ok,{value,old_image},{value,old_image},{value,old_image}},
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/actual.etf",term_to_binary({ok,Actual})),
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/expected.etf",term_to_binary({ok,Expected})),
    true=(Actual=:=Expected),true=unregister(beam_on_load_observer),halt(0)
catch Class:Reason:Stack ->
    ok=file:write_file("/home/runner/work/Erlang.Net/Erlang.Net/artifacts/beam-fixtures/on-load-observers/replacement-failure/failure.etf",term_to_binary({Class,Reason,Stack})),
    io:format(standard_error,"~tp~n",[{Class,Reason,Stack}]),halt(3)
end.
