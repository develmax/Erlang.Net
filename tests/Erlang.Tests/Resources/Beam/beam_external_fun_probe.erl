-module(beam_external_fun_probe).
-export([cleanup/0, converter/0]).

cleanup() -> fun application:stop/1.
converter() -> fun erlang:atom_to_list/1.
