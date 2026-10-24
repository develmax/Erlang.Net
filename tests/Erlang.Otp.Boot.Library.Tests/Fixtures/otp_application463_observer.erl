-module(otp_application463_observer).
-file("otp_application463_observer.erl", 1).
-export([worker/1, run/0]).

worker(Parent) ->
    Parent ! {finished, run()}.

run() ->
    [observe(counters_default, fun() ->
         C = counters:new(2, []),
         ok = counters:add(C, 1, 7),
         ok = counters:sub(C, 1, 2),
         ok = counters:put(C, 2, -9),
         {counters:get(C, 1), counters:get(C, 2), maps:get(size, counters:info(C))}
     end),
     observe(counters_concurrent, fun() ->
         C = counters:new(2, [write_concurrency]),
         ok = counters:put(C, 1, 9223372036854775807),
         ok = counters:add(C, 1, 1),
         {counters:get(C, 1), counters:get(C, 2), maps:get(size, counters:info(C))}
     end),
     observe(counters_bad_options, fun() -> counters:new(1, [invalid]) end),
     observe(counters_bad_size, fun() -> counters:new(-1, [write_concurrency]) end),
     observe(counters_bad_get, fun() -> counters:get(invalid, 1) end),
     observe(counters_bad_sub, fun() -> counters:sub(invalid, 1, 2) end),
     observe(counters_bad_info, fun() -> counters:info(invalid) end),
     observe(warnings_empty, fun() -> systools_lib:werror([], []) end),
     observe(warnings_as_errors, fun() -> systools_lib:werror([warnings_as_errors], [warning]) end),
     observe(warnings_ordinary, fun() -> systools_lib:werror([other], [warning]) end),
     observe(warnings_bad_options, fun() -> systools_lib:werror(invalid, [warning]) end),
     observe(assq_found, fun() -> misc_supp:assq(a, [{a, 7}, {b, 9}]) end),
     observe(assq_missing, fun() -> misc_supp:assq(c, [{a, 7}]) end),
     observe(passq_found, fun() -> misc_supp:passq(a, [{a, 7}]) end),
     observe(passq_missing, fun() -> misc_supp:passq(c, [{a, 7}]) end),
     observe(string_empty, fun() -> misc_supp:is_string([]) end),
     observe(string_printable, fun() -> misc_supp:is_string("hello") end),
     observe(string_control, fun() -> misc_supp:is_string([10]) end),
     observe(string_improper, fun() -> misc_supp:is_string([65 | invalid]) end)].

observe(Name, Function) ->
    try Function() of
        Value -> {Name, {returned, Value}}
    catch
        Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
