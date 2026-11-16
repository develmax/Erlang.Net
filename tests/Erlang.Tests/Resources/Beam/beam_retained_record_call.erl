-module(beam_retained_record_call).
-export([run/2, consume/3]).
-record(state, {config}).

run(Message, State) ->
    ?MODULE:consume(State#state.config, "value", [Message]),
    {noreply, State}.

consume(Config, Format, Values) ->
    erlang:put(Config, {Format, Values}).
