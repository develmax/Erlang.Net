-module(unicode_pair_probe).
-export([run/2, target/0, work/3, done/1]).
run(First, Second) when is_list(First), is_list(Second) ->
    Target = ?MODULE:target(),
    FirstBin = unicode:characters_to_binary([First|[0]]),
    SecondBin = unicode:characters_to_binary([Second|[0]]),
    Target:work(FirstBin, SecondBin, marker),
    ?MODULE:done(marker).
target() -> ?MODULE.
work(First, Second, Marker) -> self() ! {First, Second, Marker}, ok.
done(Value) -> Value.
