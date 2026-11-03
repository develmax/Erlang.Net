-module(atomics_magic586).
-export([run/0]).

%% Extends batch585's prepared observer without seeding expected observations.
%% Raw reference IDs are not successful results. Unexpected full errors/stacks
%% retain their actual terms, including any argument references.
run() ->
    [{wordsize, erlang:system_info(wordsize)},
     {atomics_all_seven,
      [{Count,Signed,outcome(fun() -> exercise(Count,Signed) end)} ||
          Count <- [1,2,7,8,9,32,33], Signed <- [true,false]]},
     {constructor_errors, constructor_errors()},
     {native_errors, native_errors()},
     {counters_original_default_and_atomics,
      [{Options,outcome(fun() -> counters_case(Options) end)} ||
          Options <- [[],[atomics]]]}].

exercise(Count,Signed) ->
    Ref = atomics:new(Count,[{signed,Signed}]),
    Other = atomics:new(Count,[{signed,Signed}]),
    Shared = {Ref,Ref},
    Independent = {Ref,Other},
    Before = {sizes(Ref),sizes(Shared),sizes(Independent)},
    Put = atomics:put(Ref,1,7),
    Get = atomics:get(Ref,1),
    Add = atomics:add(Ref,1,3),
    AddGet = atomics:add_get(Ref,1,-2),
    Exchange = atomics:exchange(Ref,1,11),
    Hit = atomics:compare_exchange(Ref,1,11,13),
    Miss = atomics:compare_exchange(Ref,1,11,17),
    Info = atomics:info(Ref),
    true = garbage_collect(),
    After = {sizes(Ref),sizes(Shared),sizes(Independent)},
    [{is_reference,is_reference(Ref)},
     {equal_self,Ref =:= Ref},
     {distinct_identity,Ref =/= Other},
     {same_self,erts_debug:same(Ref,Ref)},
     {same_distinct,erts_debug:same(Ref,Other)},
     {all_native_results,{Put,Get,Add,AddGet,Exchange,Hit,Miss,Info}},
     {after_gc_value,atomics:get(Ref,1)},
     {before_sizes,Before},{after_sizes,After}].

constructor_errors() ->
    [{zero,outcome(fun() -> atomics:new(0,[]) end)},
     {negative,outcome(fun() -> atomics:new(-1,[]) end)},
     {wrong_size,outcome(fun() -> atomics:new(bad,[]) end)},
     {wrong_options,outcome(fun() -> atomics:new(1,bad) end)},
     {improper_options,outcome(fun() -> atomics:new(1,[{signed,true}|bad]) end)},
     {wrong_signed,outcome(fun() -> atomics:new(1,[{signed,bad}]) end)},
     {unknown_option,outcome(fun() -> atomics:new(1,[bad]) end)}].

native_errors() ->
    Ref = atomics:new(2,[]),
    [{get_index_zero,outcome(fun() -> atomics:get(Ref,0) end)},
     {put_index_past_end,outcome(fun() -> atomics:put(Ref,3,0) end)},
     {add_wrong_increment,outcome(fun() -> atomics:add(Ref,1,bad) end)},
     {add_get_wrong_index,outcome(fun() -> atomics:add_get(Ref,bad,0) end)},
     {exchange_wrong_value,outcome(fun() -> atomics:exchange(Ref,1,bad) end)},
     {compare_wrong_value,outcome(fun() -> atomics:compare_exchange(Ref,1,bad,0) end)},
     {info_wrong_reference,outcome(fun() -> atomics:info(bad) end)}].

counters_case(Options) ->
    Counter = counters:new(2,Options),
    {atomics,Ref} = Counter,
    Other = counters:new(2,Options),
    Put = counters:put(Counter,1,3),
    Add = counters:add(Counter,1,2),
    Sub = counters:sub(Counter,1,1),
    Before = {sizes(Counter),sizes({Counter,Counter}),sizes({Counter,Other})},
    true = garbage_collect(),
    [{actual_original_wrapper,atomics},
     {reference_payload,is_reference(Ref)},
     {results,{Put,Add,Sub,counters:get(Counter,1),counters:info(Counter)}},
     {sizes_before,Before},
     {sizes_after,{sizes(Counter),sizes({Counter,Counter}),sizes({Counter,Other})}}].

sizes(Value) ->
    [{flat,erts_debug:flat_size(Value)},
     {shared,erts_debug:size_shared(Value)}].

outcome(Fun) ->
    try Fun() of
        Value -> {returned,Value}
    catch
        Class:Reason:Stack -> {raised,Class,Reason,Stack}
    end.
