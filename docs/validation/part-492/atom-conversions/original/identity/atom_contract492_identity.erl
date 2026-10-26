-module(atom_contract492_identity).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [binary_empty1,binary_nonempty1,binary_empty2,binary_nonempty2,list_empty,list_nonempty,info_repeat,info_cross,info_existing].
probe(binary_empty1) -> A=erlang:apply(erlang,opaque(atom_to_binary),['']),B=erlang:apply(erlang,opaque(atom_to_binary),['']),erts_debug:same(A,B);
probe(binary_nonempty1) -> A=erlang:apply(erlang,opaque(atom_to_binary),[abc]),B=erlang:apply(erlang,opaque(atom_to_binary),[abc]),erts_debug:same(A,B);
probe(binary_empty2) -> A=erlang:apply(erlang,opaque(atom_to_binary),['',utf8]),B=erlang:apply(erlang,opaque(atom_to_binary),['',utf8]),erts_debug:same(A,B);
probe(binary_nonempty2) -> A=erlang:apply(erlang,opaque(atom_to_binary),[abc,utf8]),B=erlang:apply(erlang,opaque(atom_to_binary),[abc,utf8]),erts_debug:same(A,B);
probe(list_empty) -> A=erlang:apply(erlang,opaque(atom_to_list),['']),B=erlang:apply(erlang,opaque(atom_to_list),['']),erts_debug:same(A,B);
probe(list_nonempty) -> A=erlang:apply(erlang,opaque(atom_to_list),[abc]),B=erlang:apply(erlang,opaque(atom_to_list),[abc]),erts_debug:same(A,B);
probe(info_repeat) -> A=info(atom_to_binary,123),B=info(atom_to_binary,123),erts_debug:same(A,B);
probe(info_cross) -> A=info(atom_to_binary,123),B=info(binary_to_atom,123),erts_debug:same(A,B);
probe(info_existing) -> A=info(atom_to_binary,123),B=info(binary_to_existing_atom,123),erts_debug:same(A,B).
info(F,A) -> try erlang:apply(erlang,opaque(F),[A]) catch error:_:S -> {error_info,I}=lists:keyfind(error_info,1,element(4,hd(S))),I end.
opaque(Value) -> put(atom_contract492_identity_input,Value),erase(atom_contract492_identity_input).
