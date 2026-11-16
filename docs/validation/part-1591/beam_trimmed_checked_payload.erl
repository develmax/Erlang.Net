-module(beam_trimmed_checked_payload).
-export([run/2]).
run(First, Second) ->
    {ok, Payload} = prepare(),
    ok = inspect(Payload),
    ok = write_value(Payload, First, Second),
    ok = inspect(Payload),
    done.
prepare() -> {ok,payload}.
inspect(payload) -> ok.
write_value(payload, first, second) -> ok;
write_value(_, _, _) -> invalid.
