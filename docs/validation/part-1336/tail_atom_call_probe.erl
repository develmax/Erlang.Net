-module(tail_atom_call_probe).
-export([dispatch/3, choice/3, join/2, echo/1]).
dispatch(Module, A, B) ->
    Handler = join,
    case erlang:function_exported(Module, Handler, 2) of
        false -> fallback(Module,A,B);
        true -> Module:Handler(A,B)
    end.
choice(Selector, Module, Value) ->
    case pass(Selector) of
        no -> fallback(Module,Value,none);
        yes -> Module:echo(Value)
    end.
fallback(Module,A,B) -> {fallback,Module,A,B}.
join(A,B) -> {A,B}.
echo(Value) -> {echo,Value}.
pass(Value) -> Value.
