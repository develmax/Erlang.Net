-module(beam_md5_width423).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[lambda_unsigned_negative_labels,lambda_regular,debug_fallback_large_hint,debug_typed_large_hint,debug_narrow_call_arity,debug_narrow_extra,debug_frame_full_header,record_narrow_extra].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end after 10000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,exit(worker_timeout) end.
worker(P,N)->V=try {value,erts_internal:beamfile_module_md5(input(N))} catch C:R:S->{exception,C,R,S} end,P!{N,V}.
chunk({Id,B})->Pad=(4-byte_size(B) rem 4) rem 4,<<Id/binary,(byte_size(B)):32,B/binary,0:(Pad*8)>>.
beam(Labels,Cs)->B=iolist_to_binary([<<"BEAM">>|[chunk(C)||C<-[{<<"AtU8">>,<<2:32,6,"md5423",4,"true">>},{<<"Code">>,<<16:32,0:32,0:32,Labels:32/signed,0:32>>},{<<"ImpT">>,<<0:32>>},{<<"ExpT">>,<<0:32>>}|Cs]]]),<<"FOR1",(byte_size(B)):32,B/binary>>.
big()-><<120,1,0,0,0,0>>.
frame()-><<4,0,2>>.
hint()->B=big(),<<4,16,23,32,17,87,3,B/binary>>.
fun_table()-><<1:32,1:32,0:32,1:32,0:32,0:32,0:32>>.
input(lambda_unsigned_negative_labels)->beam(-1,[{<<"FunT">>,fun_table()}]);
input(lambda_regular)->beam(2,[{<<"FunT">>,fun_table()}]);
input(debug_fallback_large_hint)->F=frame(),H=hint(),beam(2,[{<<"DbgB">>,<<1:32,1:32,2:32,F/binary,H/binary>>}]);
input(debug_typed_large_hint)->F=frame(),H=hint(),beam(2,[{<<"Type">>,<<4:32,1:32,8191:16>>},{<<"DbgB">>,<<1:32,1:32,2:32,F/binary,H/binary>>}]);
input(debug_narrow_call_arity)->F=frame(),B=big(),beam(2,[{<<"DbgB">>,<<1:32,1:32,3:32,F/binary,4,32,23,48,B/binary,18,18>>}]);
input(debug_narrow_extra)->F=frame(),B=big(),beam(2,[{<<"DbgB">>,<<1:32,1:32,1:32,F/binary,4,48,B/binary,4,48,23,16,2>>}]);
input(debug_frame_full_header)->B=big(),beam(2,[{<<"DbgB">>,<<1:32,1:32,0:32,4,0,23,B/binary>>}]);
input(record_narrow_extra)->B=big(),beam(2,[{<<"Recs">>,<<0:32,1:32,0:32,5,18,34,23,B/binary>>}]).
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
