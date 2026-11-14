-module(beam_nested_record_call).
-export([run/2, consume/3]).
-record(event, {payload}).
-record(payload, {value}).
-record(state, {config}).

run(#event{payload = #payload{value = Value}}, State = #state{}) ->
    ?MODULE:consume(State#state.config, "value", [Value]),
    {noreply, State}.

consume(Config, Format, Values) ->
    erlang:put(Config, {Format, Values}).
