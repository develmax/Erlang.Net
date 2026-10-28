-module(spawn_resources540).
-export([run/0, target/0]).

%% Observed full values/stacks are the oracle. No expected result is projected
%% into this worker; every malformed option must leave no successful child.
run() ->
    Defaults = #{minimum_heap => erlang:system_info(min_heap_size),
                 minimum_binary_heap => erlang:system_info(min_bin_vheap_size),
                 wordsize => erlang:system_info(wordsize)},
    {min_heap_size,Minimum} = erlang:system_info(min_heap_size),
    Cases = cases() ++ [{max_below_actual_default,[{max_heap_size,Minimum-1}]}],
    {Defaults, [{Api, Id, observe(fun() -> invoke(Api, Options) end)}
                || Api <- apis(), {Id, Options} <- Cases]}.

target() -> ok.

apis() ->
    [erlang_opt2, erlang_opt3, erlang_opt4, erlang_opt5,
     erlang_request2, erlang_request3, erlang_request4, erlang_request5,
     proc_opt2, proc_opt3, proc_opt4, proc_opt5,
     proc_start5, proc_start_link5, proc_start_monitor5].

cases() ->
    [{min_negative, [{min_heap_size,-1}]},
     {min_noninteger, [{min_heap_size,invalid}]},
     {min_bignum, [{min_heap_size,1 bsl 70}]},
     {binary_negative, [{min_bin_vheap_size,-1}]},
     {binary_noninteger, [{min_bin_vheap_size,invalid}]},
     {max_negative, [{max_heap_size,-1}]},
     {max_missing_size, [{max_heap_size,#{}}]},
     {max_bad_size, [{max_heap_size,#{size=>invalid}}]},
     {max_bad_kill, [{max_heap_size,#{size=>0,kill=>1}}]},
     {max_bad_log, [{max_heap_size,#{size=>0,error_logger=>invalid}}]},
     {max_bad_shared, [{max_heap_size,#{size=>0,include_shared_binaries=>0}}]},
     {min_sticky, [{min_heap_size,-1},{min_heap_size,0}]},
     {binary_sticky, [{min_bin_vheap_size,-1},{min_bin_vheap_size,0}]},
     {max_sticky, [{max_heap_size,#{size=>0,kill=>invalid}},{max_heap_size,0}]},
     {max_below_explicit_min, [{min_heap_size,234},{max_heap_size,233}]},
     {min_then_unknown, [{min_heap_size,0},unknown_option]},
     {unknown_then_min, [unknown_option,{min_heap_size,0}]},
     {max_then_unknown, [{max_heap_size,#{size=>0,extra=>ignored}},unknown_option]},
     {monitor_precedence, [monitor,{min_heap_size,-1}]},
     {tuple_monitor_precedence, [{monitor,[]},{min_heap_size,-1}]},
     {improper_options, [{min_heap_size,-1}|improper_tail]}].

observe(F) ->
    try F() of Value -> {returned,Value}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack}
    end.

invoke(erlang_opt2,O) -> erlang:spawn_opt(fun target/0,O);
invoke(erlang_opt3,O) -> erlang:spawn_opt(node(),fun target/0,O);
invoke(erlang_opt4,O) -> erlang:spawn_opt(?MODULE,target,[],O);
invoke(erlang_opt5,O) -> erlang:spawn_opt(node(),?MODULE,target,[],O);
invoke(erlang_request2,O) -> request(erlang:spawn_request(fun target/0,O));
invoke(erlang_request3,O) -> request(erlang:spawn_request(node(),fun target/0,O));
invoke(erlang_request4,O) -> request(erlang:spawn_request(?MODULE,target,[],O));
invoke(erlang_request5,O) -> request(erlang:spawn_request(node(),?MODULE,target,[],O));
invoke(proc_opt2,O) -> proc_lib:spawn_opt(fun target/0,O);
invoke(proc_opt3,O) -> proc_lib:spawn_opt(node(),fun target/0,O);
invoke(proc_opt4,O) -> proc_lib:spawn_opt(?MODULE,target,[],O);
invoke(proc_opt5,O) -> proc_lib:spawn_opt(node(),?MODULE,target,[],O);
invoke(proc_start5,O) -> proc_lib:start(?MODULE,target,[],0,O);
invoke(proc_start_link5,O) -> proc_lib:start_link(?MODULE,target,[],0,O);
invoke(proc_start_monitor5,O) -> proc_lib:start_monitor(?MODULE,target,[],0,O).

request(Reference) ->
    receive {spawn_reply,Reference,Status,Value} ->
        %% These cases are all invalid. Retain an unexpected successful child
        %% result and terminate/join it rather than leaving it behind.
        case Status of
            ok when is_pid(Value) ->
                Monitor = erlang:monitor(process,Value),
                exit(Value,kill),
                receive {'DOWN',Monitor,process,Value,Down} ->
                    {unexpected_child,Status,Value,Down}
                after 5000 -> error({cleanup_timeout540,Value}) end;
            _ -> {spawn_reply,Reference,Status,Value}
        end
    after 5000 -> error({reply_timeout540,Reference}) end.
