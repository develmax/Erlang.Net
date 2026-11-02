-module(record_field331_observer).
-export([run/0,remote/1,unknown/1,guarded/1]).
-import_record(record_defaults313_owner,[r]).
remote(R) -> R#r.payload.
unknown(R) -> R#r.missing331.
guarded(R) when R#r.payload =/= not_a_record331 -> matched331;
guarded(_) -> missed331.
run() ->
    [{local_bad,capture(fun() -> record_defaults313_owner:payload(not_a_record331) end)},
     {remote_bad,capture(fun() -> remote(not_a_record331) end)},
     {missing,capture(fun() -> unknown(record_defaults313_owner:make()) end)},
     {guard_bad,guarded(not_a_record331)},
     {guard_good,guarded(record_defaults313_owner:make())}].
capture(Fun) ->
    try Fun() of Value -> {ok,Value}
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    end.
