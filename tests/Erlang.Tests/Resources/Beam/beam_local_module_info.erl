-module(beam_local_module_info).
-export([run/0]).
run() -> module_info(exports).
