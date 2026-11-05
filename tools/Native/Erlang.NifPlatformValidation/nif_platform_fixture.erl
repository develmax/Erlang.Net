-module(nif_platform_fixture).
-export([load/1, sum/2, tuple/0, generation/0]).
-nifs([sum/2, tuple/0, generation/0]).
load(Path) -> erlang:load_nif(Path, 0).
sum(_, _) -> erlang:nif_error(not_loaded).
tuple() -> erlang:nif_error(not_loaded).
generation() -> erlang:nif_error(not_loaded).
