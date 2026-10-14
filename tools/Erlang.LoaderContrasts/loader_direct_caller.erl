-module(loader_direct_caller).
-export([direct/1]).

direct(Module) -> erlang:call_on_load_function(Module).
