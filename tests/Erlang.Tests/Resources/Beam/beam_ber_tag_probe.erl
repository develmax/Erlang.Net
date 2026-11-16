-module(beam_ber_tag_probe).
-export([tag_number/1, long_tag/2]).
tag_number(<<Class:2, _:1, 31:5, Tail/binary>>) -> (Class bsl 16) bor long_tag(Tail, 0);
tag_number(<<Class:2, _:1, Tag:5>>) -> (Class bsl 16) bor Tag.

long_tag(<<1:1, Part:7, Tail/binary>>, Acc) -> long_tag(Tail, (Acc bsl 7) bor Part);
long_tag(<<0:1, Part:7>>, Acc) -> (Acc bsl 7) bor Part.

