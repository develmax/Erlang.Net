-module(beam_sibling_leaf_registers).
-export([reuse/3, shared/3, cons_tail/3]).

reuse(Name, Type, Data) -> {Name, {Name, start, Data}, Type, [Name, marker]}.
shared(Name, Item, Data) -> {Name, {Name, start, Data}, Data, [Item, marker]}.
cons_tail(Name, Data, Tail) -> {Name, {Name, start, Data}, [Name | Tail]}.
