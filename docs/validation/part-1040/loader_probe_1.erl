-module(loader_probe_1).
-export([run/2]).
run(A,B) -> io:format(A,B), {A,B}.
