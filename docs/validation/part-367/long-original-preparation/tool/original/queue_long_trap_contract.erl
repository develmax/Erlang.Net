-module(queue_long_trap_contract).
-export([reverse_cases/0,reverse_run/0,reverse_stacks/0,
         to_list_cases/0,to_list_run/0,to_list_stacks/0,
         out_cases/0,out_run/0,out_stacks/0,
         control_cases/0,control_run/0,control_stacks/0]).

reverse_cases() -> [reverse_64017,reverse_256017].
to_list_cases() -> [to_list_rear_64017,to_list_rear_256017].
out_cases() -> [out_rear_64017,out_rear_256017].
control_cases() -> [short_reverse_direct,short_reverse_rear,
                    long_append_64017,long_length_64017,long_from_list_64017].

reverse_run() -> run_names(reverse_cases()).
to_list_run() -> run_names(to_list_cases()).
out_run() -> run_names(out_cases()).
control_run() -> run_names(control_cases()).
reverse_stacks() -> stack_names(reverse_cases()).
to_list_stacks() -> stack_names(to_list_cases()).
out_stacks() -> stack_names(out_cases()).
control_stacks() -> stack_names(control_cases()).

run_names(Names) -> [{Name,value(Name)} || Name <- Names].
value(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason -> {error,Class,Reason} end.

stack_names(Names) ->
    Default = {default,[{Name,capture(Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Name)} || Name <- Names]}.
capture(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.

improper(Count) -> lists:seq(1,Count) ++ tail.

probe(reverse_64017) -> lists:reverse(improper(64017),[]);
probe(reverse_256017) -> lists:reverse(improper(256017),[]);
probe(to_list_rear_64017) -> queue:to_list({improper(64017),[]});
probe(to_list_rear_256017) -> queue:to_list({improper(256017),[]});
probe(out_rear_64017) -> queue:out({[head|improper(64017)],[]});
probe(out_rear_256017) -> queue:out({[head|improper(256017)],[]});
probe(short_reverse_direct) -> lists:reverse([1,2,3|tail],[]);
probe(short_reverse_rear) -> queue:to_list({[1,2,3|tail],[]});
probe(long_append_64017) -> queue:to_list({[],improper(64017)});
probe(long_length_64017) -> queue:len({improper(64017),[]});
probe(long_from_list_64017) -> queue:from_list(improper(64017)).
