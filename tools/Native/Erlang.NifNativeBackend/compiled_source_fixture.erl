-module(memory_allocator_nif_fixture).
-export([install/0,increment/1,greeting/0,plain/0,tail/1,local/1,named/1,external/1,anonymous/1,remote/1]).
-nifs([increment/1,greeting/0]).
install() -> ok = nif_native_compiled_bridge:install(), ok.
increment(_) -> erlang:nif_error(not_loaded).
greeting() -> erlang:nif_error(not_loaded).
plain() -> 17.
tail(X) -> increment(X).
local(X) -> {increment(X),greeting()}.
named(X) -> F=fun increment/1,F(X).
external(X) -> F=fun memory_allocator_nif_fixture:increment/1,F(X).
anonymous(X) -> F=fun(Y)->increment(Y) end,F(X).
remote(X) -> memory_allocator_nif_fixture:increment(X).
