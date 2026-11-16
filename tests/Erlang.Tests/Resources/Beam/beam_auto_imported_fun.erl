-module(beam_auto_imported_fun).
-export([capture/0]).
capture() -> fun is_integer/1.
