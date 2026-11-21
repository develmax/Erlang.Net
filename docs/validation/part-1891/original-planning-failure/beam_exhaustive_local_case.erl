-module(beam_exhaustive_local_case).
-export([covered/2, unknown/2, bound/2]).
covered(Key, Values) ->
    case erlang:phash2(Key) of
        0 -> skipped;
        _ ->
            case tagged(Values) of
                {yes, Value} -> {found, Value};
                no -> missing
            end
    end.
unknown(Key, Values) ->
    case erlang:phash2(Key) of
        0 -> skipped;
        _ ->
            case loose(Values) of
                {yes, Value} -> {found, Value};
                no -> missing
            end
    end.
bound(Key, Values) ->
    case erlang:phash2(Key) of
        0 -> skipped;
        _ ->
            case tagged(Values) of
                {yes, Key} -> found;
                no -> missing
            end
    end.
tagged([]) -> no;
tagged([{yes, Data}|_]) -> {yes, Data};
tagged([_|Rest]) -> tagged(Rest).
loose([]) -> no;
loose([{yes, Data}|_]) -> {yes, Data};
loose([_|Rest]) -> loose(Rest);
loose(Value) -> Value.
