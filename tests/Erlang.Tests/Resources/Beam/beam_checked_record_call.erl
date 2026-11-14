-module(beam_checked_record_call).
-export([run/2, consume/3]).
-record(event, {value}).
-record(state, {config}).

run(Event = #event{}, State = #state{}) ->
    ?MODULE:consume(State#state.config, "value", [Event]),
    {noreply, State}.

consume(Config, Format, Values) ->
    erlang:put(Config, {Format, Values}).
