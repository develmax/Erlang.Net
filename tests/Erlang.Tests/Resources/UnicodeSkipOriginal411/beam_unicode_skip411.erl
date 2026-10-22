-module(beam_unicode_skip411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [be16_bmp,be16_pair,le16_bmp,le16_pair,native16,be32_bmp,be32_pair,le32_bmp,le32_max,native32,empty16,short16,low16,high16,badpair16,lelow16,short32,surrogate32,large32,negative32,lelarge32,offset16_pair,offset32_valid,offset16_invalid,offset32_invalid,retry16_suffix,retry16_invalid,retry32_suffix,retry32_invalid,retry16_first,retry32_first,badmatch16,badmatch32].
run() -> [{C,observe(C)} || C <- cases()].
run_stacks() -> [depth(D) || D <- [default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V} -> receive {'DOWN',M,process,Pid,normal} -> V end end.
worker({P,R},C) -> V=try probe(C) of X -> {value,X} catch K:E:S -> {caught,K,E,S} end,P!{R,V}.
probe(be16_bmp) -> skip16be(<<0,65,7>>);
probe(be16_pair) -> skip16be(<<216,61,222,0,7>>);
probe(le16_bmp) -> skip16le(<<65,0,7>>);
probe(le16_pair) -> skip16le(<<61,216,0,222,7>>);
probe(native16) -> skip16native(<<16#1f600/utf16-native,7>>);
probe(be32_bmp) -> skip32be(<<0,0,0,65,7>>);
probe(be32_pair) -> skip32be(<<0,1,246,0,7>>);
probe(le32_bmp) -> skip32le(<<65,0,0,0,7>>);
probe(le32_max) -> skip32le(<<255,255,16,0,7>>);
probe(native32) -> skip32native(<<16#10ffff/utf32-native,7>>);
probe(empty16) -> skip16be(<<>>);
probe(short16) -> skip16be(<<216>>);
probe(low16) -> skip16be(<<220,0,7>>);
probe(high16) -> skip16be(<<216,0>>);
probe(badpair16) -> skip16be(<<216,0,0,65,7>>);
probe(lelow16) -> skip16le(<<0,220,7>>);
probe(short32) -> skip32be(<<0,0,0>>);
probe(surrogate32) -> skip32be(<<0,0,216,0,7>>);
probe(large32) -> skip32be(<<0,17,0,0,7>>);
probe(negative32) -> skip32be(<<255,255,255,255,7>>);
probe(lelarge32) -> skip32le(<<0,0,17,0,7>>);
probe(offset16_pair) -> offset16(<<5:3,216,61,222,0,7>>);
probe(offset32_valid) -> offset32(<<5:3,0,246,1,0,7>>);
probe(offset16_invalid) -> offset16(<<5:3,220,0,7>>);
probe(offset32_invalid) -> offset32(<<5:3,0,216,0,0,7>>);
probe(retry16_suffix) -> retry16(<<0,65,7>>);
probe(retry16_invalid) -> retry16(<<220,0,7>>);
probe(retry32_suffix) -> retry32(<<0,0,0,65,7>>);
probe(retry32_invalid) -> retry32(<<0,17,0,0,7>>);
probe(retry16_first) -> retry16(<<0,65,42,7>>);
probe(retry32_first) -> retry32(<<0,0,0,65,42,7>>);
probe(badmatch16) -> must16(<<216,0>>);
probe(badmatch32) -> must32(<<0,17,0,0>>).
skip16be(<<_/utf16-big,R/bitstring>>) -> R;
skip16be(_) -> no_match.
skip16le(<<_/utf16-little,R/bitstring>>) -> R;
skip16le(_) -> no_match.
skip16native(<<_/utf16-native,R/bitstring>>) -> R;
skip16native(_) -> no_match.
skip32be(<<_/utf32-big,R/bitstring>>) -> R;
skip32be(_) -> no_match.
skip32le(<<_/utf32-little,R/bitstring>>) -> R;
skip32le(_) -> no_match.
skip32native(<<_/utf32-native,R/bitstring>>) -> R;
skip32native(_) -> no_match.
offset16(<<_:3,_/utf16-big,R/bitstring>>) -> R;
offset16(_) -> no_match.
offset32(<<_:3,_/utf32-little,R/bitstring>>) -> R;
offset32(_) -> no_match.
retry16(<<_/utf16-big,42:8,R/bitstring>>) -> {first,R};
retry16(<<H:16,R/bitstring>>) -> {fallback,H,R};
retry16(_) -> no_match.
retry32(<<_/utf32-big,42:8,R/bitstring>>) -> {first,R};
retry32(<<H:32,R/bitstring>>) -> {fallback,H,R};
retry32(_) -> no_match.
must16(B) -> <<_/utf16-big,R/bitstring>>=B,R.
must32(B) -> <<_/utf32-big,R/bitstring>>=B,R.
