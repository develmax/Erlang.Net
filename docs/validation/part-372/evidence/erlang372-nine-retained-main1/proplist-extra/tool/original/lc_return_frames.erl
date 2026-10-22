-module(lc_return_frames).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [terminal_after_skip,may_return_after_skip,terminal_renamed,improper_identity,improper_effect,proper_identity,singleton_terminal,non_tail_terminal,dynamic_source,unknown_filter,named_fun_short,named_fun_long,anonymous_fun_short,anonymous_fun_long,named_fun_error,anonymous_fun_error,apply_badarity,apply_nontail,foldr_direct_error,foldr_short,foldr_long,foldr_catch].
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
probe(terminal_after_skip) -> [die(Item) || Item <- [skip,bad], Item =/= skip];
probe(may_return_after_skip) -> [die(Item) || Item <- [skip,ok,bad], Item =/= skip];
probe(terminal_renamed) -> [renamed(Item) || Item <- [skip,bad], Item =/= skip];
probe(improper_identity) -> [Item || Item <- [ok,ok,ok|tail]];
probe(improper_effect) -> [die(Item) || Item <- [ok,ok,bad|tail]];
probe(proper_identity) -> [Item || Item <- [ok,ok,ok]];
probe(singleton_terminal) -> [die(Item) || Item <- [bad]];
probe(non_tail_terminal) -> Value=[die(Item) || Item <- [skip,bad], Item =/= skip],{unreachable,Value};
probe(dynamic_source) -> dynamic([skip,bad]);
probe(unknown_filter) -> [die(Item) || Item <- [skip,bad], permit(Item)];
probe(named_fun_short) -> named_tail(3);
probe(named_fun_long) -> named_tail(4096);
probe(anonymous_fun_short) -> anonymous_tail(3);
probe(anonymous_fun_long) -> anonymous_tail(4096);
probe(named_fun_error) -> named_error(4096);
probe(anonymous_fun_error) -> anonymous_error(4096);
probe(apply_badarity) -> Function=fun identity/1,Function(a,b);
probe(apply_nontail) -> Function=fun named_error/1,Value=Function(3),{unreachable,Value};
probe(foldr_direct_error) -> lists:foldr(fun(_,_) -> erlang:error(fold_marker) end,seed,[a,b,c]);
probe(foldr_short) -> lists:foldr(fun(Element,Acc) -> [Element|Acc] end,[],[a,b,c]);
probe(foldr_long) -> lists:foldr(fun(Element,Acc) -> Element+Acc end,0,lists:duplicate(4096,1));
probe(foldr_catch) -> lists:foldr(fun(Element,Acc) -> try throw(Element) catch throw:Element -> [Element|Acc] end end,[],[a,b,c]).
dynamic(List) -> [die(Item) || Item <- List, Item =/= skip].
die(bad) -> erlang:error(return_marker);
die(Item) -> Item.
renamed(bad) -> erlang:error(return_marker);
renamed(Item) -> Item.
permit(skip) -> false;
permit(_) -> true.
named_tail(0) -> done;
named_tail(Count) -> Function=fun named_tail/1,Function(Count-1).
anonymous_tail(0) -> done;
anonymous_tail(Count) -> Function=fun(Next) -> anonymous_tail(Next) end,Function(Count-1).
named_error(0) -> erlang:error(tail_marker);
named_error(Count) -> Function=fun named_error/1,Function(Count-1).
anonymous_error(0) -> erlang:error(tail_marker);
anonymous_error(Count) -> Function=fun(Next) -> anonymous_error(Next) end,Function(Count-1).
identity(Value) -> Value.
