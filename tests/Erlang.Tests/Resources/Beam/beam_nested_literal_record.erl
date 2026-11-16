-module(beam_nested_literal_record).
-export([run/2, consume/3]).
-record(event, {payload, ignored}).
-record(payload, {kind}).
-record(state, {config}).

run(#event{payload = #payload{kind = selected}}, State = #state{}) ->
    ?MODULE:consume(State#state.config, "value", []),
    {noreply, State}.

consume(Config, Format, Values) ->
    erlang:put(Config, {Format, Values}).
