-module(conversion_call_probe).
-export([bytes/1, radix/2, slice/3, as_atom/1, local_target/1]).
-compile({no_auto_import,[atom_to_list/1]}).
bytes(Value) ->
    list_to_binary(erlang:atom_to_list(Value)).
radix(Value, Base) ->
    list_to_integer(integer_to_list(Value, Base), Base).
slice(Value, Start, Stop) ->
    list_to_binary(binary_to_list(Value, Start, Stop)).
as_atom(Value) ->
    list_to_atom(binary_to_list(Value)).
local_target(Value) ->
    iolist_to_binary(atom_to_list(Value)).
atom_to_list(_) ->
    "local".
