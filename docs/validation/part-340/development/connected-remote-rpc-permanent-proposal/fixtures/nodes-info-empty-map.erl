-module(nodes_info_empty_map).
-export([run/0]).
run()-> [{Self,#{}}]=nodes(this,#{}),Self=node(),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
