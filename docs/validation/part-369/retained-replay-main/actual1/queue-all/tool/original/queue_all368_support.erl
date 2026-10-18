-module(queue_all368_support).
-export([run/2,profiles/2,bad_list/1,caller/3,logged/3,logged_fold/3,invoke/2,
         cb_zero/0,cb_true/1,cb_false/1,cb_fold/2,
         cb_select/1,cb_replace/1,cb_empty/1,cb_one/1,cb_many/1,cb_improper/1,
         cb_bad/1,cb_throw/1,cb_error/1,cb_exit/1,cb_error_later/1,cb_badfun/1,
         cb_is_x/1,cb_fold_throw/2,cb_fold_error/2,cb_fold_exit/2]).
run(Module,Names) -> [{Name,value(Module,Name)} || Name <- Names].
value(Module,Name) ->
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value}
    catch Class:Reason -> {error,Class,Reason} end.
profiles(Module,Names) ->
    Default = {default,[{Name,capture(Module,Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Module,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Module,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Module,Name)} || Name <- Names]}.
capture(Module,Name) ->
    try erlang:apply(Module,probe,[Name]) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
bad_list(Count) -> lists:seq(1,Count) ++ tail.
caller(Module,Function,Arguments) ->
    Result = caller_b(Module,Function,Arguments), {caller_return,Result}.
caller_b(Module,Function,Arguments) ->
    Result = caller_c(Module,Function,Arguments), {caller_b_return,Result}.
caller_c(Module,Function,Arguments) ->
    Result = erlang:apply(Module,Function,Arguments), {caller_c_return,Result}.
logged(Operation,Callback,Queue) ->
    put(queue_all368_trace,[]),
    try Result = erlang:apply(queue,Operation,[Callback,Queue]),
        {Result,lists:reverse(get(queue_all368_trace))}
    after erase(queue_all368_trace) end.
logged_fold(Callback,Accumulator,Queue) ->
    put(queue_all368_trace,[]),
    try Result = queue:fold(Callback,Accumulator,Queue),
        {Result,lists:reverse(get(queue_all368_trace))}
    after erase(queue_all368_trace) end.
note(Value) ->
    case get(queue_all368_trace) of
        undefined -> put(queue_all368_trace,[Value]);
        Values -> put(queue_all368_trace,[Value|Values])
    end.
invoke(Function,Arguments) -> erlang:apply(Function,Arguments).
invoke_one(Function,Value) -> Function(Value).
cb_zero() -> zero.
cb_true(Value) -> note(Value),true.
cb_false(Value) -> note(Value),false.
cb_select(Value) -> note(Value),Value rem 2 =:= 0.
cb_replace(Value) -> note(Value),{true,{mapped,Value}}.
cb_empty(Value) -> note(Value),[].
cb_one(Value) -> note(Value),[{mapped,Value}].
cb_many(Value) -> note(Value),[Value,{mapped,Value}].
cb_improper(Value) -> note(Value),[Value|callback_tail].
cb_bad(Value) -> note(Value),not_boolean.
cb_throw(Value) -> note(Value),throw({callback_throw,Value}).
cb_error(Value) -> note(Value),erlang:error({callback_error,Value}).
cb_exit(Value) -> note(Value),exit({callback_exit,Value}).
cb_error_later(Value) -> note(Value),case Value of 3 -> erlang:error({callback_later,Value}); _ -> true end.
cb_badfun(Value) -> note(Value),invoke_one(Value,Value).
cb_is_x(Value) -> note(Value),Value =:= x.
cb_fold(Value,Accumulator) -> note(Value),[Value|Accumulator].
cb_fold_throw(Value,_Accumulator) -> note(Value),throw({callback_throw,Value}).
cb_fold_error(Value,_Accumulator) -> note(Value),erlang:error({callback_error,Value}).
cb_fold_exit(Value,_Accumulator) -> note(Value),exit({callback_exit,Value}).
