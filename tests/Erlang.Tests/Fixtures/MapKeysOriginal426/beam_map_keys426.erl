-module(beam_map_keys426).
-export([run/0,run_stacks/0,run_own/0,worker/2,capture/1]).

cases()->[empty,one,numeric32,mixed_numbers,nested_keys,large33,large64,bad_integer,bad_float,bad_atom,bad_nil,bad_list,bad_tuple,bad_binary,bad_bits].
run()->[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 10000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,erts_internal:map_to_tuple_keys(input(N))} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
input(empty)->#{};
input(one)->#{19=>value};
input(numeric32)->maps:from_list([{N,N+1}||N<-lists:seq(1,32)]);
input(mixed_numbers)->#{1=>a,1.0=>b,-9=>c,-9.0=>d,123456789012345678901234567890=>e,0.25=>f};
input(nested_keys)->#{{1,2}=>a,{}=>b,[]=>c,[1,2]=>d,<<1,2>>=>e,<<3:3>>=>f};
input(large33)->maps:from_list([{N,N}||N<-lists:seq(1,33)]);
input(large64)->maps:from_list([{N,N}||N<-lists:seq(1,64)]);
input(bad_integer)->1;
input(bad_float)->1.5;
input(bad_atom)->not_a_map;
input(bad_nil)->[];
input(bad_list)->[1,2];
input(bad_tuple)->{};
input(bad_binary)-><<1>>;
input(bad_bits)-><<3:3>>.
run_own()->[{repeat_identity,repeat_identity()},{atom_storage,atom_storage()},{key_identity,key_identity()}].
repeat_identity()->M=#{73=>a,29=>b},A=erts_internal:map_to_tuple_keys(M),B=erts_internal:map_to_tuple_keys(M),{erts_debug:same(A,B),A,B}.
atom_storage()->M=#{zz426=>1,aa426=>2,mm426=>3},K=erts_internal:map_to_tuple_keys(M),{lists:sort(tuple_to_list(K))=:=lists:sort(maps:keys(M)),K}.
key_identity()->K=list_to_tuple(lists:seq(1,12)),M=#{K=>v},T=erts_internal:map_to_tuple_keys(M),{erts_debug:same(K,element(1,T)),T}.
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),ok=prim_file:write_file(D++"/own-state.etf",term_to_binary(run_own(),[{minor_version,2}])),erlang:halt(0).
