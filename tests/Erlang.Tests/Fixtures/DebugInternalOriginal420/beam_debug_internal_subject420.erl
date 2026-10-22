-module(beam_debug_internal_subject420).
-export([cases/0,worker/2,expected/1]).
cases() -> [off_atom,off_badterm,off_invalid_enable,off_other_set,enable_sequence,enabled_unknown_get,enabled_unknown_set,enabled_bad_query,enabled_bad_enable,enabled_bad_reds,enabled_reds_big,enabled_reds_outside,enabled_force_frag,enabled_collision,enabled_lockgraph,enabled_debugger,enabled_status_self,enabled_has_self,enabled_hash_repeat].
worker(Parent,Case) ->
 Outcome=try {returned,probe(Case)} catch C:R:S -> {raised,C,R,S}
 after erts_debug:set_internal_state(available_internal_state,false) end,
 Parent ! {Case,Outcome}.
probe(off_atom) -> erts_debug:get_internal_state(reds_left);
probe(off_badterm) -> erts_debug:get_internal_state({unknown,[]});
probe(off_invalid_enable) -> erts_debug:set_internal_state(available_internal_state,unknown);
probe(off_other_set) -> erts_debug:set_internal_state(unknown,[]);
probe(enable_sequence) ->
 A=erts_debug:set_internal_state(available_internal_state,true),
 B=erts_debug:get_internal_state(available_internal_state),
 C=erts_debug:set_internal_state(available_internal_state,true),
 D=erts_debug:set_internal_state(available_internal_state,false),
 E=erts_debug:set_internal_state(available_internal_state,false),
 {A,B,C,D,E};
probe(Case) -> erts_debug:set_internal_state(available_internal_state,true), enabled(Case).
enabled(enabled_unknown_get) -> erts_debug:get_internal_state(unknown);
enabled(enabled_unknown_set) -> erts_debug:set_internal_state(unknown,[]);
enabled(enabled_bad_query) -> erts_debug:get_internal_state(42);
enabled(enabled_bad_enable) -> erts_debug:set_internal_state(available_internal_state,unknown);
enabled(enabled_bad_reds) -> erts_debug:set_internal_state(reds_left,[]);
enabled(enabled_reds_big) -> erts_debug:set_internal_state(reds_left,1180591620717411303424);
enabled(enabled_reds_outside) ->
 A=erts_debug:set_internal_state(reds_left,-1),B=erts_debug:set_internal_state(reds_left,4001),{A,B};
enabled(enabled_force_frag) -> erts_debug:get_internal_state(force_heap_frags);
enabled(enabled_collision) -> erts_debug:get_internal_state(hashmap_collision_bonanza);
enabled(enabled_lockgraph) -> erts_debug:get_internal_state(lc_graph);
enabled(enabled_debugger) -> erts_debug:get_internal_state(debugger_support);
enabled(enabled_status_self) -> erts_debug:get_internal_state({process_status,self()});
enabled(enabled_has_self) -> has_self(erts_debug:get_internal_state(processes),self());
enabled(enabled_hash_repeat) ->
 H=erts_debug:get_internal_state({internal_hash,[1,2,3]}),
 is_integer(H) andalso H>=0 andalso H=:=erts_debug:get_internal_state({internal_hash,[1,2,3]}).
has_self([],_) -> false;
has_self([H|T],P) -> H=:=P orelse has_self(T,P).
expected(off_atom)->undef;
expected(off_badterm)->undef;
expected(off_invalid_enable)->undef;
expected(off_other_set)->undef;
expected(enable_sequence)->{value,{false,true,true,true,false}};
expected(enabled_reds_outside)->{value,{true,true}};
expected(enabled_force_frag)->{value,false};
expected(enabled_collision)->{value,false};
expected(enabled_lockgraph)->{value,notsup};
expected(enabled_debugger)->{value,false};
expected(enabled_status_self)->{value,running};
expected(enabled_has_self)->{value,true};
expected(enabled_hash_repeat)->{value,true};
expected(_)->badarg.
