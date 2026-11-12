-module(beam_general_catch_probe).
-export([run/1, select/4, status/3, lookup/3, pick/2]).
run({M,F,X}) -> catch M:F(X).
select(M,F,X,Saved) ->
    case catch M:F(X) of
        4 -> Saved;
        _ -> failed
    end.
status(Key, Entries, Saved) ->
    case lists:member(Key, Entries) of
        true -> Saved;
        Other -> Other
    end.
lookup(Key, Entries, Saved) ->
    case lists:keyfind(Key, 1, Entries) of
        Value when is_tuple(Value) -> pair(Saved, Value);
        Other -> Other
    end.
pick(Entries, Saved) ->
    case proplists:get_value(key, Entries, missing) of
        c -> pair(Saved,c);
        a -> Saved;
        b -> skipped;
        Other -> Other
    end.
pair(A, B) -> {A,B}.

