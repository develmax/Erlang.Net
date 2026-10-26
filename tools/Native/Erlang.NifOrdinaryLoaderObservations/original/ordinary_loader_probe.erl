-module(ordinary_loader_probe).
-export([load/1,entryprobe/0]).
load(Path) -> erlang:load_nif(Path, []).
entryprobe() -> erlang:nif_error(not_loaded).
