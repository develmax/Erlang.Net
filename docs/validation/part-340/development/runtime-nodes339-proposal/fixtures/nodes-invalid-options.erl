-module(nodes_invalid_options).
-export([run/0]).
run()-> badarg(fun()->nodes(this,[])end),badarg(fun()->nodes(this,#{node_type=>unexpected})end),badarg(fun()->nodes(this,#{connection_id=>1})end),badarg(fun()->nodes(this,#{unexpected=>true})end),ok.
badarg(F)->try F() of _->error(unexpected_return) catch error:badarg->ok end.
