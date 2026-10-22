-module(beam_debug_unique_subject436).
-export([cases/0,worker/2]).

cases() -> [minimums,off_read,off_write,
 {seed,-9223372036854775808,false},{seed,-1,false},{seed,0,false},
 {seed,576460752303423486,false},{seed,576460752303423487,false},
 {seed,1152921504606846975,false},{seed,9223372036854775807,false},
 {seed,18446744073709551614,false},{seed,18446744073709551615,false},
 {seed,-9223372036854775808,true},{seed,-1,true},{seed,0,true},
 {seed,9223372036854775807,true},{seed,18446744073709551614,true},
 {seed,18446744073709551615,true},
 {invalid,bad},{invalid,[]},{invalid,1.0},
 {invalid,-9223372036854775809},{invalid,18446744073709551616},invalid_modifiers].

worker(Parent,Case) ->
 Outcome=try setup(Case) of Value->{returned,Value}
 catch Class:Reason:Stack->{raised,Class,Reason,Stack} end,
 Parent!{Case,Outcome}.

setup(off_read) ->
 erts_debug:set_internal_state(available_internal_state,false),
 erts_debug:get_internal_state(unique_monotonic_integer_state);
setup(off_write) ->
 erts_debug:set_internal_state(available_internal_state,false),
 erts_debug:set_internal_state(unique_monotonic_integer_state,0);
setup(Case) ->
 erts_debug:set_internal_state(available_internal_state,true),
 Saved=erts_debug:get_internal_state(unique_monotonic_integer_state),
 try probe(Case)
 after erts_debug:set_internal_state(unique_monotonic_integer_state,Saved),
       erts_debug:set_internal_state(available_internal_state,false) end.

probe(minimums) ->
 {erts_debug:get_internal_state(min_unique_integer),
  erts_debug:get_internal_state(min_unique_monotonic_integer)};
probe({seed,Value,Positive}) ->
 true=erts_debug:set_internal_state(unique_monotonic_integer_state,Value),
 Before=erts_debug:get_internal_state(unique_monotonic_integer_state),
 Options=case Positive of true->[monotonic,positive];false->[monotonic] end,
 First=erlang:unique_integer(Options),Second=erlang:unique_integer(Options),
 {Before,First,Second,erts_debug:get_internal_state(unique_monotonic_integer_state)};
probe({invalid,Value}) ->
 true=erts_debug:set_internal_state(unique_monotonic_integer_state,35),
 {erts_debug:set_internal_state(unique_monotonic_integer_state,Value),
  erts_debug:get_internal_state(unique_monotonic_integer_state)};
probe(invalid_modifiers) ->
 true=erts_debug:set_internal_state(unique_monotonic_integer_state,35),
 try erlang:unique_integer([monotonic,bad])
 after 35=erts_debug:get_internal_state(unique_monotonic_integer_state) end.
