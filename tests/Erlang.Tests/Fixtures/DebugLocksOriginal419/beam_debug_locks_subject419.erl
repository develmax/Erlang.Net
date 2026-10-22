-module(beam_debug_locks_subject419).
-export([cases/0,worker/2,expected/1]).
cases() -> [clear,collect,mask,copy_save,unknown,term,list,mask_nil,mask_values,copy_true,copy_false,unknown_true,term_pair].
worker(Parent,Case) ->
 Outcome=try {returned,probe(Case)} catch C:R:S -> {raised,C,R,S} end,
 Parent ! {Case,Outcome}.
probe(clear) -> erts_debug:lcnt_clear();
probe(collect) -> erts_debug:lcnt_collect();
probe(mask) -> erts_debug:lcnt_control(mask);
probe(copy_save) -> erts_debug:lcnt_control(copy_save);
probe(unknown) -> erts_debug:lcnt_control(unknown);
probe(term) -> erts_debug:lcnt_control({mask});
probe(list) -> erts_debug:lcnt_control([]);
probe(mask_nil) -> erts_debug:lcnt_control(mask,[]);
probe(mask_values) -> erts_debug:lcnt_control(mask,[process]);
probe(copy_true) -> erts_debug:lcnt_control(copy_save,true);
probe(copy_false) -> erts_debug:lcnt_control(copy_save,false);
probe(unknown_true) -> erts_debug:lcnt_control(unknown,true);
probe(term_pair) -> erts_debug:lcnt_control(#{},[]).
expected(clear)->{value,error};
expected(collect)->{value,error};
expected(_)->badarg.
