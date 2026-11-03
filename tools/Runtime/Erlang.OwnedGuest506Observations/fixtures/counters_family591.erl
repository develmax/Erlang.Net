-module(counters_family591).
-export([run/0, add_worker/4]).

run() ->
    {counters591,
     [{profile, [{wordsize, erlang:system_info(wordsize)},
                 {schedulers, erlang:system_info(schedulers)},
                 {schedulers_online, erlang:system_info(schedulers_online)},
                 {debug_compiled, erlang:system_info(debug_compiled)}]},
      {families, [family591(Options) || Options <- [[], [atomics], [write_concurrency]]]},
      {invalid_new, [capture591(fun() -> counters:new(Size, Options) end)
                    || {Size, Options} <- [{0,[]}, {-1,[]}, {1.0,[]}, {bad_size,[]},
                       {1,[bad_option]}, {1,[atomics,write_concurrency]},
                       {1,[write_concurrency|bad_tail]}, {1 bsl 128,[write_concurrency]}]]},
      {native_family, native591()},
      {scheduler_handoff, concurrent591()}]}.

family591(Options) ->
    R = counters:new(9, Options),
    Empty = [counters:get(R, I) || I <- lists:seq(1,9)],
    ok = counters:put(R, 9, 9223372036854775807),
    ok = counters:add(R, 9, 1),
    Wrapped = counters:get(R, 9),
    ok = counters:put(R, 9, -9223372036854775808),
    ok = counters:sub(R, 9, 1),
    Back = counters:get(R, 9),
    ok = counters:put(R, 1, 7),
    ok = counters:add(R, 1, 18446744073709551615),
    Decremented = counters:get(R, 1),
    {Options, [{handle_witness, R}, {initial, Empty}, {wrap, Wrapped},
               {sub_wrap, Back}, {unsigned_increment, Decremented},
               {complete_info, counters:info(R)},
               {invalid_get, [capture591(fun() -> counters:get(R, Ix) end)
                              || Ix <- [0,-1,10,1.0,bad_index]]},
               {invalid_put, [capture591(fun() -> counters:put(R, 1, Value) end)
                              || Value <- [9223372036854775808,-9223372036854775809,1.0,bad_value]]},
               {invalid_add, [capture591(fun() -> counters:add(R, 1, Value) end)
                              || Value <- [18446744073709551616,-9223372036854775809,1.0,bad_value]]}]}.

native591() ->
    R = erts_internal:counters_new(1),
    WrongKind = atomics:new(1, []),
    Initial = erts_internal:counters_get(R, 1),
    ok = erts_internal:counters_add(R, 1, 18446744073709551615),
    Added = erts_internal:counters_get(R, 1),
    ok = erts_internal:counters_put(R, 1, -9223372036854775808),
    Put = erts_internal:counters_get(R, 1),
    [{handle_witness, R}, {wrong_kind_witness, WrongKind}, {initial,Initial},
     {added,Added}, {put,Put}, {complete_info,erts_internal:counters_info(R)},
     {new_errors,[capture591(fun() -> erts_internal:counters_new(V) end)
                  || V <- [0,-1,1.0,bad_size,1 bsl 128]]},
     {wrong_kind,capture591(fun() -> erts_internal:counters_get(WrongKind,1) end)},
     {bad_reference,capture591(fun() -> erts_internal:counters_info(not_a_reference) end)},
     {index_errors,[capture591(fun() -> erts_internal:counters_get(R,V) end)
                    || V <- [0,2,-1,1.0,bad_index]]}].

concurrent591() ->
    R = counters:new(1, [write_concurrency]),
    ok = counters:put(R,1,17),
    Root = self(), Token = make_ref(),
    Lanes = lists:seq(1, min(4,erlang:system_info(schedulers_online))),
    Workers = [spawn_monitor(?MODULE,add_worker,[Root,Token,R,Lane]) || Lane <- Lanes],
    try
        lists:foreach(fun({Pid,_}) ->
            receive {Token,ready,Pid} -> ok after 10000 -> error({no_ready591,Pid}) end
        end,Workers),
        lists:foreach(fun({Pid,_}) -> Pid ! {Token,go} end,Workers),
        Reasons = [receive {'DOWN',M,process,Pid,Why} -> Why
                   after 10000 -> error({no_down591,Pid}) end || {Pid,M} <- Workers],
        case lists:all(fun(Why) -> Why =:= normal end,Reasons) of
            true -> ok;
            false -> error({bad_worker591,Reasons})
        end,
        Value = counters:get(R,1),
        ok = counters:put(R,1,-19),
        Reset = counters:get(R,1),
        ok = counters:add(R,1,2),
        [{handle_witness,R},{lanes,Lanes},{iterations,200},{base,17},
         {after_workers,Value},{after_put,Reset},{after_add,counters:get(R,1)},
         {down_reasons,Reasons},{complete_info,counters:info(R)}]
    after
        lists:foreach(fun({Pid,M}) -> exit(Pid,kill), demonitor(M,[flush]) end,Workers)
    end.

add_worker(Root,Token,R,Lane) ->
    _ = process_flag(scheduler,Lane),
    Root ! {Token,ready,self()},
    receive {Token,go} -> ok after 10000 -> error(no_go591) end,
    lists:foreach(fun(_) -> counters:add(R,1,1) end,lists:seq(1,200)).

capture591(Fun) ->
    try {returned,Fun()} catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.
