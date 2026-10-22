-module(beam_provider_subject408).
-export([worker/2,probe/1,writer/3]).

worker(Parent,Case) ->
    cleanup(),
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack}
            after cleanup() end,
    Parent ! {Case,Outcome},
    ok.

key(N) -> {?MODULE,N}.
cleanup() ->
    persistent_term:erase(key(1)),persistent_term:erase(key(2)),
    case ets:whereis(?MODULE) of undefined -> ok; _ -> ets:delete(?MODULE) end,
    ok.
probe(missing_get1) -> persistent_term:get(key(1));
probe(missing_get2) -> persistent_term:get(key(1),{default,[a,b],#{x=>y}});
probe(erase_missing) -> persistent_term:erase(key(1));
probe(put_roundtrip) -> R=persistent_term:put(key(1),{value,[1,2]}),{R,persistent_term:get(key(1))};
probe(put_new_insert) -> R=persistent_term:put_new(key(1),first),{R,persistent_term:get(key(1))};
probe(put_new_equal) -> persistent_term:put(key(1),{same,[1,2]}),R=persistent_term:put_new(key(1),{same,[1,2]}),{R,persistent_term:get(key(1))};
probe(put_new_different) -> persistent_term:put(key(1),first),persistent_term:put_new(key(1),second);
probe(exact_numeric_keys) ->
    K1={?MODULE,1},K2={?MODULE,1.0},
    try persistent_term:put(K1,integer),persistent_term:put(K2,float),
        {persistent_term:get(K1),persistent_term:get(K2),persistent_term:erase(K1),persistent_term:get(K2)}
    after persistent_term:erase(K1),persistent_term:erase(K2) end;
probe(writer_exit_retains) ->
    {P,M}=spawn_monitor(?MODULE,writer,[self(),key(1),{retained,#{a=>b}}]),
    receive {writer_done,P}->down(P,M),persistent_term:get(key(1)) end;
probe(get0_member) ->
    persistent_term:put(key(1),one),persistent_term:put(key(2),two),
    All=persistent_term:get(),
    has_pair(key(1),one,All) andalso has_pair(key(2),two,All);
probe(ets_lifecycle_select) ->
    ?MODULE=ets:new(?MODULE,[named_table,public,set]),
    true=is_reference(ets:whereis(?MODULE)),
    true=ets:insert(?MODULE,{key,1}),
    [{key,1}]=ets:lookup(?MODULE,key),
    true=ets:member(?MODULE,key),
    [{key,1}]=ets:select(?MODULE,[{{'$1','$2'},[],['$_']}]),
    true=ets:delete(?MODULE,key),
    []=ets:lookup(?MODULE,key),
    true=ets:delete(?MODULE),
    undefined=ets:whereis(?MODULE),
    ok.
writer(Parent,Key,Value) -> persistent_term:put(Key,Value),Parent!{writer_done,self()}.
down(P,M) -> receive {'DOWN',M,process,P,normal}->ok; {'DOWN',M,process,P,R}->erlang:error({peer_failed,R}) after 5000->erlang:error(peer_timeout) end.
has_pair(_,_,[])->false;
has_pair(K,V,[{K,V}|_])->true;
has_pair(K,V,[_|T])->has_pair(K,V,T).
