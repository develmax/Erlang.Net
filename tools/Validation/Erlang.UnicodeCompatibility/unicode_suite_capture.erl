-module(unicode_suite_capture).
-export([write/1]).

%% Unchanged pinned upstream tests execute only in the official development oracle.
%% Their actual unique exported unicode calls are replayed through the same observer.
write(Directory) ->
    Parent = self(),
    Collector = spawn(fun() -> collect(Parent,#{}) end),
    Worker = spawn(fun() ->
        receive start -> ok end,
        Tests = [exceptions,latin1,normalize,huge_illegal_code_points,
                 bin_is_7bit,incomplete_utf32_to_latin1],
        Results = [{T,catch apply(unicode_SUITE,T,[[]])} || T <- Tests],
        Parent ! {suite_results,Results},
        receive stop -> ok end
    end),
    erlang:trace(Worker,true,[call,{tracer,Collector}]),
    erlang:trace_pattern({unicode,'_','_'},true,[]),
    Worker ! start,
    Results = receive {suite_results,R} -> R end,
    Delivery = erlang:trace_delivered(Worker),
    receive {trace_delivered,Worker,Delivery} -> ok end,
    erlang:trace(Worker,false,[call]),
    erlang:trace_pattern({unicode,'_','_'},false,[]),
    Worker ! stop,
    Collector ! finish,
    Cases = receive {captured,C} -> C end,
    ok = file:write_file(filename:join(Directory,"upstream-suite-results.etf"),term_to_binary(Results)),
    io:format("Upstream suite results: ~p~nCaptured unique calls: ~p~n",[Results,length(Cases)]),
    true = lists:all(fun({_,Value}) -> Value =:= ok end,Results),
    Rows = [{F,A,unicode_port_cases:run(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"upstream-suite-original.etf"),term_to_binary(Rows)).

collect(Parent,Cases) ->
    receive
        {trace,_,call,{unicode,F,A}} -> collect(Parent,Cases#{{F,A} => true});
        finish -> Parent ! {captured,lists:sort(maps:keys(Cases))}
    end.
