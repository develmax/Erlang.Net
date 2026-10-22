%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 1996-2026. All Rights Reserved.
%%
%% Licensed under the Apache License, Version 2.0 (the "License");
%% you may not use this file except in compliance with the License.
%% You may obtain a copy of the License at
%%
%%     http://www.apache.org/licenses/LICENSE-2.0
%%
%% Unless required by applicable law or agreed to in writing, software
%% distributed under the License is distributed on an "AS IS" BASIS,
%% WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
%% See the License for the specific language governing permissions and
%% limitations under the License.
%%
%% %CopyrightEnd%
%%
%% Explicit pinned-source partial ETS bootstrap facet, not unchanged full module.
-module(ets).
-export([new/2,whereis/1,insert/2,lookup/2,member/2,delete/1,delete/2,select/2,lookup_element/3,lookup_element/4,match/2,internal_select_delete/2,internal_delete_all/2,select_delete/2,match_delete/2,match_object/2,select_count/2,select_reverse/2,tab2list/1,insert_new/2,take/2,delete_object/2,update_element/3,update_element/4,first/1,last/1,next/2,prev/2,first_lookup/1,last_lookup/1,next_lookup/2,prev_lookup/2,update_counter/3,update_counter/4]).

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

-file("ets.erl", 1092).
match_object(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1814).
select_count(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1935).
select_reverse(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2771).
tab2list(T) ->
    ets:match_object(T, '_').

-file("ets.erl", 761).
insert_new(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2074).
take(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 418).
delete_object(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2172).
update_element(_, _, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2224).
update_element(_, _, _, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 443).
first(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 823).
last(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 1444).
next(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 1495).
prev(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 469).
first_lookup(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 849).
last_lookup(_) ->
    erlang:nif_error(undef).

-file("ets.erl", 1470).
next_lookup(_, _) ->
  erlang:nif_error(undef).

-file("ets.erl", 1521).
prev_lookup(_, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2088).
update_counter(_, _, _) ->
    erlang:nif_error(undef).

-file("ets.erl", 2161).
update_counter(_, _, _, _) ->
    erlang:nif_error(undef).

