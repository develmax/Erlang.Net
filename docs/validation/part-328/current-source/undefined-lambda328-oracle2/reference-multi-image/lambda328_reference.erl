-module(lambda328_reference).
-export([run/2,undefined_lambda/3]).
run(Mode,Images)->
    {module,lambda328_payload}=code:load_binary(lambda328_payload,"source-v1.erl",maps:get(v1,Images)),
    Original=lambda328_payload:make(99),Binary=erlang:term_to_binary(Original),
    process_flag(error_handler,lambda328_reference),erase(called),erase(wrong_callback),
    case Mode of
        active_old->
            true=erlang:delete_module(lambda328_payload),false=erlang:module_loaded(lambda328_payload),
            {real_body,99,payload}=Original(payload),Restored=erlang:binary_to_term(Binary),
            {real_body,99,payload}=Restored(payload),undefined=get(called);
        _->
            true=erlang:delete_module(lambda328_payload),true=erlang:purge_module(lambda328_payload),
            false=erlang:module_loaded(lambda328_payload),
            Restored=erlang:binary_to_term(Binary),undefined=get(called),put(expected_fun,Restored),
            case Mode of
                purged_no_current->
                    Self=self(),{forwarded,lambda328_payload,[payload],Self}=Restored(payload),
                    {Restored,[payload],Self}=get(called);
                incompatible_current->
                    {module,lambda328_payload}=code:load_binary(lambda328_payload,"source-v2.erl",maps:get(v2,Images)),
                    badfun(Restored),undefined=get(called);
                no_declared_exports_current->
                    {module,lambda328_payload}=code:load_binary(lambda328_payload,"source-empty.erl",maps:get(empty,Images)),
                    true=erlang:module_loaded(lambda328_payload),
                    [{module_info,0},{module_info,1}]=lists:sort(lambda328_payload:module_info(exports)),
                    badfun(Restored),undefined=get(called);
                missing_lambda_existing_function->
                    {module,lambda328_no_lambda}=code:load_binary(lambda328_no_lambda,"handler.erl",maps:get(no_lambda,Images)),
                    process_flag(error_handler,lambda328_no_lambda),
                    caught=try Restored(payload) of _->wrong catch error:undef->caught end,
                    undefined=get(wrong_callback),undefined=get(called)
            end
    end,done.
undefined_lambda(Module,Fun,Arguments)->
    true=Fun=:=get(expected_fun),{env,[99]}=erlang:fun_info(Fun,env),
    put(called,{Fun,Arguments,self()}),{forwarded,Module,Arguments,self()}.
badfun(Fun)->
    caught=try Fun(payload) of _->wrong catch error:{badfun,Observed}->true=Observed=:=Fun,caught end.
