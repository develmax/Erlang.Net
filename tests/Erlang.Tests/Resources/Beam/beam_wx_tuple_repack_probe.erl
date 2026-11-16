-module(beam_wx_tuple_repack_probe).
-export([options/1, set/2, range/3]).
-record(wx_ref, {ref, type, state}).
-record(wx_env, {ref, sv, debug}).
-define(ENV, case erlang:get(wx_env) of
    #wx_env{ref=Ref} -> Ref;
    _ -> erlang:error({wx, unknown_env})
end).

options(Options) when is_list(Options) ->
    Mapper = fun({date, {{Year,Month,Day},{Hour,Minute,Second}}}) ->
                     {date,{Day,Month,Year,Hour,Minute,Second}};
                ({position, {_,_}} = Arg) -> Arg;
                ({style, _} = Arg) -> Arg;
                (Bad) -> erlang:error({badoption, Bad}) end,
    Mapped = lists:map(Mapper, Options),
    wxe_util:queue_cmd(Mapped, ?ENV, 1505),
    wxe_util:rec(1505).

set(#wx_ref{type=Type}=This, {{Year,Month,Day},{Hour,Minute,Second}})
  when is_integer(Day), is_integer(Month), is_integer(Year),
       is_integer(Hour), is_integer(Minute), is_integer(Second) ->
    (Type =:= wxCalendarCtrl) orelse Type:parent_class(wxCalendarCtrl),
    wxe_util:queue_cmd(This, {Day,Month,Year,Hour,Minute,Second}, ?ENV, 1506),
    wxe_util:rec(1506).

range(#wx_ref{type=Type}=This, {{Year1,Month1,Day1},{Hour1,Minute1,Second1}},
      {{Year2,Month2,Day2},{Hour2,Minute2,Second2}})
  when is_integer(Day1), is_integer(Month1), is_integer(Year1),
       is_integer(Hour1), is_integer(Minute1), is_integer(Second1),
       is_integer(Day2), is_integer(Month2), is_integer(Year2),
       is_integer(Hour2), is_integer(Minute2), is_integer(Second2) ->
    (Type =:= wxCalendarCtrl) orelse Type:parent_class(wxCalendarCtrl),
    wxe_util:queue_cmd(This, {Day1,Month1,Year1,Hour1,Minute1,Second1},
                       {Day2,Month2,Year2,Hour2,Minute2,Second2}, ?ENV, 1507).
