-module(ets).
-export([new/2, whereis/1, insert/2, lookup/2, member/2, delete/1, delete/2, select/2, lookup_element/3, lookup_element/4, match/2, internal_select_delete/2, internal_delete_all/2, select_delete/2, match_delete/2]).

-file("ets.erl", 1413).
new(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2248).
whereis(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 734).
insert(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 891).
lookup(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1211).
member(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 340).
delete(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 363).
delete(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1725).
select(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 927).
lookup_element(_, _, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 963).
lookup_element(_, _, _, _) ->
  erlang:nif_error(undef).

-file("ets.erl", 1002).
match(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1859).
internal_select_delete(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 394).
internal_delete_all(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1848).
select_delete(Table, [{'_',[],[true]}]) ->
    ets:internal_delete_all(Table, undefined);
select_delete(Table, MatchSpec) ->
    ets:internal_select_delete(Table, MatchSpec).

-file("ets.erl", 2760).
match_delete(Table, Pattern) ->
    ets:select_delete(Table, [{Pattern,[],[true]}]),
    true.
