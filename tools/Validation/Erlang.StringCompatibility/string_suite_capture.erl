-module(string_suite_capture).
-export([write/1]).

%% Run the unchanged pinned upstream tests in the development oracle, then
%% replay their actual exported string calls through our isolated observer.
write(Directory) ->
    Parent = self(),
    Collector = spawn(fun() -> collect(Parent, #{}) end),
    Worker = spawn(fun() ->
        receive start -> ok end,
        Tests = lists:append([Ts || {_,Ts} <- string_SUITE:groups()]),
        Results = [{T,catch apply(string_SUITE,T,[[]])} || T <- Tests, T =/= meas],
        Parent ! {suite_results,Results},
        receive stop -> ok end
    end),
    erlang:trace(Worker,true,[call,{tracer,Collector}]),
    erlang:trace_pattern({string,'_','_'},true,[]),
    Worker ! start,
    Results = receive {suite_results,R} -> R end,
    true = lists:all(fun({_,Value}) -> Value =:= ok end,Results),
    Delivery = erlang:trace_delivered(Worker),
    receive {trace_delivered,Worker,Delivery} -> ok end,
    erlang:trace(Worker,false,[call]),
    erlang:trace_pattern({string,'_','_'},false,[]),
    Worker ! stop,
    Collector ! finish,
    Cases = receive {captured,C} -> C end,
    ok = file:write_file(filename:join(Directory,"upstream-suite-results.etf"),term_to_binary(Results)),
    ok = file:write_file(filename:join(Directory,"upstream-suite-cases.etf"),term_to_binary(Cases)),
    Rows = [{F,Args,string_port_cases:run(F,Args)} || {F,Args} <- Cases],
    ok = file:write_file(filename:join(Directory,"upstream-suite-original.etf"),term_to_binary(Rows)),
    io:format("Upstream suite results: ~p~nCaptured unique calls: ~p~n",[Results,length(Cases)]),
    ok.

collect(Parent, Cases) ->
    receive
        {trace,_,call,{string,F,Args}} -> collect(Parent,Cases#{{F,Args} => true});
        finish -> Parent ! {captured,lists:sort(maps:keys(Cases))}
    end.
