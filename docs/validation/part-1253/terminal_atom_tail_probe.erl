-module(terminal_atom_tail_probe).
-export([route/3, lookup/3]).

route(Selector, Host, Fallback) ->
    case pick(route, Selector) of
        remote -> remote(Host, Fallback);
        local -> Host;
        ignored -> ignored
    end.

lookup(Selector, Port, Protocol) ->
    case pick(backend, Selector) of
        inet -> inet_result(Port, Protocol);
        socket -> socket_result(Port, Protocol)
    end.

pick(Key, Selector) -> maps:get(Key, Selector).
remote(Host, Fallback) -> {remote, Host, Fallback}.
inet_result(Port, Protocol) -> {inet, Port, Protocol}.
socket_result(Port, Protocol) -> {socket, Port, Protocol}.
