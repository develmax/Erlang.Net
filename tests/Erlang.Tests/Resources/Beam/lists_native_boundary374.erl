-module(lists_native_boundary374).
-export([run/0]).
run() -> try probe() catch error:badarg:Stack -> Stack end.
probe() -> lists:reverse([1|bad], []).
