-module(beam_macro_args_probe).
-define(PICK, ready).
-define(PICK(), 9).
-define(PICK(X, Y), X).
-define(ID(X), X).
-define(CALL_LINE, ?ID(?LINE)).
-export([object/0, zero/0, nested/0, line/0, wrapped_line/0]).
object() -> ?PICK.
zero() -> ?PICK().
nested() -> ?PICK(?ID(42), {ignored,[a,b]}).
line() -> ?ID(
    ?LINE).

wrapped_line() -> ?CALL_LINE.
