-module(native_record_physical).
-export([run/1,wait/1]).
-record #r{z,a}.
-export_record([r]).
run(N) ->
    R = #r{z=N,a = <<1,2,3>>},
    true = erlang:garbage_collect(),
    native_record_physical_probe:check(1),
    Z = R#r.z,
    R2 = R#r{z=N+1},
    true = erlang:garbage_collect(),
    native_record_physical_probe:check(2),
    {R,R2,Z}.
wait(N) ->
    R = #r{z=N,a = <<1,2,3>>},
    native_record_physical_probe:check(3),
    receive stop -> R end.
