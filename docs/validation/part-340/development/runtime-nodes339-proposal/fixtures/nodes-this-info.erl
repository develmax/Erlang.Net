-module(nodes_this_info).
-export([run/0]).
run()-> [{Self,#{connection_id:=undefined,node_type:=this}}]=nodes(this,#{connection_id=>true,node_type=>true}),Self=node(),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
