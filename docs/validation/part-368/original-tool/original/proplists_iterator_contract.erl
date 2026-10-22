-module(proplists_iterator_contract).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [none,literal_next,ordered,reversed,wrapped_iterator,malformed_atom,malformed_tail,malformed_keys].
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
probe(none) -> proplists:from_map(none);
probe(literal_next) -> proplists:from_map({k,v,none});
probe(ordered) -> proplists:from_map(maps:iterator(#{1=>integer,1.0=>float,a=>atom},ordered));
probe(reversed) -> proplists:from_map(maps:iterator(#{1=>integer,1.0=>float,a=>atom},reversed));
probe(wrapped_iterator) -> proplists:from_map({prefix,item,maps:iterator(#{a=>1,b=>2},ordered)});
probe(malformed_atom) -> proplists:from_map(invalid);
probe(malformed_tail) -> proplists:from_map({k,v,invalid});
probe(malformed_keys) -> proplists:from_map([[missing]|#{a=>1}]).
