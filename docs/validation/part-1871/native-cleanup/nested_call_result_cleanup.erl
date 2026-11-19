-module(nested_call_result_cleanup).
-export([run/2, direct/2, caught/2]).
run(A, B) ->
    Keys = [one, two],
    case nested_cleanup_dep:read(A, Keys) of
        {found, Value} ->
            case nested_cleanup_dep:write(B, Value) of
                accept -> {saved, B};
                Inner ->
                    dispose_inner(B), Inner
            end;
        Outer ->
            dispose_outer(B), Outer
    end.
direct(A, B) ->
    case nested_cleanup_dep:read(A, keys) of
        {found, Value} ->
            case nested_cleanup_dep:write(B, Value) of
                accept -> {saved, B};
                Inner ->
                    dispose_outer(B), Inner
            end;
        Outer ->
            dispose_outer(B), Outer
    end.
caught(A, B) ->
    case nested_cleanup_dep:read(A, keys) of
        {found, Value} ->
            case nested_cleanup_dep:write(B, Value) of
                accept -> {saved, B};
                Inner ->
                    (catch dispose_inner(B)), Inner
            end;
        Outer ->
            dispose_outer(B), Outer
    end.
dispose_inner(crash) -> error(cleanup_failed);
dispose_inner(X) -> put(last_cleanup, {inner,X}).
dispose_outer(X) -> put(last_cleanup, {outer,X}).
