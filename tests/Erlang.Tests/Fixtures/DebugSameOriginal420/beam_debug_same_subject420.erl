-module(beam_debug_same_subject420).
-export([cases/0,worker/2,expected/1]).
cases()->[atom_equal,atom_different,small_equal,small_different,min_small_copy,max_small_copy,big_positive_copy,big_negative_copy,big_alias,tuple_alias,tuple_copy,list_alias,list_copy,float_alias,float_copy,binary_alias,binary_copy,ref_alias,ref_copy,pid_copy,external_pid_copy,external_fun_copy,local_fun_copy,local_fun_alias].
worker(Parent,Case)->
 Outcome=try {returned,probe(Case)} catch C:R:S->{raised,C,R,S} end,
 Parent ! {Case,Outcome}.
probe(atom_equal)->erts_debug:same(atom,atom);
probe(atom_different)->erts_debug:same(atom,other);
probe(small_equal)->erts_debug:same(42,fresh(42));
probe(small_different)->erts_debug:same(42,43);
probe(min_small_copy)->T=-(1 bsl 59),erts_debug:same(T,fresh(T));
probe(max_small_copy)->T=(1 bsl 59)-1,erts_debug:same(T,fresh(T));
probe(big_positive_copy)->T=1 bsl 59,erts_debug:same(T,fresh(T));
probe(big_negative_copy)->T=-(1 bsl 59)-1,erts_debug:same(T,fresh(T));
probe(big_alias)->T=1 bsl 4096,erts_debug:same(T,T);
probe(tuple_alias)->T={one,two},erts_debug:same(T,T);
probe(tuple_copy)->T={one,two},erts_debug:same(T,fresh(T));
probe(list_alias)->T=[one,two],erts_debug:same(T,T);
probe(list_copy)->T=[one,two],erts_debug:same(T,fresh(T));
probe(float_alias)->T=1.5,erts_debug:same(T,T);
probe(float_copy)->T=1.5,erts_debug:same(T,fresh(T));
probe(binary_alias)->T = <<1,2,3>>,erts_debug:same(T,T);
probe(binary_copy)->T = <<1,2,3>>,erts_debug:same(T,fresh(T));
probe(ref_alias)->T=make_ref(),erts_debug:same(T,T);
probe(ref_copy)->T=make_ref(),erts_debug:same(T,fresh(T));
probe(pid_copy)->T=self(),erts_debug:same(T,fresh(T));
probe(external_pid_copy)->T=binary_to_term(<<131,88,119,13,"other@same420",1:32,0:32,7:32>>),erts_debug:same(T,fresh(T));
probe(external_fun_copy)->T=fun erlang:length/1,erts_debug:same(T,fresh(T));
probe(local_fun_copy)->T=fun(X)->X+1 end,erts_debug:same(T,fresh(T));
probe(local_fun_alias)->T=fun(X)->X+1 end,erts_debug:same(T,T).
fresh(T)->put(same420_copy,T),binary_to_term(term_to_binary(get(same420_copy))).
expected(atom_different)->{value,false};
expected(small_different)->{value,false};
expected(big_positive_copy)->{value,false};
expected(big_negative_copy)->{value,false};
expected(tuple_copy)->{value,false};
expected(list_copy)->{value,false};
expected(float_copy)->{value,false};
expected(binary_copy)->{value,false};
expected(ref_copy)->{value,false};
expected(external_pid_copy)->{value,false};
expected(local_fun_copy)->{value,false};
expected(_)->{value,true}.
