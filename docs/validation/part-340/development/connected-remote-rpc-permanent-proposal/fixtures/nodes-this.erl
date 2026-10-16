-module(nodes_this).
-export([run/0]).
run()-> [Self]=nodes(this), Self=node(),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
