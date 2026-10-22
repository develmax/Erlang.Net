-module(beam_os_subject399).
-export([worker/2,probe/1]).
worker(Parent,Case) ->
    cleanup(),
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack}
            after cleanup() end,
    Parent!{Case,Outcome},ok.
cleanup() -> os:unsetenv("ERLANG399_TEST_A"),os:unsetenv("ERLANG399_TEST_B"),os:unsetenv("ERLANG399_TEST_UNICODE"),ok.
probe(getenv_seed) -> os:getenv("ERLANG399_SEED");
probe(getenv_missing) -> os:getenv("ERLANG399_TEST_A");
probe(put_get_unset) -> A=os:putenv("ERLANG399_TEST_A","first"),V=os:getenv("ERLANG399_TEST_A"),B=os:unsetenv("ERLANG399_TEST_A"),{A,V,B,os:getenv("ERLANG399_TEST_A")};
probe(put_replace) -> os:putenv("ERLANG399_TEST_A","first"),R=os:putenv("ERLANG399_TEST_A","second"),{R,os:getenv("ERLANG399_TEST_A")};
probe(unset_missing) -> os:unsetenv("ERLANG399_TEST_A");
probe(empty_value) -> R=os:putenv("ERLANG399_TEST_A",[]),{R,os:getenv("ERLANG399_TEST_A")};
probe(binary_key_value) -> R=os:putenv(<<"ERLANG399_TEST_A">>,<<"binary_value">>),{R,os:getenv("ERLANG399_TEST_A")};
probe(nested_key_value) -> R=os:putenv(["ERLANG399_","TEST_A"],["nested_",["value"]]),{R,os:getenv("ERLANG399_TEST_A")};
probe(unicode_value) -> R=os:putenv("ERLANG399_TEST_UNICODE",[16#3BB,16#1F642]),{R,os:getenv("ERLANG399_TEST_UNICODE")};
probe(case_keys) ->
    try os:putenv("ERLANG399_TEST_A","upper"),{os:getenv("erlang399_test_a"),os:getenv("ERLANG399_TEST_A")}
    after os:unsetenv("erlang399_test_a") end;
probe(getenv_atom) -> os:getenv(bad_name);
probe(getenv_nul) -> os:getenv([$A,0,$B]);
probe(getenv_empty) -> os:getenv([]);
probe(getenv_equals) -> os:getenv("A=B");
probe(putenv_atom_value) -> os:putenv("ERLANG399_TEST_A",bad_value);
probe(putenv_nul_value) -> os:putenv("ERLANG399_TEST_A",[$A,0,$B]);
probe(unsetenv_atom) -> os:unsetenv(bad_name);
probe(env_raw) -> os:env();
probe(env_own_state) ->
    os:putenv("ERLANG399_TEST_A","owned_value"),All=os:env(),
    {has_pair("ERLANG399_TEST_A","owned_value",All),All};
probe(getpid_raw) -> os:getpid();
probe(getpid_own_state) -> P=os:getpid(),{digits(P),P};
probe(perf_counter_raw) -> os:perf_counter();
probe(system_time_raw) -> os:system_time();
probe(system_time_seconds_raw) -> os:system_time(second);
probe(system_time_unit_zero) -> os:system_time(0);
probe(system_time_unit_bad) -> os:system_time(bad_unit);
probe(timestamp_raw) -> os:timestamp();
probe(timestamp_own_state) -> T={Mega,Sec,Micro}=os:timestamp(),{is_integer(Mega) andalso is_integer(Sec) andalso is_integer(Micro) andalso Sec>=0 andalso Sec<1000000 andalso Micro>=0 andalso Micro<1000000,T};
probe(signal_bad_option) -> os:set_signal(not_signal,bad_option);
probe(signal_nonatom) -> os:set_signal(17,ignore);
probe(signal_unknown) -> os:set_signal(not_signal,default);
probe(signal_ignore) -> try os:set_signal(sigusr1,ignore) after catch os:set_signal(sigusr1,default) end;
probe(signal_handle) -> try os:set_signal(sigusr1,handle) after catch os:set_signal(sigusr1,default) end;
probe(utf16_pair) -> R=os:putenv("ERLANG399_TEST_UNICODE",[16#D83D,16#DE42]),{R,os:getenv("ERLANG399_TEST_UNICODE")};
probe(utf16_lone) -> R=os:putenv("ERLANG399_TEST_UNICODE",[16#D800]),{R,os:getenv("ERLANG399_TEST_UNICODE")};
probe(empty_key_roundtrip) -> try R=os:putenv([],"empty_key"),{R,os:getenv([])} after os:unsetenv([]) end;
probe(tree_ascending) -> fixed_tree(["A","B","C","D","E","F","G"],[]);
probe(tree_descending) -> fixed_tree(["G","F","E","D","C","B","A"],[]);
probe(tree_delete_successor) -> fixed_tree(["D","B","F","A","C","E","G"],["D","F","B"]);
probe(tree_case_replace) -> fixed_tree(["D","B","F","A","C","E","G","b","d"],["F","A"]).
fixed_tree(Keys,Deletes) ->
    Saved=os:env(),
    try clear_pairs(Saved),put_keys(Keys),Before=os:env(),delete_keys(Deletes),{Before,os:env()}
    after clear_pairs(os:env()),restore_pairs(Saved) end.
clear_pairs([])->ok;
clear_pairs([{K,_}|T])->os:unsetenv(K),clear_pairs(T).
restore_pairs([])->ok;
restore_pairs([{K,V}|T])->os:putenv(K,V),restore_pairs(T).
put_keys([])->ok;
put_keys([K|T])->os:putenv(K,["v_",K]),put_keys(T).
delete_keys([])->ok;
delete_keys([K|T])->os:unsetenv(K),delete_keys(T).
has_pair(_,_,[])->false;
has_pair(K,V,[{K,V}|_])->true;
has_pair(K,V,[_|T])->has_pair(K,V,T).
digits([])->false;
digits([H|T]) when H >= $0,H =< $9 -> digits_tail(T);
digits(_)->false.
digits_tail([])->true;
digits_tail([H|T]) when H >= $0,H =< $9 -> digits_tail(T);
digits_tail(_)->false.
