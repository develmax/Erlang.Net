-module(ets).
-export([new/2,whereis/1,insert/2,lookup/2,member/2,delete/1,delete/2,select/2,lookup_element/3,lookup_element/4,match/2,internal_select_delete/2,internal_delete_all/2,select_delete/2,match_delete/2,match_object/2,select_count/2,select_reverse/2,tab2list/1,insert_new/2,take/2,delete_object/2,update_element/3,update_element/4,first/1,last/1,next/2,prev/2,first_lookup/1,last_lookup/1,next_lookup/2,prev_lookup/2,update_counter/3,update_counter/4,rename/2,setopts/2,give_away/3,is_compiled_ms/1,match_spec_compile/1,match_spec_run_r/3,internal_request_all/0,all/0,safe_fixtable/2,slot/2,match/1,match/3,match_object/1,match_object/3,select/1,select/3,select_reverse/1,select_reverse/3]).

new(_, _) ->
    erlang:nif_error(undef).

whereis(_) ->
    erlang:nif_error(undef).

insert(_, _) ->
    erlang:nif_error(undef).

lookup(_, _) ->
    erlang:nif_error(undef).

member(_, _) ->
    erlang:nif_error(undef).

delete(_) ->
    erlang:nif_error(undef).

delete(_, _) ->
    erlang:nif_error(undef).

select(_, _) ->
    erlang:nif_error(undef).

lookup_element(_, _, _) ->
    erlang:nif_error(undef).

lookup_element(_, _, _, _) ->
  erlang:nif_error(undef).

match(_, _) ->
    erlang:nif_error(undef).

internal_select_delete(_, _) ->
    erlang:nif_error(undef).

internal_delete_all(_, _) ->
    erlang:nif_error(undef).

select_delete(Table, [{'_',[],[true]}]) ->
    ets:internal_delete_all(Table, undefined);
select_delete(Table, MatchSpec) ->
    ets:internal_select_delete(Table, MatchSpec).

match_delete(Table, Pattern) ->
    ets:select_delete(Table, [{Pattern,[],[true]}]),
    true.

match_object(_, _) ->
    erlang:nif_error(undef).

select_count(_, _) ->
    erlang:nif_error(undef).

select_reverse(_, _) ->
    erlang:nif_error(undef).

tab2list(T) ->
    ets:match_object(T, '_').

insert_new(_, _) ->
    erlang:nif_error(undef).

take(_, _) ->
    erlang:nif_error(undef).

delete_object(_, _) ->
    erlang:nif_error(undef).

update_element(_, _, _) ->
    erlang:nif_error(undef).

update_element(_, _, _, _) ->
    erlang:nif_error(undef).

first(_) ->
    erlang:nif_error(undef).

last(_) ->
    erlang:nif_error(undef).

next(_, _) ->
    erlang:nif_error(undef).

prev(_, _) ->
    erlang:nif_error(undef).

first_lookup(_) ->
    erlang:nif_error(undef).

last_lookup(_) ->
    erlang:nif_error(undef).

next_lookup(_, _) ->
  erlang:nif_error(undef).

prev_lookup(_, _) ->
    erlang:nif_error(undef).

update_counter(_, _, _) ->
    erlang:nif_error(undef).

update_counter(_, _, _, _) ->
    erlang:nif_error(undef).

rename(_, _) ->
    erlang:nif_error(undef).

setopts(_, _) ->
    erlang:nif_error(undef).

give_away(_, _, _) ->
    erlang:nif_error(undef).

is_compiled_ms(_) ->
    erlang:nif_error(undef).

match_spec_compile(_) ->
    erlang:nif_error(undef).

match_spec_run_r(_, _, _) ->
    erlang:nif_error(undef).

internal_request_all() ->
    erlang:nif_error(undef).

all() ->
    receive_all(ets:internal_request_all(),
                erlang:system_info(schedulers),
                []).

receive_all(_Ref, 0, All) ->
    All;
receive_all(Ref, N, All) ->
    receive
        {Ref, SchedAll} ->
            receive_all(Ref, N-1, SchedAll ++ All)
    end.


safe_fixtable(_, _) ->
    erlang:nif_error(undef).

slot(_, _) ->
    erlang:nif_error(undef).

match(_) ->
    erlang:nif_error(undef).

match(_, _, _) ->
    erlang:nif_error(undef).

match_object(_) ->
    erlang:nif_error(undef).

match_object(_, _, _) ->
    erlang:nif_error(undef).

select(_) ->
    erlang:nif_error(undef).

select(_, _, _) ->
    erlang:nif_error(undef).

select_reverse(_) ->
    erlang:nif_error(undef).

select_reverse(_, _, _) ->
    erlang:nif_error(undef).
