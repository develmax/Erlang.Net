-module(beam_finite_entry_dispatch_probe).
-export([up/1, down/1, left/1, right/1, from_call/2]).

up(N) -> move(up, N).
down(N) -> move(down, N).
left(N) -> move(left, N).
right(N) -> move(right, N).

move(up, N) -> lists:duplicate(N, io_ansi:cursor_up());
move(down, N) -> lists:duplicate(N, io_ansi:cursor_down());
move(left, N) -> lists:duplicate(N, io_ansi:cursor_backward());
move(right, N) -> lists:duplicate(N, io_ansi:cursor_forward()).

from_call(X, Value) ->
    Direction = direction(X),
    route(Direction, Value).

direction(0) -> north;
direction(_) -> south.

route(north, Value) -> north(Value);
route(south, Value) -> south(Value).

north(Value) -> {north, Value}.
south(Value) -> {south, Value}.
