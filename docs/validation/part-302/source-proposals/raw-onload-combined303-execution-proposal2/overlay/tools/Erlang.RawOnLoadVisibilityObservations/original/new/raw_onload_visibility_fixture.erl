-module(raw_onload_visibility_fixture).
-on_load(init/0).
-export([body/0, capture/0]).

init() ->
    erlang:put(raw_visibility_held_fun, fun body/0),
    case os:getenv("RAW_ONLOAD_RETURN") of
        "not_ok" -> not_ok;
        _ -> ok
    end.

body() -> new_body.
capture() -> fun body/0.
