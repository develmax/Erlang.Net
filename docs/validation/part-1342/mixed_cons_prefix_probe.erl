-module(mixed_cons_prefix_probe).
-export([rewrite/1, nil_marker/1, literal_first/1, duplicate/1, fields/2, literal_field/1]).
rewrite([Bin|Tail]) when is_binary(Bin) -> [Bin|rewrite(Tail)];
rewrite([List|Tail]) when is_list(List) -> [rewrite(List)|rewrite(Tail)];
rewrite([$<|Tail]) -> ["&lt;"|rewrite(Tail)];
rewrite([$>|Tail]) -> ["&gt;"|rewrite(Tail)];
rewrite([$&|Tail]) -> ["&amp;"|rewrite(Tail)];
rewrite([Head|Tail]) when is_integer(Head) -> [Head|rewrite(Tail)];
rewrite([]) -> [];
rewrite(Other) -> Other.
nil_marker([Head|Tail]) when is_integer(Head) -> [Head|nil_marker(Tail)];
nil_marker([item|Tail]) -> [mapped|nil_marker(Tail)];
nil_marker([]) -> done;
nil_marker(Other) -> Other.
literal_first([a|Tail]) -> [mapped_a|literal_first(Tail)];
literal_first([c|Tail]) -> [mapped_c|literal_first(Tail)];
literal_first([]) -> [];
literal_first(Other) -> Other.
duplicate([a|Tail]) -> [first|duplicate(Tail)];
duplicate([a|Tail]) -> [second|duplicate(Tail)];
duplicate([b|Tail]) -> [mapped|duplicate(Tail)];
duplicate(Other) -> Other.
fields(Key, [{tag,Key}=Head|_]) -> Head;
fields(Key, [{tag,_}|Tail]) -> fields(Key,Tail);
fields(_, _) -> absent.
literal_field([{tag,undefined}|Tail]) -> [first|literal_field(Tail)];
literal_field([{tag,Value}|Tail]) -> [Value|literal_field(Tail)];
literal_field([]) -> [];
literal_field(Other) -> Other.
