-module(private_atom_relays).
-export([start/2, same_start/2, rotate_start/2, open_start/3, open_other/3]).
start(Opts, Def) -> relay(Opts, restart_type, Def).
same_start(Opts, Def) -> same(Opts, restart_type, Def).
rotate_start(Opts, Def) -> rotate(Opts, restart_type, Def).
open_start(Opts, Atom, Def) -> open(Opts, Atom, Def).
open_other(Opts, Atom, Def) -> open(Opts, Atom, Def).
relay(Opts, Key, Def) -> probe:get(Key, Opts, Def).
same(Opts, Key, Def) -> probe:get(Opts, Key, Def).
rotate(Opts, Key, Def) -> probe:get(Def, Opts, Key).
open(Opts, Key, Def) -> probe:get(Key, Opts, Def).
