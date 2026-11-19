-module(beam_shared_tag_payload).
-export([choose/1]).

choose(Options) ->
    case proplists:get_value(result, Options) of
        {ok, Pid} = Success when is_pid(Pid) ->
            Success;
        {error, {Reason, _Details}} ->
            {error, Reason}
    end.
