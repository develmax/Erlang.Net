-module(beam_closed_atom_identity).
-export([convert/1, passthrough/1, guarded/1]).

convert(listen) -> accept;
convert(accept) -> listen;
convert(connect = Match) -> Match.

passthrough(Match = amber) -> Match;
passthrough(Match = blue) -> Match;
passthrough(Match = cyan) -> Match.

guarded(tcp) -> native;
guarded(Value) when Value == tls; ssl =:= Value -> encrypted.
