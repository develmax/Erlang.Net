-module(beam_entry_tagged_projection_probe).
-export([read/2, project/1, forward/1, reverse/1, dispatch/1, retained/2, staged/2]).
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
forward(Input) ->
    case identity(Input) of
        #entry{key=K,value=V} -> pair(K,V);
        _ -> missing
    end.
reverse(Input) ->
    case identity(Input) of
        #entry{key=K,value=V} -> pair(V,K);
        _ -> missing
    end.
dispatch(Input) ->
    case identity(Input) of
        {left,K,V} -> pair(K,V);
        {right,K,V} -> pair(V,K);
        _ -> missing
    end.
retained(Input, Saved) ->
    case identity(Input) of
        #entry{value=V} -> pair(Saved,V);
        Other -> Other
    end.
staged(Input, Saved) ->
    remember(identity(Saved)),
    case identity(Input) of
        #entry{value=V} -> pair(Saved,V);
        Other -> Other
    end.
remember(Value) -> erlang:put(probe_value, Value).
fetch(entry, Value) -> #entry{key=entry, value=Value};
fetch(_, _) -> missing.
identity(Value) -> Value.
pair(First, Second) -> {First,Second}.

