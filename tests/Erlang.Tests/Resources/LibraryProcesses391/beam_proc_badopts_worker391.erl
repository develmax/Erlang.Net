-module(beam_proc_badopts_worker391).
-export([worker/2,value/1]).

worker(Parent,Case) ->
    erlang:erase('$ancestors'),
    Old=erlang:system_flag(backtrace_depth,8), erlang:system_flag(backtrace_depth,Old),
    CallLine=?LINE, Outcome=try proc_lib:spawn_opt(?MODULE,value,[],not_list) of
        Unexpected -> {unexpected_return,Unexpected}
    catch Class:Reason:Stack ->
        CompleteExpected=[
            {erlang,spawn_opt,[proc_lib,init_p,[self(),[],?MODULE,value,[]],not_list],
             [{error_info,#{module=>erl_erts_errors,cause=>badopt}}]},
            {proc_lib,spawn_opt,4,[{file,"proc_lib.erl"},{line,269}]},
            {?MODULE,worker,2,[{file,?FILE},{line,CallLine}]}
        ],
        Expected=prefix(erlang:max(1,Old),CompleteExpected),
        {{source_check,Old,Class=:=error,Reason=:=badarg,Stack=:=Expected},
         {complete_raw,self(),Class,Reason,Stack}}
    end,
    Parent ! {Case,Outcome},
    ok.

prefix(0,_) -> [];
prefix(_,[]) -> [];
prefix(N,[H|T]) -> [H|prefix(N-1,T)].
value(Value) -> Value.
