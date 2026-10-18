-module(operator371_source_lines).
-export([run/0,unary/1,binary/2,outer_unary/0,operand_failure/0]).
run() ->
    {capture(unary,[bad]),capture(binary,[bad,2]),capture(outer_unary,[]),
     order(),guard(bad),false andalso erlang:error(unexpected),true orelse erlang:error(unexpected)}.

unary(Value) ->
    -
        Value.

binary(Left,Right) ->
    Left
        rem
        Right.

outer_unary() ->
    -
        operand_failure().

operand_failure() ->
    erlang:error(operand_failed).

order() ->
    put(operator371_order,[]),
    try mark(left,bad)
            rem
            mark(right,2)
    of Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {get(operator371_order),Class,Reason,Stack} end.

mark(Key,Value) -> put(operator371_order,[Key|get(operator371_order)]),Value.
guard(Value) when Value rem 2 =:= 0 -> even;
guard(_) -> other.
capture(Function,Arguments) ->
    try erlang:apply(operator371_source_lines,Function,Arguments) of Value -> {unexpected,Value}
    catch Class:Reason:Stack -> {Class,Reason,Stack} end.
