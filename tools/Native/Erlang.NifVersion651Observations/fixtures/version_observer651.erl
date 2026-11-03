-module(version_observer651).
-export([run/1,entry/2]).
entry(Path, Parent) ->
    Result = run(Path),
    Parent ! {version651,Result},
    Result.
run(Path) ->
    Load = outcome(fun() -> nif_version651:load(Path) end),
    First = outcome(fun() -> nif_version651:identity() end),
    Second = outcome(fun() -> nif_version651:second({payload,[17,<<1,2,3>>]}) end),
    {Load,First,Second}.
outcome(Fun) ->
    try Fun() of Value -> {value,Value}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.
