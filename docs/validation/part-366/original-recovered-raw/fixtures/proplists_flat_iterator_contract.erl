-module(proplists_flat_iterator_contract).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [flat_nonzero,flat_at_end,flat_outside,empty_zero,empty_outside,flat_negative].
run() -> [{Name,value(Name)} || Name <- cases()].
value(Name) -> try probe(Name) of Value -> {ok,Value} catch Class:Reason -> {error,Class,Reason} end.
run_stacks() ->
    Names=cases(),
    Default={default,[{Name,capture(Name)} || Name <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names) -> erlang:system_flag(backtrace_depth,Depth),{Depth,[{Name,capture(Name)} || Name <- Names]}.
capture(Name) -> try probe(Name) of Value -> {ok,Value} catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
probe(flat_nonzero) -> proplists:from_map([1|#{a=>1}]);
probe(flat_at_end) -> proplists:from_map([2|#{a=>1,b=>2}]);
probe(flat_outside) -> proplists:from_map([2|#{a=>1}]);
probe(empty_zero) -> proplists:from_map([0|#{}]);
probe(empty_outside) -> proplists:from_map([1|#{}]);
probe(flat_negative) -> proplists:from_map([-1|#{a=>1}]).
