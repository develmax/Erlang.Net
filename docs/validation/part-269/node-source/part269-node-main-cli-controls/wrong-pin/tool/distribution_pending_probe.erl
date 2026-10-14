-module(distribution_pending_probe).
-export([run/0]).

run() ->
    Node = pending_probe@localhost,
    H1 = {Id, Ref} = erts_internal:new_connection(Node),
    true = is_integer(Id),
    true = is_reference(Ref),
    true = Id >= 0 andalso Id =< 16#ffffff,
    H2 = erts_internal:new_connection(Node),
    true = H1 =:= H2,
    S0 = erlang:dist_get_stat(H1),
    io:format(standard_error, "pending_stat=~p~n", [S0]),
    High = classify(fun() -> erlang:dist_get_stat({Id + (1 bsl 32), Ref}) end),
    Low = classify(fun() -> erlang:dist_get_stat({Id - (1 bsl 32), Ref}) end),
    Big = classify(fun() -> erlang:dist_get_stat({1 bsl 59, Ref}) end),
    WrongRef = classify(fun() -> erlang:dist_get_stat({Id, make_ref()}) end),
    true = erts_internal:abort_pending_connection(Node, H1),
    true = erts_internal:abort_pending_connection(Node, H1),
    H3 = {Next, Ref3} = erts_internal:new_connection(Node),
    true = Next =:= ((Id + 1) band 16#ffffff),
    true = Ref =:= Ref3,
    Stale = classify(fun() -> erlang:dist_get_stat(H1) end),
    Current = classify(fun() -> erlang:dist_get_stat(H3) end),
    true = erts_internal:abort_pending_connection(Node, H1),
    true = erts_internal:abort_pending_connection(Node, H3),
    {ok, flat_typed_handle, same_pending_identity, next_connection_id,
     High, Low, Big, WrongRef, Stale, Current}.

classify(F) ->
    try F() of Value -> {ok, Value}
    catch Class:Reason -> {Class, Reason}
    end.
