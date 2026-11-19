-module(leading_tag_frame_fixture).
-export([route/1, route/2, tagged/1, contained/1, shared/1, failure/1, ordered/1]).
-record(endpoint, {module, data}).

route(Arg) when is_pid(Arg); is_record(Arg, endpoint) ->
    route(Arg, []);
route(Arg) ->
    finish(fallback(Arg)).

route(Arg, Options) -> {Arg, Options}.

tagged(#endpoint{} = Arg) ->
    route(Arg, []);
tagged(Arg) ->
    finish(fallback(Arg)).

contained(Arg) ->
    try route(Arg)
    catch error:Reason -> {caught, Reason}
    end.

shared(Arg) ->
    Value = case Arg of
        #endpoint{} -> Arg;
        Other -> fallback(Other)
    end,
    finish(fallback(Value)).

failure(#endpoint{} = Arg) ->
    erlang:error({selected, Arg});
failure(Arg) ->
    finish(fallback(Arg)).

ordered(Arg) ->
    Before = fallback(Arg),
    case Arg of
        #endpoint{} -> route(Before, []);
        _ -> finish(fallback(Before))
    end.

fallback(fault) -> erlang:error(fallback_failed);
fallback(Arg) -> {fallback, Arg}.
finish(Arg) -> {finished, Arg}.

