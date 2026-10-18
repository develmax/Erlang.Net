-module(source_bif_continuation_contract).
-export([direct/1,forwarded/1,protected_body/1,of_body/1,after_body/1,anonymous_scope/1]).
direct(Tuple) -> Value=erlang:element(16777216,Tuple), {continued,Value}.
forwarded(Tuple) -> Value=pass_element(16777216,Tuple), {continued,Value}.
pass_element(Index,Tuple) -> erlang:element(Index,Tuple).
protected_body(Tuple) ->
    try Value=erlang:element(16777216,Tuple), {continued,Value}
    catch error:badarg -> caught end.
of_body(Tuple) ->
    try ok of
        ok -> Value=erlang:element(16777216,Tuple), {continued,Value}
    catch error:badarg -> caught end.
after_body(Tuple) ->
    try Value=erlang:element(16777216,Tuple), {continued,Value}
    after source_bif_continuation_observer:cleanup() end.
anonymous_scope(Tuple) ->
    Function=fun() -> Value=erlang:element(16777216,Tuple), {inner,Value} end,
    Result=Function(), {outer,Result}.
