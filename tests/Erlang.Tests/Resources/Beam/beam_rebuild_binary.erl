-module(beam_rebuild_binary).
-export([replace/2]).
replace(Id, <<Prefix:12/binary, _:32, Tail/binary>>) ->
    <<Prefix/binary, Id:32, Tail/binary>>.
