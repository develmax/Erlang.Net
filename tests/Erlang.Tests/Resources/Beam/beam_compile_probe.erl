-module(beam_compile_probe).
-compile([{nowarn_possibly_unsafe_function,{erlang,list_to_atom,1}}]).
-export([answer/0]).
answer() -> 42.
