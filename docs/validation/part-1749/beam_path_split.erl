-module(beam_path_split).
-export([split/3]).
split([$/ | Tail], Path, Paths) ->
    split(Tail, [], [Path | Paths]);
split([Head | Tail], Path, Paths) ->
    split(Tail, [Head | Path], Paths);
split([], Path, Paths) ->
    [Path | Paths].
