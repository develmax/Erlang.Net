-module(beam_closure_result_stores).
-export([classify/1, keep_fun/2]).

classify(Name) when is_atom(Name) ->
    lists:all(fun($-) -> true;
                 (D) when $0 =< D, D =< $9 -> true;
                 (UC) when $A =< UC, UC =< $Z -> true;
                 (_) -> false
              end, atom_to_list(Name)).

keep_fun(Flag, Values) ->
    Checker = fun(Value) -> Value =:= Flag end,
    put(checker, Flag),
    lists:any(Checker, Values).
