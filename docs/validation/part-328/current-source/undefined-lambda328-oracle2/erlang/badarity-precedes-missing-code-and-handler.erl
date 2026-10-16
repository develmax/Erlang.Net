-module(oracle_lambda_handler).
-export([run/0,make/1,make_undef/0,invoke/2,child/2,undefined_lambda/3,undefined_function/3]).
run()->process_flag(error_handler,oracle_lambda_handler),Fun=unresolved(),caught=try oracle_lambda_handler:invoke(Fun,[]) of _->wrong catch error:{badarity,{Observed,Arguments}}->true=Observed=:=Fun,[]=Arguments,caught end,undefined=get(called),undefined=get(wrong_callback),done.
make(Value)->fun(Argument)->{real_body,Value,Argument} end.
make_undef()->fun(_)->erlang:error(undef) end.
invoke(Fun,Arguments)->apply(Fun,Arguments).
undefined_lambda(Module,Fun,Arguments)->
    true=Fun=:=get(expected_fun),
    {module,Module}=erlang:fun_info(Fun,module),
    put(called,{Module,Fun,Arguments,self()}),
    case get(mode) of
        error_mode->erlang:error(boom);
        throw_mode->throw(boom);
        exit_mode->exit(boom);
        explicit_mode->erlang:raise(error,boom,[]);
        _->{forwarded,Module,Arguments,self()}
    end.
undefined_function(Module,Function,Arguments)->
    put(wrong_callback,{Module,Function,Arguments}),erlang:error(wrong_callback).
unresolved()->
    erase(called),erase(wrong_callback),false=erlang:module_loaded(absent_lambda_handler),
    Original=oracle_lambda_handler:make(99),Binary=erlang:term_to_binary(Original),
    From=erlang:atom_to_list(oracle_lambda_handler),To=erlang:atom_to_list(absent_lambda_handler),
    true=length(From)=:=length(To),
    {Patched,1}=replace_name(erlang:binary_to_list(Binary),From,To),
    Fun=erlang:binary_to_term(erlang:list_to_binary(Patched)),
    undefined=get(called),undefined=get(wrong_callback),
    {module,absent_lambda_handler}=erlang:fun_info(Fun,module),
    {arity,1}=erlang:fun_info(Fun,arity),{env,[99]}=erlang:fun_info(Fun,env),
    put(expected_fun,Fun),Fun.
replace_name(Bytes,From,To)->replace_name(Bytes,From,To,[],0).
replace_name([],_,_,Acc,Count)->{lists:reverse(Acc),Count};
replace_name(Bytes,From,To,Acc,Count)->
    case lists:prefix(From,Bytes) of
        true->replace_name(lists:nthtail(length(From),Bytes),From,To,lists:reverse(To,Acc),Count+1);
        false->[Head|Tail]=Bytes,replace_name(Tail,From,To,[Head|Acc],Count)
    end.
incompatible()->
    Original=oracle_lambda_handler:make(99),Binary=erlang:term_to_binary(Original),
    ExternalTermVersion=131,LocalFunctionTag=112,
    <<ExternalTermVersion,LocalFunctionTag,Size:32,Arity,FirstUniqueByte,Rest/binary>>=Binary,
    ChangedUniqueByte=FirstUniqueByte bxor 1,
    Fun=erlang:binary_to_term(<<ExternalTermVersion,LocalFunctionTag,Size:32,Arity,ChangedUniqueByte,Rest/binary>>),
    put(expected_fun,Fun),Fun.
failed(Fun,Class)->
    try oracle_lambda_handler:invoke(Fun,[payload]) of _->erlang:error(accepted)
    catch Class:boom->ok end.
child(Parent,Fun)->
    error_handler=process_flag(error_handler,oracle_lambda_handler),put(expected_fun,Fun),
    Result=oracle_lambda_handler:invoke(Fun,[payload]),Parent!{lambda_child,self(),Result}.
cleanup(Child,OriginalMonitor)->
    CleanupMonitor=erlang:monitor(process,Child),erlang:exit(Child,kill),
    receive {'DOWN',CleanupMonitor,process,Child,_}->ok after 5000->erlang:error(cleanup_timeout) end,
    erlang:demonitor(OriginalMonitor,[flush]).