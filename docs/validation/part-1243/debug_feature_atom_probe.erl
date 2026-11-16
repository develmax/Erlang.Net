-module(debug_feature_atom_probe).
-export([value/0, suffix/1, suffix_le/1]).
value() -> {'maybe', 'else', 'ok', [first, ((1 + 2))/3], '\x{6d}aybe'}.
suffix(<<"tag"/utf8, Rest/binary>>) -> Rest.
suffix_le(<<"tag"/little-utf16, Rest/binary>>) -> Rest.
