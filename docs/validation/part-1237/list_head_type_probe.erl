-module(list_head_type_probe).
-export([dot/1, at/1, exported/2, escaped/0, escaped_dot/1]).
dot(List) -> private($., List).
at(List) -> private($@, List).
private(Key, [Key | Rest]) -> Rest;
private(Key, [_ | Rest]) -> private(Key, Rest);
private(_, []) -> [].
exported(Key, [Key | Rest]) -> Rest;
exported(Key, [_ | Rest]) -> exported(Key, Rest);
exported(_, []) -> [].
escaped() -> fun referenced/2.
escaped_dot(List) -> referenced($., List).
referenced(Key, [Key | Rest]) -> Rest;
referenced(Key, [_ | Rest]) -> referenced(Key, Rest);
referenced(_, []) -> [].
