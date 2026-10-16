-module(nodes_empty_info).
-export([run/0]).
run()-> []=nodes([],#{connection_id=>true,node_type=>true}),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
