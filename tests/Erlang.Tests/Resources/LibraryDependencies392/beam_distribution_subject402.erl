-module(beam_distribution_subject402).
-export([worker/2,probe/1]).
worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent!{Case,Outcome},ok.
remote()->'pending402@localhost'.
with_pending(Body)->N=remote(),H=erts_internal:new_connection(N),try Body(N,H) after erts_internal:abort_pending_connection(N,H) end.
probe(creation_current)->{erts_internal:get_creation(),node()};
probe(pending_abort)->with_pending(fun(N,H)->erts_internal:abort_pending_connection(N,H) end);
probe(idle_repeat)->with_pending(fun(N,H)->A=erts_internal:abort_pending_connection(N,H),{A,erts_internal:abort_pending_connection(N,H)} end);
probe(stale_generation)->with_pending(fun(N,H)->erts_internal:abort_pending_connection(N,H),H2=erts_internal:new_connection(N),try {erts_internal:abort_pending_connection(N,H),H2=:=erts_internal:new_connection(N),erts_internal:abort_pending_connection(N,H2)} after erts_internal:abort_pending_connection(N,H2) end end);
probe(high_word_alias)->with_pending(fun(N,{Id,Ref})->erts_internal:abort_pending_connection(N,{Id+16#100000000,Ref}) end);
probe(low_word_alias)->with_pending(fun(N,{Id,Ref})->erts_internal:abort_pending_connection(N,{Id-16#100000000,Ref}) end);
probe(stale_negative_id)->with_pending(fun(N,{_,Ref})->erts_internal:abort_pending_connection(N,{-1,Ref}) end);
probe(bad_node)->erts_internal:abort_pending_connection(17,bad_handle);
probe(bad_handle)->erts_internal:abort_pending_connection(remote(),bad_handle);
probe(bad_shape)->erts_internal:abort_pending_connection(remote(),{0,bad_ref,extra});
probe(bad_generation_type)->erts_internal:abort_pending_connection(remote(),{bad_id,bad_ref});
probe(wrong_node_raw)->with_pending(fun(_,H)->erts_internal:abort_pending_connection('other402@localhost',H) end);
probe(ordinary_reference_raw)->erts_internal:abort_pending_connection(remote(),{0,make_ref()});
probe(serialized_handle_raw)->with_pending(fun(N,H)->Copied=binary_to_term(term_to_binary(H)),{Copied=:=H,erts_internal:abort_pending_connection(N,Copied)} end);
probe(handle_raw)->with_pending(fun(_,H)->H end);
probe({connected,Node})->
    true=net_kernel:connect_node(Node),
    try H=erts_internal:new_connection(Node),{erts_internal:abort_pending_connection(Node,H),lists:member(Node,nodes(connected)),H=:=erts_internal:new_connection(Node)}
    after erlang:disconnect_node(Node) end.
