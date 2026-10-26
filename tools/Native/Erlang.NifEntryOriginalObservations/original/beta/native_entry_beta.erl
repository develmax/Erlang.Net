-module(native_entry_beta).
-export([load/2,identity/1,marker/0,'café'/0,third/0,ordinary/0]).
-nifs([identity/1,marker/0,'café'/0]).
load(Path, Info) -> erlang:load_nif(Path, Info).
identity(_) -> erlang:nif_error(not_loaded).
marker() -> erlang:nif_error(not_loaded).
'café'() -> erlang:nif_error(not_loaded).
third() -> 'café'().
ordinary() -> ordinary_untouched.
