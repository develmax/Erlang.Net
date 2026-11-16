-module(debug_nested_apply_probe).
-export([invoke/2, suffix/1, encode/1]).
invoke(Factory, Value) -> (Factory(select))(Value).
suffix([$a,$b] ++ Rest) -> Rest;
suffix(_) -> missing.
encode(Value) -> <<(byte_size(Value)):4/unit:8>>.
