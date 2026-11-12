-module(beam_targeted_inline_probe).
-export([run/2, constant/1, wrapped/1]).
-compile({inline, [pick/2]}).
-compile({inline, answer/1}).
-compile({inline, tagged/1}).

run(Left, Right) -> pick(Left, Right).
constant(Input) -> answer(Input).
pick(_, Selected) -> Selected.
answer(_) -> {ok, 42}.

wrapped(Value) -> tagged(Value).
tagged(Value) -> {ok, Value}.