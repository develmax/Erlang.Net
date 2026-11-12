-module(debug_paren_probe).
-export([value/0, field/1, map/1]).
-record(node, {next, value}).
value() -> {"(", '(', $(, (2 + 3)}.
field(Node) -> (Node#node.next)#node.value.
map(Map) -> (Map#{a => 1})#{b => 2}.
