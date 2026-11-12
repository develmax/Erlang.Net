-module(beam_append_tail_probe).
-export([name/2, join/3, with_literal/2]).

name(Op, Atom) ->
    Prefix = prefix(Op),
    list_to_atom(Prefix ++ atom_to_list(Atom)).

join(Op, Atom, Tail) ->
    Prefix = prefix(Op),
    pair(Prefix ++ atom_to_list(Atom), Tail).

with_literal(Op, Suffix) ->
    Prefix = prefix(Op),
    pair(Prefix ++ Suffix, ok).

prefix(Op) ->
    Op.

pair(Head, Tail) ->
    {Head,Tail}.
