namespace Erlang.Differential;

public static class ProcessErrorHandlerCaseNames
{
    public const string Prefix = "process-error-handler-";
    public const string ModulePrefix = """
        -module(oracle_error_handler).
        -export([run/0,undefined_function/3,dynamic_apply/3,dynamic_fun/2,valid/0,child/1]).
        run()->
        """;
    public const string ModuleSuffix = """
        ,done.
        install()->process_flag(error_handler,oracle_error_handler).
        undefined_function(M,F,A)->
            put(called,{M,F,A,self()}),
            case get(mode) of
                error_mode->error(boom);
                error_two_mode->error(boom,[projected]);
                error_three_mode->error(boom,[projected],[]);
                throw_mode->throw(boom);
                exit_mode->exit(boom);
                empty_mode->erlang:raise(error,boom,[]);
                custom_mode->erlang:raise(error,boom,[{trace_module,trace_function,[],[]}]);
                nested_mode->put(mode,return_mode),nested_missing:second(A);
                nested_error_mode->case F of outer_missing->R=nested_missing:inner_missing(),{kept,R};inner_missing->error(boom,[M,F,A]) end;
                _->{forwarded,M,F,A}
            end.
        dynamic_apply(M,F,A)->apply(M,F,A).
        dynamic_fun(F,A)->F(A).
        valid()->error(undef).
        child(S)->S!{child,self(),process_info(self(),error_handler)}.
        rejected(F)->try F() of _->error(accepted) catch error:badarg->ok end.
        frame(F,C)->try F() of _->error(accepted) catch C:boom:[{oracle_error_handler,undefined_function,[missing_module,missing_fun,[payload]],_}|_]->ok end.
        """;
    public const string Literal = "literal-remote-exact-mfa-args-and-self";
    public const string LiteralBody = "error_handler=install(),{forwarded,missing_module,missing_fun,[payload,42]}=missing_module:missing_fun(payload,42),{missing_module,missing_fun,[payload,42],S}=get(called),S=self()";
    public const string NonTail = "non-tail-return-continues-caller";
    public const string NonTailBody = "install(),R=missing_module:missing_fun(payload),{kept,{forwarded,missing_module,missing_fun,[payload]}}={kept,R}";
    public const string Apply = "exported-dynamic-apply";
    public const string ApplyBody = "install(),{forwarded,missing_module,missing_fun,[payload]}=oracle_error_handler:dynamic_apply(missing_module,missing_fun,[payload])";
    public const string ExternalFun = "exported-external-fun-entry";
    public const string ExternalFunBody = "install(),{forwarded,missing_module,missing_fun,[payload]}=oracle_error_handler:dynamic_fun(fun missing_module:missing_fun/1,payload)";
    public const string Nested = "nested-missing-lookup-is-not-globally-disabled";
    public const string NestedBody = "install(),put(mode,nested_mode),{forwarded,nested_missing,second,[[payload]]}=nested_missing:first(payload)";
    public const string Previous = "previous-value-reset-and-any-atom";
    public const string PreviousBody = "error_handler=process_flag(error_handler,unloaded_handler),{error_handler,unloaded_handler}=process_info(self(),error_handler),unloaded_handler=install(),oracle_error_handler=process_flag(error_handler,error_handler),{error_handler,error_handler}=process_info(self(),error_handler)";
    public const string Invalid = "invalid-values-do-not-mutate";
    public const string InvalidBody = "install(),rejected(fun()->process_flag(error_handler,[]) end),rejected(fun()->process_flag(error_handler,42) end),rejected(fun()->process_flag(error_handler,{wrong}) end),rejected(fun()->process_flag({error_handler},oracle_error_handler) end),{error_handler,oracle_error_handler}=process_info(self(),error_handler)";
    public const string Absent = "missing-selected-handler-retains-original-undef";
    public const string AbsentBody = "process_flag(error_handler,unloaded_handler),caught=try missing_module:missing_fun(payload) of _->wrong catch error:undef:[{missing_module,missing_fun,[payload],_}|_]->caught end";
    public const string NoInterception = "valid-undef-and-invalid-send-do-not-invoke-handler";
    public const string NoInterceptionBody = "install(),erase(called),caught=try oracle_error_handler:valid() of _->wrong catch error:undef->caught end,rejected(fun()->erlang:send(unregistered_error_handler_target,payload) end),rejected(fun()->erlang:send(42,payload) end),false=erlang:function_exported(missing_module,missing_fun,1),undefined=get(called)";
    public const string Child = "child-default-does-not-inherit-custom-handler";
    public const string ChildBody = "install(),S=self(),{P,R}=spawn_monitor(fun()->child(S) end),receive {child,P,{error_handler,error_handler}}->ok after 5000->error(child_timeout) end,receive {'DOWN',R,process,P,normal}->ok after 5000->error(down_timeout) end,{error_handler,oracle_error_handler}=process_info(self(),error_handler)";
    public const string ErrorFrames = "error-frame-is-real-selected-handler";
    public const string ErrorFramesBody = "install(),put(mode,error_mode),frame(fun()->missing_module:missing_fun(payload) end,error),frame(fun()->oracle_error_handler:dynamic_apply(missing_module,missing_fun,[payload]) end,error)";
    public const string ThrowFrames = "throw-class-and-selected-handler-frame";
    public const string ThrowFramesBody = "install(),put(mode,throw_mode),frame(fun()->missing_module:missing_fun(payload) end,throw)";
    public const string ExitFrames = "exit-class-and-selected-handler-frame";
    public const string ExitFramesBody = "install(),put(mode,exit_mode),frame(fun()->missing_module:missing_fun(payload) end,exit)";
    public const string ErrorTwo = "error-two-retains-projected-handler-arguments";
    public const string ErrorTwoBody = "install(),put(mode,error_two_mode),caught=try missing_module:missing_fun(payload) of _->wrong catch error:boom:[{oracle_error_handler,undefined_function,[projected],_}|_]->caught end";
    public const string ErrorThree = "error-three-retains-projected-handler-arguments";
    public const string ErrorThreeBody = "install(),put(mode,error_three_mode),caught=try missing_module:missing_fun(payload) of _->wrong catch error:boom:[{oracle_error_handler,undefined_function,[projected],_}|_]->caught end";
    public const string NestedError = "nested-error-two-innermost-handler-precedes-outer-continuation";
    public const string NestedErrorBody = "install(),put(mode,nested_error_mode),caught=try nested_missing:outer_missing() of _->wrong catch error:boom:[{oracle_error_handler,undefined_function,[nested_missing,inner_missing,[]],_}|T]->true=lists:any(fun({oracle_error_handler,undefined_function,3,_})->true;({oracle_error_handler,undefined_function,[nested_missing,outer_missing,[]],_})->true;(_)->false end,T),caught end";
    public const string ExplicitEmpty = "explicit-empty-raise-trace-is-unchanged";
    public const string ExplicitEmptyBody = "install(),put(mode,empty_mode),caught=try missing_module:missing_fun(payload) of _->wrong catch error:boom:[]->caught end";
    public const string ExplicitCustom = "explicit-custom-raise-trace-is-unchanged";
    public const string ExplicitCustomBody = "install(),put(mode,custom_mode),caught=try oracle_error_handler:dynamic_apply(missing_module,missing_fun,[payload]) of _->wrong catch error:boom:[{trace_module,trace_function,[],[]}]->caught end";
    public const string Evaluation = "argument-evaluation-before-missing-dispatch";
    public const string EvaluationBody = "install(),erase(called),put(order,initial),caught=try missing_module:missing_fun(begin put(order,first),error(first_failure) end,begin put(order,second),payload end) of _->wrong catch error:first_failure->caught end,first=get(order),undefined=get(called)";
}
