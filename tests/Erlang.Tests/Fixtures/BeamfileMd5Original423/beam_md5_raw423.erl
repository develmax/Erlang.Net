-module(beam_md5_raw423).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[minimal,unaligned,opaque_code,outer_trailing,unknown_chunk,attribute_garbage,compile_garbage,duplicate_atoms,empty_strings,empty_fun,fun_old_uniq,fun_old_uniq_zero,line_unknown,line_empty,line_trailing,type_unknown,debug_unknown,debug_empty,records_empty,literal_empty,bad_atom,bad_list,bad_bits,bad_form,truncated_form,bad_form_size,bad_chunk_size,missing_code,missing_atoms,missing_imports,missing_exports,empty_code,code_short,code_version,atoms_zero,atoms_truncated,imports_truncated,imports_bad_atom,exports_bad_atom,exports_bad_arity,fun_bad_label,fun_trailing,line_short,line_negative,line_bad_utf8,type_short,type_empty,debug_short,debug_negative,records_version,records_negative,literal_short,literal_bad_term].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end after 10000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,exit(worker_timeout) end.
worker(P,N)->V=try {value,erts_internal:beamfile_module_md5(input(N))} catch C:R:S->{exception,C,R,S} end,P!{N,V}.
base()->[{<<"AtU8">>,<<1:32,6,"md5423">>},{<<"Code">>,<<16:32,0:32,0:32,2:32,0:32>>},{<<"ImpT">>,<<0:32>>},{<<"ExpT">>,<<0:32>>}].
chunk({Id,B})->Pad=(4-byte_size(B) rem 4) rem 4,<<Id/binary,(byte_size(B)):32,B/binary,0:(Pad*8)>>.
beam(Cs)->B=iolist_to_binary([<<"BEAM">>|[chunk(C)||C<-Cs]]),<<"FOR1",(byte_size(B)):32,B/binary>>.
replace(Id,B)->beam([{K,case K of Id->B;_->V end}||{K,V}<-base()]).
extra(Id,B)->beam(base()++[{Id,B}]).
without(Id)->beam([{K,V}||{K,V}<-base(),K=/=Id]).
input(minimal)->beam(base());
input(unaligned)->B=beam(base()),L=byte_size(B),<<_:1,U:L/binary,_:7>> = <<0:1,B/binary,0:7>>,U;
input(opaque_code)->replace(<<"Code">>,<<16:32,0:32,999:32,1:32,0:32,255,254,253>>);
input(outer_trailing)->B=beam(base()),<<B/binary,255,0,1>>;
input(unknown_chunk)->extra(<<"ZZZZ">>,<<0,1,2>>);
input(attribute_garbage)->extra(<<"Attr">>,<<255,0,1>>);
input(compile_garbage)->extra(<<"CInf">>,<<255,0,1>>);
input(duplicate_atoms)->extra(<<"AtU8">>,<<1:32,3,"new">>);
input(empty_strings)->extra(<<"StrT">>,<<>>);
input(empty_fun)->extra(<<"FunT">>,<<0:32>>);
input(fun_old_uniq_zero)->extra(<<"FunT">>,<<1:32,1:32,0:32,1:32,0:32,0:32,0:32>>);
input(fun_old_uniq)->extra(<<"FunT">>,<<1:32,1:32,0:32,1:32,0:32,0:32,123:32>>);
input(line_unknown)->extra(<<"Line">>,<<999:32>>);
input(line_empty)->extra(<<"Line">>,<<0:32,0:32,0:32,0:32,0:32>>);
input(line_trailing)->extra(<<"Line">>,<<0:32,0:32,0:32,0:32,0:32,255>>);
input(type_unknown)->extra(<<"Type">>,<<999:32>>);
input(debug_unknown)->extra(<<"DbgB">>,<<999:32>>);
input(debug_empty)->extra(<<"DbgB">>,<<1:32,0:32,0:32>>);
input(records_empty)->extra(<<"Recs">>,<<0:32,0:32,0:32>>);
input(literal_empty)->extra(<<"LitT">>,<<0:32,0:32>>);
input(bad_atom)->bad;
input(bad_list)->[1,2];
input(bad_bits)-><<1:1>>;
input(bad_form)-><<"NOPE",4:32,"BEAM">>;
input(truncated_form)-><<"FOR1",4:32,"BEA">>;
input(bad_form_size)-><<"FOR1",3:32,"BEAM">>;
input(bad_chunk_size)-><<"FOR1",12:32,"BEAMCode",(-1):32/signed>>;
input(missing_code)->without(<<"Code">>);
input(missing_atoms)->without(<<"AtU8">>);
input(missing_imports)->without(<<"ImpT">>);
input(missing_exports)->without(<<"ExpT">>);
input(empty_code)->replace(<<"Code">>,<<>>);
input(code_short)->replace(<<"Code">>,<<12:32,0:32,0:32,1:32>>);
input(code_version)->replace(<<"Code">>,<<16:32,1:32,0:32,1:32,0:32>>);
input(atoms_zero)->replace(<<"AtU8">>,<<0:32>>);
input(atoms_truncated)->replace(<<"AtU8">>,<<1:32,6,"no">>);
input(imports_truncated)->replace(<<"ImpT">>,<<1:32>>);
input(imports_bad_atom)->replace(<<"ImpT">>,<<1:32,2:32,1:32,0:32>>);
input(exports_bad_atom)->replace(<<"ExpT">>,<<1:32,2:32,0:32,0:32>>);
input(exports_bad_arity)->replace(<<"ExpT">>,<<1:32,1:32,256:32,0:32>>);
input(fun_bad_label)->extra(<<"FunT">>,<<1:32,1:32,0:32,0:32,0:32,0:32,0:32>>);
input(fun_trailing)->extra(<<"FunT">>,<<0:32,255>>);
input(line_short)->extra(<<"Line">>,<<0:32>>);
input(line_negative)->extra(<<"Line">>,<<0:32,0:32,0:32,(-1):32/signed,0:32>>);
input(line_bad_utf8)->extra(<<"Line">>,<<0:32,0:32,0:32,0:32,1:32,1:16,255>>);
input(type_short)->extra(<<"Type">>,<<4:32>>);
input(type_empty)->extra(<<"Type">>,<<4:32,0:32>>);
input(debug_short)->extra(<<"DbgB">>,<<1:32>>);
input(debug_negative)->extra(<<"DbgB">>,<<1:32,(-1):32/signed,0:32>>);
input(records_version)->extra(<<"Recs">>,<<1:32>>);
input(records_negative)->extra(<<"Recs">>,<<0:32,(-1):32/signed,0:32>>);
input(literal_short)->extra(<<"LitT">>,<<0:32>>);
input(literal_bad_term)->extra(<<"LitT">>,<<0:32,1:32,2:32,131,255>>).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
