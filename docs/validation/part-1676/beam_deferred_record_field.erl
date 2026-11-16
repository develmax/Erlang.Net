-module(beam_deferred_record_field).
-export([run/1, consume/1]).
-record(env,{unused,field,other}).
run(#env{field=Field}=Env) ->
    put(env,Env),
    ?MODULE:consume(Field),
    ok.
consume(Field) -> put(field,Field).
