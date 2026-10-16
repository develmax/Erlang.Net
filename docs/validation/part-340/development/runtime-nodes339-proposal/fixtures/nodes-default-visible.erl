-module(nodes_default_visible).
-export([run/0]).
run()-> true=(lists:sort(nodes())=:=lists:sort(nodes(visible))),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
