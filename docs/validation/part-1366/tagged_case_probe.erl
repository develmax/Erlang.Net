-module(tagged_case_probe).
-export([discard/1, project/1, preserve/1, distinct_errors/1]).

discard(Options) ->
    {ok, _} = check(),
    case lists:member(enabled, Options) of
        true -> enabled;
        false -> disabled
    end.

project(Options) ->
    {ok, Payload} = check(),
    case lists:member(Payload, Options) of
        true -> enabled;
        false -> disabled
    end.

preserve(Options) ->
    {ok, Payload} = check(),
    case lists:member(Payload, Options) of
        true -> Payload;
        false -> disabled
    end.

distinct_errors(Input) ->
    {ok, Payload} = check(),
    case decide(Input) of
        good -> {yes, Payload};
        bad -> no
    end.

check() ->
    get(tagged_case_value).

decide(Input) ->
    put(tagged_case_seen, Input),
    Input.
