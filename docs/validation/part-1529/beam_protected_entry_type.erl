-module(beam_protected_entry_type).
-export([integer_key/2, list_key/2, mixed_key/2]).

integer_key(Key, Rows) when is_integer(Key) ->
    try find_integer(Key, Rows)
    catch error:badarg -> invalid
    end.

list_key(Key, Rows) when is_list(Key) ->
    try erlang:length(Rows) of
        _ -> find_list(Key, Rows)
    catch error:badarg -> invalid
    end.

mixed_key(Key, Rows) ->
    case Key of
        I when is_integer(I) -> catch find_mixed(I, Rows);
        _ -> find_mixed(Key, Rows)
    end.

find_integer(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {value, Value};
        false -> not_found
    end.

find_list(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {value, Value};
        false -> not_found
    end.

find_mixed(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {value, Value};
        false -> not_found
    end.
