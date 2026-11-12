-module(beam_entry_tagged_projection_probe).
-export([read/2, project/1]).
-record(entry, {key, value}).
read(Key, Value) ->
    case fetch(Key, Value) of
        #entry{value=V} when is_list(V) -> list_to_atom(V);
        #entry{value=V} -> V;
        _ -> missing
    end.
project(Input) ->
    case identity(Input) of
        #entry{value=V} -> V;
        _ -> missing
    end.
fetch(entry, Value) -> #entry{key=entry, value=Value};
fetch(_, _) -> missing.
identity(Value) -> Value.

