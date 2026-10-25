-module(apply_optimizer_probe).
-export([run/3,literal/1,known_list/1,bound_targets/1,unknown_list/3,module_argument/2,targets_arguments/3,item_effect/1,item_throw/1]).
-compile(no_inline).
run(Name,Input,Output)->
    put(output,Output),save("stage.txt",<<"entered">>),
    try
        {ok,B}=file:read_file(Input),P=erlang:prepare_loading(beam_on_load_fresh_success,B),
        {on_load,[beam_on_load_fresh_success]}=erlang:finish_loading([P]),
        term("prepared",P),
        Result=try invoke(Name,beam_on_load_fresh_success) of Value->{value,Value} catch Class:Reason:Stack->{exception,Class,Reason,Stack} end,
        term("actual",Result),text("actual.txt",Result),
        {messages,Messages}=process_info(self(),messages),term("messages",Messages),
        save("stage.txt",<<"completed">>),halt(0)
    catch FailureClass:FailureReason:FailureStack ->term("failure",{FailureClass,FailureReason,FailureStack}),text("failure.txt",{FailureClass,FailureReason,FailureStack}),save("stage.txt",<<"failed">>),halt(3) end.
invoke("literal",M)->literal(M);
invoke("known-list",M)->known_list(M);
invoke("bound-targets",M)->bound_targets(M);
invoke("unknown-list",M)->?MODULE:unknown_list(erlang,call_on_load_function,[M]);
invoke("module-argument",M)->?MODULE:module_argument(erlang,M);
invoke("targets-arguments",M)->?MODULE:targets_arguments(erlang,call_on_load_function,M);
invoke("item-effect",M)->item_effect(M);
invoke("item-throw",M)->item_throw(M);
invoke("local-override",M)->apply_optimizer_local:run(M).
literal(M)->erlang:apply(erlang,call_on_load_function,[M]).
known_list(M)->Args=[M],erlang:apply(erlang,call_on_load_function,Args).
bound_targets(M)->Mod=erlang,Fun=call_on_load_function,Args=[M],erlang:apply(Mod,Fun,Args).
unknown_list(Mod,Fun,Args)->erlang:apply(Mod,Fun,Args).
module_argument(Mod,M)->erlang:apply(Mod,call_on_load_function,[M]).
targets_arguments(Mod,Fun,M)->erlang:apply(Mod,Fun,[M]).
item_effect(M)->erlang:apply(erlang,call_on_load_function,[begin self()!item_evaluated,M end]).
item_throw(M)->erlang:apply(erlang,call_on_load_function,[begin erlang:error(item_failed),M end]).
term(Name,Value)->save(Name++".etf",term_to_binary(Value)).
text(Name,Value)->save(Name,iolist_to_binary(io_lib:format("~tp~n",[Value]))).
save(Name,Bytes)->ok=file:write_file(filename:join(get(output),Name),Bytes).
