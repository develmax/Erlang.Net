-module(list_conversion_probe).
-compile({no_auto_import,[tuple_to_list/1]}).
-export([tuple_tail/1, tuple_head/1, atom_length/1, binary_length/1,
         copied_length/1, shadow/1, dynamic/2, unrelated/2, branch/3]).

tuple_tail(Tuple) -> tl(erlang:tuple_to_list(Tuple)).
tuple_head(Tuple) -> hd(erlang:tuple_to_list(Tuple)).
atom_length(Atom) -> length(erlang:atom_to_list(Atom)).
binary_length(Binary) -> length(erlang:binary_to_list(Binary)).
copied_length(Atom) ->
    List = erlang:atom_to_list(Atom),
    {List,length(List)}.

tuple_to_list(_) -> [head|improper].
shadow(Tuple) -> tl(tuple_to_list(Tuple)).
dynamic(Module,Tuple) -> tl(Module:tuple_to_list(Tuple)).
unrelated(Tuple,List) -> erlang:tuple_to_list(Tuple), tl(List).
branch(Flag,Tuple,List) ->
    Value = case Flag of true -> erlang:tuple_to_list(Tuple); false -> List end,
    tl(Value).

