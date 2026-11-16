-module(beam_apply_tail).
-export([invoke/2, invoke/3, from_record/3]).
-record(target,{module,function,args}).
invoke(Fun, Args) -> apply(Fun, Args).
invoke(Module, Function, Args) -> erlang:apply(Module, Function, Args).
from_record(_, _, #target{module=M,function=F,args=A}) ->
    apply(M,F,A).
