-module(beam_alternating_subject388).
-export([run/2, worker/2]).

worker(Owner, Mode) ->
    Outcome = try run(16384, Mode) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end,
    Owner ! {Mode, Outcome},
    ok.

run(Depth, Mode) -> a(Depth, Mode).

a(0, Mode) -> leaf(Mode);
a(Depth, Mode) ->
    Value = b(Depth - 1, Mode),
    Value + 1.

b(0, Mode) -> leaf(Mode);
b(Depth, Mode) ->
    Value = a(Depth - 1, Mode),
    Value + 1.

leaf(success) -> 0;
leaf(error) -> erlang:error({alternating_error, #{declared_depth => 16384}}).
