-module(beam_dynamic_fun_probe).
-export([make/3, module_only/1, arity_only/1]).

make(Module, Function, Arity) -> fun Module:Function/Arity.
module_only(Module) -> fun Module:atom_to_list/1.
arity_only(Arity) -> fun erlang:atom_to_list/Arity.
