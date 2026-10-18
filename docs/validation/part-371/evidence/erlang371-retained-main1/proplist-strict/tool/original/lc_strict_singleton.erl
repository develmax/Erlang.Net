-module(lc_strict_singleton).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [relaxed_variable,strict_variable,strict_match,strict_mismatch,strict_improper,strict_body_error,after_strict,unknown_singleton,strict_renamed].
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
probe(relaxed_variable) -> [Item || Item <- [ok]];
probe(strict_variable) -> [Item || Item <:- [ok]];
probe(strict_match) -> [matched || wanted <:- [wanted]];
probe(strict_mismatch) -> [matched || wanted <:- [other]];
probe(strict_improper) -> [Item || Item <:- [ok|tail]];
probe(strict_body_error) -> [die(Item) || Item <:- [bad]];
probe(after_strict) -> Value=[Item || Item <:- [ok]],{Value,[die(Next) || Next <- [ok,bad]]};
probe(unknown_singleton) -> [Item || Item <:- source()];
probe(strict_renamed) -> renamed().
renamed() -> [die(Other) || Other <:- [bad]].
source() -> [ok].
die(bad) -> erlang:error(strict_marker);
die(Item) -> Item.
