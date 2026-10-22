-module(beam_try_fixture378).
-export([capture/2, cases/0]).

cases() ->
    [normal_body, error_class, throw_class, exit_class,
     unmatched_class, nested_lifo, handler_throws, try_of_mismatch,
     after_normal, after_failure, after_replaces, explicit_raise,
     reraised, call_unwind, tail_unwind, saved_trace, deep_unwind].

capture(Owner, Case) ->
    Outcome = try run(Case) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end,
    Owner ! {Case, Outcome},
    ok.

run(normal_body) -> protected(body(normal));
run(error_class) -> protected(body(error));
run(throw_class) -> protected(body(throw));
run(exit_class) -> protected(body(exit));
run(unmatched_class) -> unmatched(body(error));
run(nested_lifo) -> nested(body(throw));
run(handler_throws) -> handler_throws(body(error));
run(try_of_mismatch) -> of_mismatch(body(normal));
run(after_normal) -> after_count(body(normal));
run(after_failure) -> after_count(body(error));
run(after_replaces) -> after_replaces(body(error));
run(explicit_raise) -> explicit_raise(body(error));
run(reraised) -> reraised(body(error));
run(call_unwind) -> protected_local(body(error));
run(tail_unwind) -> protected_tail(body(error));
run(saved_trace) -> saved_trace(body(error));
run(deep_unwind) -> protected(fun() -> descend_depth(8) end).

body(Kind) ->
    fun() -> body_value(Kind) end.

body_value(normal) -> {body_value, 378};
body_value(error) -> erlang:error(body_error);
body_value(throw) -> erlang:throw(body_throw);
body_value(exit) -> erlang:exit(body_exit).

protected(Fun) ->
    try Fun() of
        Value -> {normal, Value}
    catch
        Class:Reason:Stack -> {protected_caught, Class, Reason, Stack}
    end.

unmatched(Fun) ->
    try
        try Fun() catch throw:body_throw -> inner_caught end
    catch
        Class:Reason:Stack -> {outer_caught, Class, Reason, Stack}
    end.

nested(Fun) ->
    try
        First = try Fun() catch throw:body_throw -> inner_caught end,
        erlang:error({outer_after_inner, First})
    catch
        Class:Reason:Stack -> {outer_caught, Class, Reason, Stack}
    end.

handler_throws(Fun) ->
    try
        try Fun() catch error:body_error -> erlang:throw(handler_throw) end
    catch
        Class:Reason:Stack -> {outer_caught, Class, Reason, Stack}
    end.

of_mismatch(Fun) ->
    try Fun() of
        expected -> matched
    catch
        Class:Reason:Stack -> {wrong_inner_catch, Class, Reason, Stack}
    end.

after_count(Fun) ->
    put(after_count, 0),
    try
        Value = try Fun() after increment_after() end,
        {normal, Value, erase(after_count)}
    catch
        Class:Reason:Stack -> {after_caught, Class, Reason, Stack, erase(after_count)}
    end.

increment_after() ->
    put(after_count, get(after_count) + 1),
    ok.

after_replaces(Fun) ->
    try Fun() after erlang:error(after_replacement) end.

explicit_raise(Fun) ->
    try Fun() catch
        Class:Reason:_ ->
            erlang:raise(Class, Reason,
                         [{literal_raise_module, literal_raise_function, 0,
                           [{file, "literal_raise.erl"}, {line, 378}]}])
    end.

reraised(Fun) ->
    try Fun() catch
        Class:Reason:Stack -> erlang:raise(Class, Reason, Stack)
    end.

protected_local(Fun) ->
    try descend(Fun) catch
        Class:Reason:Stack -> {local_caught, Class, Reason, Stack}
    end.

descend(Fun) ->
    Value = Fun(),
    {returned, Value}.

protected_tail(Fun) ->
    try tail_descend(Fun) catch
        Class:Reason:Stack -> {tail_caught, Class, Reason, Stack}
    end.

tail_descend(Fun) ->
    Fun().

saved_trace(Fun) ->
    try Fun() catch
        Class:Reason:Stack ->
            Marker = keep_marker(retained),
            {saved, Marker, Class, Reason, Stack}
    end.

keep_marker(Value) ->
    {marker, Value}.

descend_depth(0) -> erlang:error(deep_body_error);
descend_depth(Depth) ->
    Value = descend_depth(Depth - 1),
    {returned, Value}.
