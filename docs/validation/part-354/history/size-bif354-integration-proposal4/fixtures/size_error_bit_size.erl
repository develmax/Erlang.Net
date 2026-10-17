-module(size_error_bit_size).
-export([run/0,qualified/1,auto/1,external/1,named/1,apply_three/1,apply_two/1]).
run() ->
    lists:foreach(fun(V)->lists:foreach(fun(F)->check(F,V) end,
        [fun qualified/1,fun auto/1,fun external/1,fun named/1,fun apply_three/1,fun apply_two/1]) end,values()), done.
qualified(V) -> erlang:bit_size(V).
auto(V) -> bit_size(V).
external(V) -> F=fun erlang:bit_size/1,F(V).
named(V) -> F=fun bit_size/1,F(V).
apply_three(V) -> M=erlang,F=bit_size,apply(M,F,[V]).
apply_two(V) -> F=fun erlang:bit_size/1,apply(F,[V]).
check(F,V) ->
    try F(V) of _->error(missing_failure)
    catch error:Reason:Stack ->
        badarg=Reason,
        [{erlang,bit_size,[V],[{error_info,ErrorInfo}]}|Tail]=Stack,
        true=(ErrorInfo =:= #{module=>erl_erts_errors}),
        1=length([present || {erlang,bit_size,[_],_} <- Stack]), true=is_list(Tail), ok
    end.
values() ->
    Native=records:create(test,a,[{x,1}],#{is_exported=>false}),
    ExternalPid=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),
    ExternalRef=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),
    ExternalPort=binary_to_term(<<131,89,119,13,"external@host",0,0,0,1,0,0,0,0>>),
    [bad,0,1 bsl 200,1.5,[],[a|tail],self(),make_ref(),fun()->ok end,
     fun erlang:self/0,Native,ExternalPid,ExternalRef,ExternalPort,{}, {a,b},#{a=>1}].
