-module(nodes_invalid_selector).
-export([run/0]).
run()-> badarg(fun()->nodes([visible|bad_tail])end),badarg(fun()->nodes(unexpected)end),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
