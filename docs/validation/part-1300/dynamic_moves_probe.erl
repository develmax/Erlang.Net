-module(dynamic_moves_probe).
-export([run/9, target/0, work/10]).
run(A,B,C,D,E,F,G,H,I) ->
    Target = ?MODULE:target(),
    Target:work(A,B,C,D,E,F,G,H,I,marker),
    ok.
target() -> ?MODULE.
work(A,B,C,D,E,F,G,H,I,Marker) ->
    self() ! {A,B,C,D,E,F,G,H,I,Marker},
    ok.
