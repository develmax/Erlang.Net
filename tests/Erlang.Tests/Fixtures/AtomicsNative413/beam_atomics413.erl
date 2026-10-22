-module(beam_atomics413).
-export([cases/0,run/0,run_stacks/0,worker/2,capture/1,resource_stacks/0]).
cases()->[initial_signed,initial_unsigned,options_last_wins,private_options_bits,
    signed_edges,unsigned_edges,signed_wrap,unsigned_wrap,wide_increment,subtraction,
    exchange,compare_success,compare_failure,info_signed,info_unsigned,info_sizes,
    creator_survival,concurrent_add,resource_error_contract,
    new_zero,new_negative,new_float,new_huge,new_guard_limit,new_badopt,new_improper,
    new_bad_signed,private_badopts,private_hugeopts,private_zero_precedence,
    put_badref,get_badref,add_badref,add_get_badref,exchange_badref,compare_badref,info_badref,
    sub_badref,sub_get_badref,sub_bad_number,sub_get_bad_number,get_wrongarity,new_wrongarity].
run()->[{C,observe(C)}||C<-cases()].
run_stacks()->
    Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()}|[at_depth(D)||D<-[0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D)->erlang:system_flag(backtrace_depth,D),
    Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(C)->
    {P,R}=spawn_monitor(?MODULE,worker,[self(),C]),
    receive {C,V}->receive {'DOWN',R,process,P,normal}->V end
    after 30000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,exit(worker_timeout) end.
worker(Parent,C)->
    V=try probe(C) of Value->{returned,Value} catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent!{C,V},ok.
probe(initial_signed)->A=atomics:new(3,[]),{is_reference(A),[atomics:get(A,I)||I<-[1,2,3]]};
probe(initial_unsigned)->A=atomics:new(2,[{signed,false}]),[atomics:get(A,I)||I<-[1,2]];
probe(options_last_wins)->
    A=atomics:new(1,[{signed,false},{signed,true}]),B=atomics:new(1,[{signed,true},{signed,false}]),
    {maps:get(min,atomics:info(A)),maps:get(min,atomics:info(B))};
probe(private_options_bits)->
    A=erts_internal:atomics_new(1,16#8000000000000001),B=erts_internal:atomics_new(1,2),
    {maps:get(min,atomics:info(A)),maps:get(min,atomics:info(B))};
probe(signed_edges)->
    A=atomics:new(2,[]),{atomics:put(A,1,-(1 bsl 63)),atomics:put(A,2,(1 bsl 63)-1),atomics:get(A,1),atomics:get(A,2)};
probe(unsigned_edges)->
    A=atomics:new(2,[{signed,false}]),{atomics:put(A,1,0),atomics:put(A,2,(1 bsl 64)-1),atomics:get(A,1),atomics:get(A,2)};
probe(signed_wrap)->
    A=atomics:new(1,[]),ok=atomics:put(A,1,(1 bsl 63)-1),
    {atomics:add_get(A,1,1),atomics:add_get(A,1,-1),atomics:add(A,1,2),atomics:get(A,1)};
probe(unsigned_wrap)->
    A=atomics:new(1,[{signed,false}]),{atomics:add_get(A,1,-1),atomics:add_get(A,1,1)};
probe(wide_increment)->
    A=atomics:new(1,[]),B=atomics:new(1,[{signed,false}]),
    {atomics:add_get(A,1,(1 bsl 64)-1),atomics:add_get(B,1,-(1 bsl 63))};
probe(subtraction)->
    A=atomics:new(1,[]),{atomics:sub(A,1,-7),atomics:sub_get(A,1,2),atomics:sub_get(A,1,-(1 bsl 63))};
probe(exchange)->
    A=atomics:new(1,[{signed,false}]),{atomics:exchange(A,1,(1 bsl 64)-1),atomics:exchange(A,1,7),atomics:get(A,1)};
probe(compare_success)->
    A=atomics:new(1,[]),{atomics:compare_exchange(A,1,0,-7),atomics:get(A,1)};
probe(compare_failure)->
    A=atomics:new(1,[{signed,false}]),ok=atomics:put(A,1,(1 bsl 64)-1),
    {atomics:compare_exchange(A,1,0,7),atomics:get(A,1)};
probe(info_signed)->atomics:info(atomics:new(1,[]));
probe(info_unsigned)->atomics:info(atomics:new(3,[{signed,false}]));
probe(info_sizes)->[maps:get(memory,atomics:info(atomics:new(N,[])))||N<-[1,2,17,1024]];
probe(creator_survival)->
    Parent=self(),{Pid,Ref}=spawn_monitor(fun()->A=atomics:new(1,[]),ok=atomics:put(A,1,42),Parent!{owned_array,A} end),
    receive {owned_array,A}->receive {'DOWN',Ref,process,Pid,normal}->atomics:get(A,1) end end;
probe(concurrent_add)->
    A=atomics:new(1,[]),Parent=self(),
    Workers=[spawn_monitor(fun()->[atomics:add(A,1,1)||_<-lists:seq(1,1000)],Parent!{done,self()} end)||_<-lists:seq(1,4)],
    [receive {done,P}->receive {'DOWN',R,process,P,normal}->ok end end||{P,R}<-Workers],atomics:get(A,1);
probe(resource_error_contract)->
    [resource_contract(C)||C<-resource_cases()];
probe(new_zero)->atomics:new(0,[]);
probe(new_negative)->atomics:new(-1,[]);
probe(new_float)->atomics:new(1.0,[]);
probe(new_huge)->atomics:new(1 bsl 64,[]);
probe(new_guard_limit)->atomics:new(1 bsl 61,[]);
probe(new_badopt)->atomics:new(1,[unknown]);
probe(new_improper)->atomics:new(1,[{signed,true}|improper]);
probe(new_bad_signed)->atomics:new(1,[{signed,unknown}]);
probe(private_badopts)->erts_internal:atomics_new(1,-1);
probe(private_hugeopts)->erts_internal:atomics_new(1,1 bsl 64);
probe(private_zero_precedence)->erts_internal:atomics_new(0,invalid_opts);
probe(put_badref)->atomics:put(missing_atomics413,1,0);
probe(get_badref)->atomics:get(missing_atomics413,1);
probe(add_badref)->atomics:add(missing_atomics413,1,1);
probe(add_get_badref)->atomics:add_get(missing_atomics413,1,1);
probe(exchange_badref)->atomics:exchange(missing_atomics413,1,0);
probe(compare_badref)->atomics:compare_exchange(missing_atomics413,1,0,1);
probe(info_badref)->atomics:info(missing_atomics413);
probe(sub_badref)->atomics:sub(missing_atomics413,1,1);
probe(sub_get_badref)->atomics:sub_get(missing_atomics413,1,1);
probe(sub_bad_number)->atomics:sub(missing_atomics413,1,not_integer);
probe(sub_get_bad_number)->atomics:sub_get(missing_atomics413,1,not_integer);
probe(get_wrongarity)->apply(atomics,get,[missing_atomics413]);
probe(new_wrongarity)->apply(atomics,new,[1]).
resource_cases()->[index_zero,index_high,signed_too_high,unsigned_negative,delta_too_high,delta_too_low,cas_expected,cas_desired,sub_noninteger].
resource_call(index_zero,A)->atomics:get(A,0);
resource_call(index_high,A)->atomics:get(A,2);
resource_call(signed_too_high,A)->atomics:put(A,1,1 bsl 63);
resource_call(unsigned_negative,A)->atomics:put(A,1,-1);
resource_call(delta_too_high,A)->atomics:add_get(A,1,1 bsl 64);
resource_call(delta_too_low,A)->atomics:add(A,1,-(1 bsl 63)-1);
resource_call(cas_expected,A)->atomics:compare_exchange(A,1,1 bsl 63,0);
resource_call(cas_desired,A)->atomics:compare_exchange(A,1,0,1 bsl 63);
resource_call(sub_noninteger,A)->atomics:sub(A,1,not_integer).
resource(C)->
    Opts=case C of unsigned_negative->[{signed,false}];_->[] end,
    A=atomics:new(1,Opts),
    try resource_call(C,A) of V->{unexpected,V}
    catch Class:Reason:Stack->{A,Class,Reason,Stack} end.
resource_contract(C)->
    {A,Class,Reason,Stack}=resource(C),
    Expected=case C of sub_noninteger->badarith;_->badarg end,
    case Stack of
        [{atomics,Function,[First|_],Location}|_]->
            {C,Class=:=error,Reason=:=Expected,Function,First=:=A,is_reference(A),
             lists:keyfind(error_info,1,Location)=:={error_info,#{module=>erl_erts_errors}}};
        []->{C,Class=:=error,Reason=:=Expected,empty_stack};
        _->{C,Class=:=error,Reason=:=Expected,unexpected_stack_shape}
    end.
resource_rows()->[{C,resource(C)}||C<-resource_cases()].
resource_depth(D)->erlang:system_flag(backtrace_depth,D),
    Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,resource_rows()}.
resource_stacks()->
    Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,resource_rows()}|[resource_depth(D)||D<-[0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
capture([Output])->
    V=run(),S=run_stacks(),R=resource_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(V,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(S,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/resource-stacks.etf",term_to_binary(R,[{minor_version,2}])),
    erlang:halt(0).
