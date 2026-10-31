-module(gc_binary818).
-compile(no_inline).
-export([capture/0,entry/1,body/2,left_alias/2,right_alias/2,live/3,
         guarded/2,inside_try/2,inside_catch/2,add/2,multiply/2,divide/2,shift/2]).

capture() ->
    {Pid,Ref}=spawn_monitor(?MODULE,entry,[self()]),
    receive
        {gc_binary818,Pid,Rows} ->
            receive {'DOWN',Ref,process,Pid,normal} -> Rows;
                    {'DOWN',Ref,process,Pid,Reason} -> erlang:error({worker_failed,Reason})
            after 30000 -> erlang:error(worker_down_timeout) end;
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({worker_failed,Reason})
    after 30000 -> erlang:error(worker_rows_timeout) end.

entry(Parent) ->
    Rows=rows(),
    Parent!{gc_binary818,self(),Rows},
    ok.

row(Name,Function,Arguments) ->
    try erlang:apply(?MODULE,Function,Arguments) of
        Value -> {Name,{ok,Value}}
    catch Class:Reason:Stack -> {Name,{error,Class,Reason,Stack}} end.

body(Binary,Range) -> erlang:binary_part(Binary,Range).
left_alias(Binary,Range) ->
    Part=erlang:binary_part(Binary,Range),
    {Part,Range}.
right_alias(Binary,Range) ->
    Part=erlang:binary_part(Binary,Range),
    {Binary,Part}.
live(Binary,Range,Keep) ->
    Saved={Binary,Range,Keep},
    Part=erlang:binary_part(Binary,Range),
    {Part,Saved,Keep}.
guarded(Binary,Range) when byte_size(binary_part(Binary,Range)) =:= 2 ->
    {matched,Binary,Range};
guarded(Binary,Range) -> {missed,Binary,Range}.
inside_try(Binary,Range) ->
    try erlang:binary_part(Binary,Range) of Part -> {ok,Part,Binary,Range}
    catch Class:Reason:Stack -> {caught,Class,Reason,Stack,Binary,Range} end.
inside_catch(Binary,Range) -> {(catch erlang:binary_part(Binary,Range)),Binary,Range}.
add(Left,Right) -> Left+Right.
multiply(Left,Right) -> Left*Right.
divide(Left,Right) -> Left div Right.
shift(Left,Right) -> Left bsl Right.

rows() ->
    B= <<0,1,2,3,4>>,
    [row(body_forward,body,[B,{1,2}]),
     row(body_negative_length,body,[B,{3,-2}]),
     row(body_empty_end,body,[B,{5,0}]),
     row(body_empty_binary,body,[<<>>,{0,0}]),
     row(left_alias,left_alias,[B,{1,2}]),
     row(right_alias,right_alias,[B,{1,2}]),
     row(live_registers,live,[B,{1,2},{keep,[a,b,c],<<255,0,1>>}]),
     row(live_badarg,live,[B,{0,99},{keep,[a,b,c],<<255,0,1>>}]),
     row(body_bad_binary_atom,body,[bad,{0,1}]),
     row(body_bad_binary_list,body,[[0,1,2],{0,1}]),
     row(body_bad_bitstring,body,[<<0:9>>,{0,1}]),
     row(body_bad_range_atom,body,[B,bad]),
     row(body_bad_range_three,body,[B,{0,1,2}]),
     row(body_bad_range_list,body,[B,[0,1]]),
     row(body_bad_start_float,body,[B,{0.0,1}]),
     row(body_bad_length_atom,body,[B,{0,bad}]),
     row(body_negative_start,body,[B,{-1,1}]),
     row(body_underflow,body,[B,{0,-1}]),
     row(body_beyond_end,body,[B,{6,0}]),
     row(body_overflow,body,[B,{4,2}]),
     row(body_huge_start,body,[B,{1 bsl 100,0}]),
     row(guard_success,guarded,[B,{1,2}]),
     row(guard_value_miss,guarded,[B,{0,1}]),
     row(guard_bad_binary,guarded,[bad,{0,1}]),
     row(guard_bad_range,guarded,[B,bad]),
     row(guard_bad_bounds,guarded,[B,{4,2}]),
     row(try_success,inside_try,[B,{1,2}]),
     row(try_failure,inside_try,[B,{4,2}]),
     row(catch_success,inside_catch,[B,{1,2}]),
     row(catch_failure,inside_catch,[B,{4,2}]),
     row(arithmetic_add,add,[17,5]),
     row(arithmetic_add_badarg,add,[bad,5]),
     row(arithmetic_multiply,multiply,[17,5]),
     row(arithmetic_multiply_badarg,multiply,[17,bad]),
     row(arithmetic_divide,divide,[17,5]),
     row(arithmetic_divide_zero,divide,[17,0]),
     row(arithmetic_shift,shift,[17,2]),
     row(arithmetic_shift_badarg,shift,[bad,2])].
