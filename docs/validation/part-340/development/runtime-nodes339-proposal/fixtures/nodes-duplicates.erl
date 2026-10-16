-module(nodes_duplicates).
-export([run/0]).
run()-> A=lists:sort(nodes([known,this,connected,visible,hidden,known])), B=lists:usort(nodes(known)), A=B,ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
