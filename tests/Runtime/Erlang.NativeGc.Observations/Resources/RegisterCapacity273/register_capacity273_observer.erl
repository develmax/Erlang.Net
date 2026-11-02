-module(register_capacity273_observer).
-compile(no_inline).
-export([run/0, local_tail/1, local_return/1, receive_clone/1, implicit_apply/3, lambda/1, make_lambda/1, invoke_lambda/2]).
run() ->
    {local_tail({payload, <<1,2,3>>}), local_return(17),
     receive_clone({receive_payload, [a,b,c]}),
     implicit_apply(erlang, abs, -273), lambda(19)}.
local_tail(Value) -> tail_target(Value).
tail_target(Value) -> {tail_result, Value}.
local_return(Value) -> {caller_before, return_target(Value), caller_after}.
return_target(Value) -> {callee_result, Value}.
receive_clone(Value) ->
    self() ! {skip, 1}, self() ! {match, Value},
    Received = receive {match, Payload} when is_tuple(Payload) -> Payload after 0 -> timeout end,
    receive {skip, 1} -> {receive_result, Received} after 0 -> missing end.
implicit_apply(Module, Function, Value) -> Module:Function(Value).
lambda(Captured) -> invoke_lambda(make_lambda(Captured), 23).
make_lambda(Captured) -> fun(Arg) -> {Captured, Arg} end.
invoke_lambda(Fun, Arg) -> Fun(Arg).
