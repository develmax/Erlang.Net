-module(beam_retained_record_dispatch).
-export([run/3, consume/1, consume/3]).
-record(state, {config, detail}).

run(report, _, State = #state{detail = Detail}) ->
    ?MODULE:consume(Detail),
    {reported, State};
run(Message, _, State) ->
    ?MODULE:consume(State#state.config, "value", [Message]),
    {noreply, State}.

consume(Detail) ->
    erlang:put(detail, {value, Detail}).

consume(Config, Format, Values) ->
    erlang:put(Config, {Format, Values}).
