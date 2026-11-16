-module(beam_bif_probe).
-compile([nowarn_deprecated_catch,{nowarn_deprecated_function,{crypto,block_decrypt,4}}]).
-export([who/0, where/0, atom/1, integer/1, tuple/1, remote/1]).
who() -> self().
where() -> erlang:node().
atom(X) -> is_atom(X).
integer(X) -> erlang:is_integer(X).
tuple(X) -> erlang:is_tuple(X).
remote(X) -> erlang:display(X).
