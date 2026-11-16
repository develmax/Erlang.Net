-module(beam_call_case_probe).
-export([route/0]).
route() ->
    case persistent_term:get(beam_case_key, undefined) of
        {ok, Value} -> Value;
        undefined -> []
    end.
