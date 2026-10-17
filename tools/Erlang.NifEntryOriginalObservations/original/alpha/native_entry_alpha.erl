-module(native_entry_alpha).
-export([load/2,identity/1,marker/0,greeting/0,third/0,ordinary/0]).
load(Path, Info) -> erlang:load_nif(Path, Info).
identity(_) -> erlang:nif_error(not_loaded).
marker() -> erlang:nif_error(not_loaded).
greeting() -> erlang:nif_error(not_loaded).
third() -> greeting().
ordinary() -> ordinary_untouched.
